using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Currencies;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.CountryRegionCurrencies;

internal sealed partial class CountryRegionCurrenciesViewModel(
    ListCountryRegionCurrencies.Handler list,
    CreateCountryRegionCurrency.Handler create,
    UpdateCountryRegionCurrency.Handler update,
    DeleteCountryRegionCurrency.Handler delete,
    ListCurrencyOptions.Handler listCurrencyOptions,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _currencyOptions = [];

    public ObservableCollection<CountryRegionCurrencyDto> CountryRegionCurrencies { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial CountryRegionCurrencyDto? SelectedCountryRegionCurrency { get; set; }

    [ObservableProperty]
    public partial CountryRegionCurrencyEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedCountryRegionCurrencyChanged(CountryRegionCurrencyDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new CountryRegionCurrencyEditorViewModel(value, create, update, _currencyOptions, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _currencyOptions = await listCurrencyOptions.HandleAsync(cancellationToken);

        var response = await list.HandleAsync(new ListCountryRegionCurrencies.Query(Search), cancellationToken);

        CountryRegionCurrencies.Clear();
        foreach (var item in response.Items)
        {
            CountryRegionCurrencies.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("CountryRegionCurrencies_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("CountryRegionCurrencies_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedCountryRegionCurrency = null;
        OpenEditor(new CountryRegionCurrencyEditorViewModel(null, create, update, _currencyOptions, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedCountryRegionCurrency is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteCountryRegionCurrency.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedCountryRegionCurrency is not null;

    private void OpenEditor(CountryRegionCurrencyEditorViewModel editor)
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
        SelectedCountryRegionCurrency = null;
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
    private async void OnEditorSaved(object? sender, CountryRegionCurrencyDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("CountryRegionCurrencies_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}