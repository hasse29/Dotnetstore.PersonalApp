using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Consumers;

/// <summary>A deleted user's addresses are of no use to anyone, so they go with the user.</summary>
internal sealed class UserDeletedConsumer(DeletePhoneNumbersForObject.Handler deleteAll) : IIntegrationEventConsumer<UserDeleted>
{
    public async Task ConsumeAsync(UserDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteAll.HandleAsync(new DeletePhoneNumbersForObject.Command(integrationEvent.UserId), cancellationToken).ConfigureAwait(false);
}

internal sealed class ClientDeletedConsumer(DeletePhoneNumbersForObject.Handler deleteAll) : IIntegrationEventConsumer<ClientDeleted>
{
    public async Task ConsumeAsync(ClientDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteAll.HandleAsync(new DeletePhoneNumbersForObject.Command(integrationEvent.ClientId), cancellationToken).ConfigureAwait(false);
}

internal sealed class ContactPersonDeletedConsumer(DeletePhoneNumbersForObject.Handler deleteAll) : IIntegrationEventConsumer<ContactPersonDeleted>
{
    public async Task ConsumeAsync(ContactPersonDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteAll.HandleAsync(new DeletePhoneNumbersForObject.Command(integrationEvent.ContactPersonId), cancellationToken).ConfigureAwait(false);
}

internal sealed class StudentDeletedConsumer(DeletePhoneNumbersForObject.Handler deleteAll) : IIntegrationEventConsumer<StudentDeleted>
{
    public async Task ConsumeAsync(StudentDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteAll.HandleAsync(new DeletePhoneNumbersForObject.Command(integrationEvent.StudentId), cancellationToken).ConfigureAwait(false);
}
