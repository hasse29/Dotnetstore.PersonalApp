using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Stock;

/// <summary>A movement as the history shows it: the DTO plus text in the current UI language.</summary>
internal sealed record StockMovementRow(StockMovementDto Movement, string DateText, string TypeText, string QuantityText, string DetailText);

/// <summary>A type choice for the history's filter; a null <see cref="Type"/> means all types.</summary>
internal sealed record StockMovementTypeOption(StockMovementType? Type, string Label);

internal sealed partial class StockMovementsViewModel(ListStockMovements.Handler list, ILocalizationService localization) : ViewModelBase
{
    private const int PageSize = 15;

    private bool _loading;

    public ObservableCollection<StockMovementRow> Movements { get; } = [];

    public IReadOnlyList<StockMovementTypeOption> FilterTypes { get; } = TypeOptions(localization);

    [ObservableProperty]
    public partial StockMovementTypeOption? SelectedFilter { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageLabel))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageLabel))]
    [NotifyPropertyChangedFor(nameof(ShowPager))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial int PageCount { get; set; } = 1;

    public string PageLabel => localization.Format("StockMovements_PageOf", CurrentPage, PageCount);

    public bool ShowPager => PageCount > 1;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    partial void OnSelectedFilterChanged(StockMovementTypeOption? value)
    {
        if (!_loading)
        {
            CurrentPage = 1;
            LoadCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _loading = true;
        try
        {
            SelectedFilter ??= FilterTypes[0];

            var response = await list.HandleAsync(new ListStockMovements.Query(Search, SelectedFilter.Type, null, CurrentPage, PageSize), cancellationToken);

            var pageCount = Math.Max(1, (response.TotalCount + PageSize - 1) / PageSize);
            if (CurrentPage > pageCount)
            {
                CurrentPage = pageCount;
                response = await list.HandleAsync(new ListStockMovements.Query(Search, SelectedFilter.Type, null, CurrentPage, PageSize), cancellationToken);
            }

            PageCount = pageCount;

            Movements.Clear();
            foreach (var movement in response.Items)
            {
                Movements.Add(ToRow(movement));
            }

            StatusMessage = localization.Format("StockMovements_Count", response.TotalCount);
        }
        finally
        {
            _loading = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync(CancellationToken cancellationToken)
    {
        CurrentPage = 1;
        await LoadAsync(cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(HasPreviousPage))]
    private async Task PreviousPageAsync(CancellationToken cancellationToken)
    {
        CurrentPage--;
        await LoadAsync(cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(HasNextPage))]
    private async Task NextPageAsync(CancellationToken cancellationToken)
    {
        CurrentPage++;
        await LoadAsync(cancellationToken);
    }

    private bool HasPreviousPage() => CurrentPage > 1;

    private bool HasNextPage() => CurrentPage < PageCount;

    private static List<StockMovementTypeOption> TypeOptions(ILocalizationService localization)
    {
        var options = Enum.GetValues<StockMovementType>().Select(t => new StockMovementTypeOption(t, localization.Get($"StockMovementType_{t}"))).ToList();
        options.Insert(0, new StockMovementTypeOption(null, localization.Get("StockMovements_AllTypes")));
        return options;
    }

    private StockMovementRow ToRow(StockMovementDto movement)
    {
        var detail = movement.SupplierName is { } supplier ? supplier : string.Empty;
        if (movement.Note is { } note)
        {
            detail = detail.Length == 0 ? note : $"{detail} · {note}";
        }

        return new StockMovementRow(
            movement,
            string.Create(CultureInfo.CurrentCulture, $"{movement.OccurredAt.ToLocalTime():g}"),
            localization.Get($"StockMovementType_{movement.Type}"),
            string.Create(CultureInfo.CurrentCulture, $"{movement.Quantity:+0.##;-0.##;0}"),
            detail);
    }
}
