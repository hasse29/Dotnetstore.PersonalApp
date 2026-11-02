using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.TimeReports;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;

internal sealed partial class ClientDetailViewModel(ClientDto client, IObjectAddressesPanelFactory? addressesFactory = null, IObjectEmailAddressesPanelFactory? emailAddressesFactory = null, IObjectPhoneNumbersPanelFactory? phoneNumbersFactory = null, ITimeReportsPanelFactory? timeReportsFactory = null, ContactPersonsViewModel? contactPersons = null) : ViewModelBase
{
    public ClientDto Client { get; } = client;

    /// <summary>The client's contact persons tab.</summary>
    public ContactPersonsViewModel? ContactPersons { get; } = contactPersons;

    /// <summary>The client's addresses tab, provided by the Addresses module; null when that module is not installed.</summary>
    public ViewModelBase? Addresses { get; } = addressesFactory?.Create(client.Id);

    /// <summary>The email addresses tab, provided by the EmailAddresses module; null when that module is not installed.</summary>
    public ViewModelBase? EmailAddresses { get; } = emailAddressesFactory?.Create(client.Id);

    /// <summary>The phone numbers tab, provided by the PhoneNumbers module; null when that module is not installed.</summary>
    public ViewModelBase? PhoneNumbers { get; } = phoneNumbersFactory?.Create(client.Id);

    /// <summary>The time reports tab, provided by the TimeReports module; null when that module is not installed.</summary>
    public ViewModelBase? TimeReports { get; } = timeReportsFactory?.CreateForClient(client.Id);

    /// <summary>Shown in the logo slot until real logos are supported.</summary>
    public string Initials => string.Concat(
        Client.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0])));

    public event EventHandler? Closed;

    public event EventHandler? EditRequested;

    [RelayCommand]
    private void Back() => Closed?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void Edit() => EditRequested?.Invoke(this, EventArgs.Empty);
}
