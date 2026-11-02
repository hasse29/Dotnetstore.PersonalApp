using Dotnetstore.PersonalApp.Modules.Clients.Domain;
using Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;
using Dotnetstore.PersonalApp.Modules.Clients.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;

internal static class CreateContactPerson
{
    public sealed record Command(Guid ClientId, string FirstName, string LastName, string? JobTitle, string? Notes);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.FirstName).FirstNameRules();
            RuleFor(c => c.LastName).LastNameRules();
            RuleFor(c => c.JobTitle).JobTitleRules();
            RuleFor(c => c.Notes).NotesRules();
        }
    }

    public sealed class Handler(
        IDbContextFactory<ClientsDbContext> dbFactory,
        IValidator<Command> validator,
        TimeProvider clock)
    {
        public async Task<Result<ContactPersonDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (!await db.Clients.AnyAsync(c => c.Id == command.ClientId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("client_not_found", "Client not found.");
            }

            var person = new ContactPerson
            {
                Id = Guid.CreateVersion7(),
                ClientId = command.ClientId,
                FirstName = command.FirstName.Trim(),
                LastName = command.LastName.Trim(),
                JobTitle = ClientRules.NormalizeOptional(command.JobTitle),
                Notes = ClientRules.NormalizeOptional(command.Notes),
                CreatedAt = clock.GetUtcNow(),
            };

            db.ContactPersons.Add(person);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ContactPersonDto.From(person);
        }
    }
}
