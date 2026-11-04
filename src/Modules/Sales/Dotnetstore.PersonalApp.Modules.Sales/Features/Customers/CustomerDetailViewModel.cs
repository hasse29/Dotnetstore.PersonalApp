using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

/// <summary>A customer with its orders, and the contact details that other modules keep for it.</summary>
internal sealed class CustomerDetailViewModel(
    CustomerDto customer,
    SalesOrdersViewModel salesOrders,
    IObjectAddressesPanelFactory? addressesFactory = null,
    IObjectEmailAddressesPanelFactory? emailAddressesFactory = null,
    IObjectPhoneNumbersPanelFactory? phoneNumbersFactory = null) : DetailViewModel
{
    public CustomerDto Customer { get; } = customer;

    public SalesOrdersViewModel SalesOrders { get; } = salesOrders.ForParent(customer.Id);

    /// <summary>The addresses tab, provided by the Addresses module; null when that module is not installed.</summary>
    public ViewModelBase? Addresses { get; } = addressesFactory?.Create(customer.Id);

    /// <summary>The email addresses tab, provided by the EmailAddresses module; null when that module is not installed.</summary>
    public ViewModelBase? EmailAddresses { get; } = emailAddressesFactory?.Create(customer.Id);

    /// <summary>The phone numbers tab, provided by the PhoneNumbers module; null when that module is not installed.</summary>
    public ViewModelBase? PhoneNumbers { get; } = phoneNumbersFactory?.Create(customer.Id);
}
