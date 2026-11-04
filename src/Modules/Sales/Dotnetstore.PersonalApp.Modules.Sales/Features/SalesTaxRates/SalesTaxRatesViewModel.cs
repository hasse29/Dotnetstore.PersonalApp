using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTaxRates;

internal sealed partial class SalesTaxRatesViewModel(
    ListSalesTaxRates.Handler list,
    CreateSalesTaxRate.Handler create,
    UpdateSalesTaxRate.Handler update,
    DeleteSalesTaxRate.Handler delete,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<SalesTaxRateDto> SalesTaxRates { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial SalesTaxRateDto? SelectedSalesTaxRate { get; set; }

    [ObservableProperty]
    public partial SalesTaxRateEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedSalesTaxRateChanged(SalesTaxRateDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new SalesTaxRateEditorViewModel(value, create, update, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListSalesTaxRates.Query(Search), cancellationToken);

        SalesTaxRates.Clear();
        foreach (var item in response.Items)
        {
            SalesTaxRates.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("SalesTaxRates_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("SalesTaxRates_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedSalesTaxRate = null;
        OpenEditor(new SalesTaxRateEditorViewModel(null, create, update, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedSalesTaxRate is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteSalesTaxRate.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedSalesTaxRate is not null;

    private void OpenEditor(SalesTaxRateEditorViewModel editor)
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
        SelectedSalesTaxRate = null;
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
    private async void OnEditorSaved(object? sender, SalesTaxRateDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("SalesTaxRates_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}