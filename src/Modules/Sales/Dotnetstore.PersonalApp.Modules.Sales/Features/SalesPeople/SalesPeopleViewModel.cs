using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritories;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPeople;

internal sealed partial class SalesPeopleViewModel(
    ListSalesPeople.Handler list,
    CreateSalesPerson.Handler create,
    UpdateSalesPerson.Handler update,
    DeleteSalesPerson.Handler delete,
    IDetailFactory<SalesPersonDto> detailFactory,
    ListSalesTerritoryOptions.Handler listSalesTerritoryOptions,
    ExternalReferences references,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _salesTerritoryOptions = [];
    private IReadOnlyList<OptionDto> _userOptions = [];

    [ObservableProperty]
    public partial DetailViewModel? Detail { get; set; }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenDetail()
    {
        if (SelectedSalesPerson is not { } selected)
        {
            return;
        }

        var detail = detailFactory.Create(selected);
        detail.Closed += OnDetailClosed;
        Detail = detail;
    }

    // Event handler, so it has to be async void; LoadAsync reports its own outcome on the status line.
    private async void OnDetailClosed(object? sender, EventArgs e)
    {
        if (Detail is { } detail)
        {
            detail.Closed -= OnDetailClosed;
        }

        Detail = null;
        CloseEditor();
        await LoadAsync(CancellationToken.None);
    }

    public ObservableCollection<SalesPersonDto> SalesPeople { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenDetailCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial SalesPersonDto? SelectedSalesPerson { get; set; }

    [ObservableProperty]
    public partial SalesPersonEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedSalesPersonChanged(SalesPersonDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new SalesPersonEditorViewModel(value, create, update, _salesTerritoryOptions, _userOptions, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _salesTerritoryOptions = await listSalesTerritoryOptions.HandleAsync(cancellationToken);
        _userOptions = await references.UserOptionsAsync(cancellationToken);

        var response = await list.HandleAsync(new ListSalesPeople.Query(Search), cancellationToken);

        SalesPeople.Clear();
        foreach (var item in response.Items)
        {
            SalesPeople.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("SalesPeople_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("SalesPeople_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedSalesPerson = null;
        OpenEditor(new SalesPersonEditorViewModel(null, create, update, _salesTerritoryOptions, _userOptions, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedSalesPerson is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteSalesPerson.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedSalesPerson is not null;

    private void OpenEditor(SalesPersonEditorViewModel editor)
    {
        DetachEditor();
        Editor = editor;
        editor.Saved += OnEditorSaved;
        editor.Cancelled += OnEditorCancelled;
    }

    private void CloseEditor()
    {
        DetachEditor();
        Editor = null;
        SelectedSalesPerson = null;
        IsConfirmingDelete = false;
    }

    private void DetachEditor()
    {
        if (Editor is { } editor)
        {
            editor.Saved -= OnEditorSaved;
            editor.Cancelled -= OnEditorCancelled;
        }
    }

    // Event handler, so it has to be async void; LoadAsync reports its own outcome on the status line.
    private async void OnEditorSaved(object? sender, SalesPersonDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("SalesPeople_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}