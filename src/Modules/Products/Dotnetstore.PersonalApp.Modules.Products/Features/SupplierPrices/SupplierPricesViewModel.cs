using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.SupplierPrices;

/// <summary>A price as the list shows it: the DTO plus text in the current UI language.</summary>
internal sealed record SupplierPriceRow(SupplierPriceDto Price, string PriceText, string DetailText, string BadgeText);

/// <summary>The "Suppliers" card of a product's detail page: every supplier's price, with add, change and remove.</summary>
internal sealed partial class SupplierPricesViewModel(
    Guid productId,
    string unitLabel,
    ListSupplierPrices.Handler list,
    SetSupplierPrice.Handler set,
    DeleteSupplierPrice.Handler delete,
    ListSuppliers.Handler suppliers,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<SupplierPriceRow> Prices { get; } = [];

    /// <summary>The suppliers the editor offers: active ones without a price yet, or just the one being changed.</summary>
    public ObservableCollection<SupplierDto> AvailableSuppliers { get; } = [];

    public bool IsEmpty => Prices.Count == 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial SupplierPriceRow? SelectedPrice { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotEditing))]
    public partial bool IsEditing { get; set; }

    public bool IsNotEditing => !IsEditing;

    /// <summary>True while changing an existing price, when the supplier cannot be switched.</summary>
    [ObservableProperty]
    public partial bool IsSupplierLocked { get; set; }

    [ObservableProperty]
    public partial SupplierDto? SelectedSupplier { get; set; }

    [ObservableProperty]
    public partial string ArticleNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal? Price { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedPriceChanged(SupplierPriceRow? value) => IsConfirmingDelete = false;

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var items = await list.HandleAsync(new ListSupplierPrices.Query(productId), cancellationToken);

        // The cheapest price is only worth pointing out when there is something to compare it with.
        var lowest = items.Count > 1 ? items.Min(p => p.Price) : (decimal?)null;

        Prices.Clear();
        foreach (var item in items)
        {
            Prices.Add(ToRow(item, item.Price == lowest));
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    private async Task AddAsync(CancellationToken cancellationToken)
    {
        var response = await suppliers.HandleAsync(new ListSuppliers.Query(ActiveOnly: true), cancellationToken);
        var priced = Prices.Select(r => r.Price.SupplierId).ToHashSet();

        AvailableSuppliers.Clear();
        foreach (var supplier in response.Items.Where(s => !priced.Contains(s.Id)))
        {
            AvailableSuppliers.Add(supplier);
        }

        if (AvailableSuppliers.Count == 0)
        {
            StatusMessage = localization.Get(response.Items.Count == 0 ? "SupplierPrices_NoSuppliers" : "SupplierPrices_AllPriced");
            return;
        }

        StatusMessage = null;
        SelectedPrice = null;
        SelectedSupplier = AvailableSuppliers[0];
        ArticleNumber = string.Empty;
        Price = null;
        ErrorMessage = null;
        IsSupplierLocked = false;
        IsEditing = true;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Edit()
    {
        if (SelectedPrice is not { } row)
        {
            return;
        }

        var dto = row.Price;
        AvailableSuppliers.Clear();
        var current = new SupplierDto(dto.SupplierId, dto.SupplierName, null, null, null, null, null, dto.SupplierIsActive);
        AvailableSuppliers.Add(current);

        StatusMessage = null;
        SelectedSupplier = current;
        ArticleNumber = dto.SupplierArticleNumber ?? string.Empty;
        Price = dto.Price;
        ErrorMessage = null;
        IsSupplierLocked = true;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        if (SelectedSupplier is not { } supplier)
        {
            ErrorMessage = localization.Describe(Error.Validation("supplier_required", "Select a supplier."));
            return;
        }

        if (Price is not { } price)
        {
            ErrorMessage = localization.Describe(Error.Validation("supplier_price_required", "Enter a price."));
            return;
        }

        var result = await set.HandleAsync(new SetSupplierPrice.Command(productId, supplier.Id, ArticleNumber, price), cancellationToken);
        if (!result.IsSuccess)
        {
            ErrorMessage = localization.Describe(result.Error);
            return;
        }

        IsEditing = false;
        await LoadAsync(cancellationToken);
        StatusMessage = localization.Format("SupplierPrices_Saved", result.Value.SupplierName);
    }

    [RelayCommand]
    private void Cancel() => IsEditing = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedPrice is not { } row)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteSupplierPrice.Command(row.Price.Id), cancellationToken);

        IsConfirmingDelete = false;
        SelectedPrice = null;
        await LoadAsync(cancellationToken);
        StatusMessage = result.IsSuccess ? null : localization.Describe(result.Error);
    }

    private bool HasSelection() => SelectedPrice is not null;

    private SupplierPriceRow ToRow(SupplierPriceDto dto, bool isLowest)
    {
        var detail = string.Create(CultureInfo.CurrentCulture, $"{dto.UpdatedAt.ToLocalTime():d}");
        if (dto.SupplierArticleNumber is { } article)
        {
            detail = $"{article} · {detail}";
        }

        return new SupplierPriceRow(
            dto,
            string.Create(CultureInfo.CurrentCulture, $"{dto.Price:N2} / {unitLabel}"),
            detail,
            isLowest ? localization.Get("SupplierPrices_Lowest") : string.Empty);
    }
}
