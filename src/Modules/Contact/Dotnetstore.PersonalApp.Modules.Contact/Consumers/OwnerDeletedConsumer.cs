using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.Addresses;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddresses;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Features.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.Contact.Consumers;

/// <summary>
/// Addresses, email addresses and phone numbers belong to the user, client, contact person, customer or student they were
/// entered for, so they go when that owner does. The deletes are idempotent, so a retry after a failure is safe.
/// </summary>
internal sealed class OwnerDeletedConsumer(
    DeleteAddressesForObject.Handler addresses,
    DeleteEmailAddressesForObject.Handler emailAddresses,
    DeletePhoneNumbersForObject.Handler phoneNumbers)
    : IIntegrationEventConsumer<UserDeleted>,
        IIntegrationEventConsumer<ClientDeleted>,
        IIntegrationEventConsumer<ContactPersonDeleted>,
        IIntegrationEventConsumer<CustomerDeleted>,
        IIntegrationEventConsumer<StudentDeleted>
{
    public Task ConsumeAsync(UserDeleted integrationEvent, CancellationToken cancellationToken) => DeleteAllAsync(integrationEvent.UserId, cancellationToken);

    public Task ConsumeAsync(ClientDeleted integrationEvent, CancellationToken cancellationToken) => DeleteAllAsync(integrationEvent.ClientId, cancellationToken);

    public Task ConsumeAsync(ContactPersonDeleted integrationEvent, CancellationToken cancellationToken) => DeleteAllAsync(integrationEvent.ContactPersonId, cancellationToken);

    public Task ConsumeAsync(CustomerDeleted integrationEvent, CancellationToken cancellationToken) => DeleteAllAsync(integrationEvent.CustomerId, cancellationToken);

    public Task ConsumeAsync(StudentDeleted integrationEvent, CancellationToken cancellationToken) => DeleteAllAsync(integrationEvent.StudentId, cancellationToken);

    private async Task DeleteAllAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        await addresses.HandleAsync(new DeleteAddressesForObject.Command(ownerId), cancellationToken).ConfigureAwait(false);
        await emailAddresses.HandleAsync(new DeleteEmailAddressesForObject.Command(ownerId), cancellationToken).ConfigureAwait(false);
        await phoneNumbers.HandleAsync(new DeletePhoneNumbersForObject.Command(ownerId), cancellationToken).ConfigureAwait(false);
    }
}
