using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Currencies;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.CurrencyRates;

internal sealed partial class CurrencyRatesViewModel(
    ListCurrencyRates.Handler list,
    CreateCurrencyRate.Handler create,
    UpdateCurrencyRate.Handler update,
    DeleteCurrencyRate.Handler delete,
    ListCurrencyOptions.Handler listCurrencyOptions,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _currencyOptions = [];

    public ObservableCollection<CurrencyRateDto> CurrencyRates { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial CurrencyRateDto? SelectedCurrencyRate { get; set; }

    [ObservableProperty]
    public partial CurrencyRateEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedCurrencyRateChanged(CurrencyRateDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new CurrencyRateEditorViewModel(value, create, update, _currencyOptions, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _currencyOptions = await listCurrencyOptions.HandleAsync(cancellationToken);

        var response = await list.HandleAsync(new ListCurrencyRates.Query(Search), cancellationToken);

        CurrencyRates.Clear();
        foreach (var item in response.Items)
        {
            CurrencyRates.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("CurrencyRates_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("CurrencyRates_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedCurrencyRate = null;
        OpenEditor(new CurrencyRateEditorViewModel(null, create, update, _currencyOptions, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedCurrencyRate is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteCurrencyRate.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedCurrencyRate is not null;

    private void OpenEditor(CurrencyRateEditorViewModel editor)
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
        SelectedCurrencyRate = null;
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
    private async void OnEditorSaved(object? sender, CurrencyRateDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("CurrencyRates_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}