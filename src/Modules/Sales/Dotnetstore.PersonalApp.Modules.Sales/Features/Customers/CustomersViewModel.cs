using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritories;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Stores;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal sealed partial class CustomersViewModel(
    ListCustomers.Handler list,
    CreateCustomer.Handler create,
    UpdateCustomer.Handler update,
    DeleteCustomer.Handler delete,
    IDetailFactory<CustomerDto> detailFactory,
    ListStoreOptions.Handler listStoreOptions,
    ListSalesTerritoryOptions.Handler listSalesTerritoryOptions,
    ExternalReferences references,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _storeOptions = [];
    private IReadOnlyList<OptionDto> _salesTerritoryOptions = [];
    private IReadOnlyList<OptionDto> _clientOptions = [];

    [ObservableProperty]
    public partial DetailViewModel? Detail { get; set; }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenDetail()
    {
        if (SelectedCustomer is not { } selected)
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

    public ObservableCollection<CustomerDto> Customers { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenDetailCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial CustomerDto? SelectedCustomer { get; set; }

    [ObservableProperty]
    public partial CustomerEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedCustomerChanged(CustomerDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new CustomerEditorViewModel(value, create, update, _storeOptions, _salesTerritoryOptions, _clientOptions, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _storeOptions = await listStoreOptions.HandleAsync(cancellationToken);
        _salesTerritoryOptions = await listSalesTerritoryOptions.HandleAsync(cancellationToken);
        _clientOptions = await references.ClientOptionsAsync(cancellationToken);

        var response = await list.HandleAsync(new ListCustomers.Query(Search), cancellationToken);

        Customers.Clear();
        foreach (var item in response.Items)
        {
            Customers.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("Customers_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("Customers_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedCustomer = null;
        OpenEditor(new CustomerEditorViewModel(null, create, update, _storeOptions, _salesTerritoryOptions, _clientOptions, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedCustomer is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteCustomer.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedCustomer is not null;

    private void OpenEditor(CustomerEditorViewModel editor)
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
        SelectedCustomer = null;
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
    private async void OnEditorSaved(object? sender, CustomerDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("Customers_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}