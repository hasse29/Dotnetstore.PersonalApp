using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Pdf;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;

/// <summary>A pick list as the list shows it: the DTO plus its texts in the current UI language.</summary>
internal sealed record PickListRow(PickListDto PickList, string StatusText, string ProgressText);

internal sealed record PickListStatusOption(PickListStatus? Status, string Label);

/// <summary>The pick lists; opening one replaces the list with the picking screen. Open lists come first in the filter.</summary>
internal sealed partial class PickListsViewModel : ViewModelBase
{
    private readonly ListPickLists.Handler _list;
    private readonly GetPickList.Handler _get;
    private readonly SavePickedQuantities.Handler _save;
    private readonly ILocalizationService _localization;
    private readonly SalesPdfSaver _pdf;
    private readonly bool _ready;

    public PickListsViewModel(
        ListPickLists.Handler list,
        GetPickList.Handler get,
        SavePickedQuantities.Handler save,
        ILocalizationService localization,
        SalesPdfSaver pdf)
    {
        _list = list;
        _get = get;
        _save = save;
        _localization = localization;
        _pdf = pdf;

        StatusOptions =
        [
            new PickListStatusOption(null, localization.Get("PickLists_AllStatuses")),
            .. Enum.GetValues<PickListStatus>().Select(s => new PickListStatusOption(s, localization.Get($"PickListStatus_{s}"))),
        ];

        // What there is to do is what the user wants to see first.
        SelectedStatus = StatusOptions.First(o => o.Status == PickListStatus.Open);
        _ready = true;
    }

    public IReadOnlyList<PickListStatusOption> StatusOptions { get; }

    public ObservableCollection<PickListRow> PickLists { get; } = [];

    public bool HasScreen => Screen is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasScreen))]
    public partial PickListScreenViewModel? Screen { get; set; }

    [ObservableProperty]
    public partial PickListRow? SelectedPickList { get; set; }

    [ObservableProperty]
    public partial PickListStatusOption SelectedStatus { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    partial void OnSelectedPickListChanged(PickListRow? value)
    {
        if (value is null)
        {
            return;
        }

        var screen = new PickListScreenViewModel(value.PickList.Id, _get, _save, _localization, _pdf);
        screen.Closed += OnScreenClosed;
        Screen = screen;
        screen.LoadCommand.Execute(null);
    }

    partial void OnSelectedStatusChanged(PickListStatusOption value)
    {
        if (_ready)
        {
            LoadCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await _list.HandleAsync(new ListPickLists.Query(Search, SelectedStatus.Status), cancellationToken);

        PickLists.Clear();
        foreach (var pickList in response.Items)
        {
            PickLists.Add(new PickListRow(
                pickList,
                _localization.Get($"PickListStatus_{pickList.Status}"),
                _localization.Format("PickLists_Progress", pickList.PickedLineCount, pickList.LineCount)));
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? _localization.Format("PickLists_ShowingPartial", response.Items.Count, response.TotalCount)
            : _localization.Format("PickLists_Count", response.TotalCount);
    }

    // Event handler, so it has to be async void; LoadAsync reports its own outcome on the status line.
    private async void OnScreenClosed(object? sender, EventArgs e)
    {
        if (sender is PickListScreenViewModel screen)
        {
            screen.Closed -= OnScreenClosed;
        }

        Screen = null;
        SelectedPickList = null;
        await LoadAsync(CancellationToken.None);
    }
}
