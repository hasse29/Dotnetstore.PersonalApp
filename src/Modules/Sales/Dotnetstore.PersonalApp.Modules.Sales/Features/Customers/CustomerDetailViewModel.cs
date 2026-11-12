using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.Photos;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal sealed partial class CustomerDetailViewModel(
    CustomerDto customer,
    string statusText,
    IObjectAddressesPanelFactory? addressesFactory = null,
    IObjectEmailAddressesPanelFactory? emailAddressesFactory = null,
    IObjectPhoneNumbersPanelFactory? phoneNumbersFactory = null,
    Func<string, PhotoPanelViewModel?>? photoFactory = null) : ViewModelBase
{
    public CustomerDto Customer { get; } = customer;

    public string StatusText { get; } = statusText;

    /// <summary>The photo with its Change and Remove buttons; null when the host has no file dialog.</summary>
    public PhotoPanelViewModel? Photo { get; } = photoFactory?.Invoke(CustomerDto.InitialsOf(customer.Name));

    /// <summary>The customer's addresses tab, provided by the Addresses module; null when that module is not installed.</summary>
    public ViewModelBase? Addresses { get; } = addressesFactory?.Create(customer.Id);

    /// <summary>The email addresses tab, provided by the EmailAddresses module; null when that module is not installed.</summary>
    public ViewModelBase? EmailAddresses { get; } = emailAddressesFactory?.Create(customer.Id);

    /// <summary>The phone numbers tab, provided by the PhoneNumbers module; null when that module is not installed.</summary>
    public ViewModelBase? PhoneNumbers { get; } = phoneNumbersFactory?.Create(customer.Id);

    public event EventHandler? Closed;

    public event EventHandler? EditRequested;

    [RelayCommand]
    private void Back() => Closed?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void Edit() => EditRequested?.Invoke(this, EventArgs.Empty);
}
