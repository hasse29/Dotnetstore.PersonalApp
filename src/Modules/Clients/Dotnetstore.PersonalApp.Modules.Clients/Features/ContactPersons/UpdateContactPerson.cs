using Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;
using Dotnetstore.PersonalApp.Modules.Clients.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;

internal static class UpdateContactPerson
{
    public sealed record Command(Guid Id, string FirstName, string LastName, string? JobTitle, string? Notes);

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

    public sealed class Handler(IDbContextFactory<ClientsDbContext> dbFactory, IValidator<Command> validator)
    {
        public Task<Result<ContactPersonDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<ContactPersonDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var person = await db.ContactPersons.FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (person is null)
            {
                return Error.NotFound("contact_person_not_found", "Contact person not found.");
            }

            person.FirstName = command.FirstName.Trim();
            person.LastName = command.LastName.Trim();
            person.JobTitle = ClientRules.NormalizeOptional(command.JobTitle);
            person.Notes = ClientRules.NormalizeOptional(command.Notes);

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ContactPersonDto.From(person);
        }
    }
}
