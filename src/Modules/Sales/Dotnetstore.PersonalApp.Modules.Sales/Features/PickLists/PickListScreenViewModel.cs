using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Pdf;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;

/// <summary>One line of the pick list: what is ordered and how much of it has been picked (editable while the list is not cancelled).</summary>
internal sealed partial class PickListLineRow(PickListLineDto line, string unitLabel, bool isEditable) : ObservableObject
{
    public PickListLineDto Line { get; } = line;

    public bool IsEditable { get; } = isEditable;

    public string OrderedText => string.Create(CultureInfo.CurrentCulture, $"{Line.Quantity:0.##} {unitLabel}");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemainingText))]
    public partial decimal? PickedQuantity { get; set; } = line.PickedQuantity;

    /// <summary>What is left to pick; empty once the line is complete.</summary>
    public string RemainingText => Line.Quantity - (PickedQuantity ?? 0) is > 0 and var remaining
        ? string.Create(CultureInfo.CurrentCulture, $"{remaining:0.##}")
        : string.Empty;
}

/// <summary>The picking itself: enter what has been picked, save as you go. The list completes by itself when everything is picked.</summary>
internal sealed partial class PickListScreenViewModel(
    Guid pickListId,
    GetPickList.Handler get,
    SavePickedQuantities.Handler save,
    ILocalizationService localization,
    SalesPdfSaver pdf) : ViewModelBase
{
    private PickListDto? _pickList;

    public ObservableCollection<PickListLineRow> Lines { get; } = [];

    public string Heading => _pickList is null ? string.Empty : $"{_pickList.Number} · {_pickList.OrderNumber}";

    public string CustomerName => _pickList?.CustomerName ?? string.Empty;

    public string StatusText => _pickList is null ? string.Empty : localization.Get($"PickListStatus_{_pickList.Status}");

    /// <summary>False once the list is cancelled or its order has been invoiced.</summary>
    public bool IsEditable => _pickList is { Status: not PickListStatus.Cancelled, IsLocked: false };

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    public event EventHandler? Closed;

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var result = await get.HandleAsync(new GetPickList.Query(pickListId), cancellationToken);
        if (result.IsSuccess)
        {
            Apply(result.Value);
        }
        else
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var result = await save.HandleAsync(new SavePickedQuantities.Command(pickListId, [.. Lines.Select(l => new SavePickedQuantities.Entry(l.Line.Id, l.PickedQuantity ?? 0))]), cancellationToken);
        if (result.IsSuccess)
        {
            Apply(result.Value);
            StatusMessage = localization.Get(result.Value.PickList.Status == PickListStatus.Complete ? "PickListScreen_Complete" : "PickListScreen_Saved");
            return;
        }

        // Lines before the one that failed (for example for lack of stock) are saved, so the screen shows what is really picked.
        await LoadAsync(cancellationToken);
        StatusMessage = localization.Describe(result.Error);
    }

    [RelayCommand]
    private void PickAll()
    {
        foreach (var line in Lines)
        {
            line.PickedQuantity = line.Line.Quantity;
        }
    }

    [RelayCommand]
    private void Back() => Closed?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private async Task ExportPdfAsync(CancellationToken cancellationToken)
    {
        if (await pdf.SaveAsync(SalesDocumentKind.PickList, pickListId, cancellationToken) is { } message)
        {
            StatusMessage = message;
        }
    }

    private void Apply(PickListDetailDto detail)
    {
        _pickList = detail.PickList;

        Lines.Clear();
        foreach (var line in detail.Lines)
        {
            Lines.Add(new PickListLineRow(line, localization.Find($"ProductUnit_{line.Unit}") ?? line.Unit, IsEditable));
        }

        OnPropertyChanged(nameof(Heading));
        OnPropertyChanged(nameof(CustomerName));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(IsEditable));
    }
}
