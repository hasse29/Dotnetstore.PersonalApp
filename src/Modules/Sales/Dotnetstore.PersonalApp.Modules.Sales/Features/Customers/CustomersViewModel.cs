using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal sealed partial class CustomersViewModel(
    ListCustomers.Handler list,
    CreateCustomer.Handler create,
    UpdateCustomer.Handler update,
    DeleteCustomer.Handler delete,
    ILocalizationService localization,
    IObjectAddressesPanelFactory? addressesFactory = null,
    IObjectEmailAddressesPanelFactory? emailAddressesFactory = null,
    IObjectPhoneNumbersPanelFactory? phoneNumbersFactory = null,
    CustomerPhotoPanelFactory? photoFactory = null,
    GetCustomerSalesHistory.Handler? salesHistory = null) : ViewModelBase
{
    public ObservableCollection<CustomerRow> Customers { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenDetailCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial CustomerRow? SelectedCustomer { get; set; }

    [ObservableProperty]
    public partial CustomerEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial CustomerDetailViewModel? Detail { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedCustomerChanged(CustomerRow? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new CustomerEditorViewModel(value.Customer, create, update, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListCustomers.Query(Search), cancellationToken);

        Customers.Clear();
        foreach (var customer in response.Items)
        {
            Customers.Add(new CustomerRow(customer, localization.Get(customer.IsActive ? "Customers_Active" : "Customers_Inactive")));
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("Customers_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("Customers_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedCustomer = null;
        OpenEditor(new CustomerEditorViewModel(null, create, update, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenDetail()
    {
        if (SelectedCustomer is not { } row)
        {
            return;
        }

        var customer = row.Customer;
        var detail = new CustomerDetailViewModel(customer, row.StatusText, addressesFactory, emailAddressesFactory, phoneNumbersFactory,
            photoFactory is null ? null : initials => photoFactory.Create(customer.Id, initials), salesHistory, localization);
        detail.Closed += OnDetailClosed;
        detail.EditRequested += OnDetailClosed;
        Detail = detail;
    }

    // Both Back and Edit return to the list; the selected customer's editor is already open there.
    private void OnDetailClosed(object? sender, EventArgs e)
    {
        if (Detail is { } detail)
        {
            detail.Closed -= OnDetailClosed;
            detail.EditRequested -= OnDetailClosed;
        }

        Detail = null;

        // The photo may have changed, and the list shows its thumbnail.
        LoadCommand.Execute(null);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedCustomer is not { } row)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteCustomer.Command(row.Customer.Id), cancellationToken);

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
        StatusMessage = localization.Format("Customers_Saved", saved.Name);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
