using Dotnetstore.PersonalApp.Modules.Purchasing.Features.ProductVendors;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.Vendors;

/// <summary>A vendor with its prices and orders, and the contact details that other modules keep for it.</summary>
internal sealed class VendorDetailViewModel(
    VendorDto vendor,
    ProductVendorsViewModel prices,
    PurchaseOrdersViewModel purchaseOrders,
    IObjectAddressesPanelFactory? addressesFactory = null,
    IObjectEmailAddressesPanelFactory? emailAddressesFactory = null,
    IObjectPhoneNumbersPanelFactory? phoneNumbersFactory = null) : DetailViewModel
{
    public VendorDto Vendor { get; } = vendor;

    public ProductVendorsViewModel Prices { get; } = prices.ForParent(vendor.Id);

    public PurchaseOrdersViewModel PurchaseOrders { get; } = purchaseOrders.ForParent(vendor.Id);

    /// <summary>The addresses tab, provided by the Addresses module; null when that module is not installed.</summary>
    public ViewModelBase? Addresses { get; } = addressesFactory?.Create(vendor.Id);

    /// <summary>The email addresses tab, provided by the EmailAddresses module; null when that module is not installed.</summary>
    public ViewModelBase? EmailAddresses { get; } = emailAddressesFactory?.Create(vendor.Id);

    /// <summary>The phone numbers tab, provided by the PhoneNumbers module; null when that module is not installed.</summary>
    public ViewModelBase? PhoneNumbers { get; } = phoneNumbersFactory?.Create(vendor.Id);
}
