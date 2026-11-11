using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Products;

internal sealed partial class ProductEditorViewModel : ViewModelBase
{
    private const decimal DefaultVatRate = 25m;

    private readonly ProductDto? _existing;
    private readonly CreateProduct.Handler _create;
    private readonly UpdateProduct.Handler _update;
    private readonly ILocalizationService _localization;

    public ProductEditorViewModel(
        ProductDto? existing,
        IReadOnlyList<ProductKindOption> kinds,
        IReadOnlyList<ProductUnitOption> units,
        IReadOnlyList<ProductCategoryOption> categories,
        CreateProduct.Handler create,
        UpdateProduct.Handler update,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;
        Kinds = kinds;
        Units = units;
        Categories = categories;

        var initialKind = existing?.Kind ?? ProductKind.Hardware;
        SelectedKind = kinds.FirstOrDefault(k => k.Kind == initialKind) ?? kinds[0];
        SelectedUnit = units.FirstOrDefault(u => u.Unit == (existing?.Unit ?? DefaultUnit(initialKind))) ?? units[0];
        SelectedCategory = categories.FirstOrDefault(c => c.Id == existing?.CategoryId) ?? categories[0];

        if (existing is null)
        {
            VatRate = DefaultVatRate;
            UnitPrice = 0m;
            CostPrice = 0m;
        }
        else
        {
            Number = existing.Number;
            Name = existing.Name;
            Description = existing.Description ?? string.Empty;
            IsActive = existing.IsActive;
            UnitPrice = existing.UnitPrice;
            CostPrice = existing.CostPrice;
            VatRate = existing.VatRate;
            Manufacturer = existing.Manufacturer ?? string.Empty;
            Model = existing.Model ?? string.Empty;
            SoftwareVersion = existing.SoftwareVersion ?? string.Empty;
            MinimumHours = existing.MinimumHours;
            MinimumStock = existing.MinimumStock;
        }
    }

    public event EventHandler<ProductDto>? Saved;

    public event EventHandler? Cancelled;

    public IReadOnlyList<ProductKindOption> Kinds { get; }

    public IReadOnlyList<ProductUnitOption> Units { get; }

    public IReadOnlyList<ProductCategoryOption> Categories { get; }

    public string Title => _localization.Get(_existing is null ? "ProductEditor_New" : "ProductEditor_Edit");

    public bool IsHardware => SelectedKind?.Kind == ProductKind.Hardware;

    public bool IsSoftware => SelectedKind?.Kind == ProductKind.Software;

    public bool IsConsulting => SelectedKind?.Kind == ProductKind.Consulting;

    /// <summary>Hardware and software can be kept in stock; consulting hours cannot.</summary>
    public bool IsStocked => !IsConsulting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHardware))]
    [NotifyPropertyChangedFor(nameof(IsSoftware))]
    [NotifyPropertyChangedFor(nameof(IsConsulting))]
    [NotifyPropertyChangedFor(nameof(IsStocked))]
    public partial ProductKindOption? SelectedKind { get; set; }

    [ObservableProperty]
    public partial ProductUnitOption? SelectedUnit { get; set; }

    [ObservableProperty]
    public partial ProductCategoryOption? SelectedCategory { get; set; }

    [ObservableProperty]
    public partial string Number { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsActive { get; set; } = true;

    [ObservableProperty]
    public partial decimal? UnitPrice { get; set; }

    [ObservableProperty]
    public partial decimal? CostPrice { get; set; }

    [ObservableProperty]
    public partial decimal? VatRate { get; set; }

    [ObservableProperty]
    public partial string Manufacturer { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Model { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SoftwareVersion { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal? MinimumHours { get; set; }

    [ObservableProperty]
    public partial decimal? MinimumStock { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    // A new product gets the unit that usually goes with its kind; an existing product keeps what was saved.
    partial void OnSelectedKindChanged(ProductKindOption? value)
    {
        if (_existing is null && value?.Kind is { } kind)
        {
            SelectedUnit = Units.FirstOrDefault(u => u.Unit == DefaultUnit(kind)) ?? SelectedUnit;
        }
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var missing = FirstMissingField();
        if (missing is not null)
        {
            ErrorMessage = _localization.Describe(missing);
            return;
        }

        var fields = new ProductFields(
            Number,
            Name,
            SelectedKind!.Kind!.Value,
            SelectedCategory?.Id,
            Description,
            IsActive,
            SelectedUnit!.Unit,
            UnitPrice!.Value,
            CostPrice ?? 0m,
            VatRate!.Value,
            Manufacturer,
            Model,
            SoftwareVersion,
            MinimumHours,
            MinimumStock);

        var result = _existing is null
            ? await _create.HandleAsync(new CreateProduct.Command(fields), cancellationToken)
            : await _update.HandleAsync(new UpdateProduct.Command(_existing.Id, fields), cancellationToken);

        if (result.IsSuccess)
        {
            Saved?.Invoke(this, result.Value);
        }
        else
        {
            ErrorMessage = _localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);

    private static ProductUnit DefaultUnit(ProductKind? kind) => kind switch
    {
        ProductKind.Software => ProductUnit.License,
        ProductKind.Consulting => ProductUnit.Hour,
        _ => ProductUnit.Piece,
    };

    // The pickers and number boxes can be empty, which the handlers' value types cannot express, so the form reports those cases.
    private Error? FirstMissingField()
    {
        if (SelectedKind?.Kind is null)
        {
            return Error.Validation("product_kind_invalid", "Kind is not valid.");
        }

        if (SelectedUnit is null)
        {
            return Error.Validation("product_unit_invalid", "Unit is not valid.");
        }

        if (UnitPrice is null)
        {
            return Error.Validation("product_price_required", "Price is required.");
        }

        return VatRate is null ? Error.Validation("product_vat_required", "VAT is required.") : null;
    }
}
