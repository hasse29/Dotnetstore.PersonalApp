using Dotnetstore.PersonalApp.Modules.Clients.Domain;
using Dotnetstore.PersonalApp.Modules.Clients.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;

internal static class CreateClient
{
    public sealed record Command(
        string Name,
        string OrganizationNumber,
        string? VatNumber,
        string? Email,
        string? Phone,
        string? Website,
        string? Notes);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NameRules();
            RuleFor(c => c.OrganizationNumber).OrganizationNumberRules();
            RuleFor(c => c.VatNumber).VatNumberRules();
            RuleFor(c => c.Email).EmailRules().When(c => !string.IsNullOrWhiteSpace(c.Email));
            RuleFor(c => c.Phone).PhoneRules();
            RuleFor(c => c.Website).WebsiteRules().When(c => !string.IsNullOrWhiteSpace(c.Website));
            RuleFor(c => c.Notes).NotesRules();
        }
    }

    public sealed class Handler(
        IDbContextFactory<ClientsDbContext> dbFactory,
        IValidator<Command> validator,
        TimeProvider clock)
    {
        public Task<Result<ClientDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<ClientDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var organizationNumber = ClientRules.NormalizeOrganizationNumber(command.OrganizationNumber);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.Clients.AnyAsync(c => c.OrganizationNumber == organizationNumber, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("client_conflict", $"A client with organization number '{organizationNumber}' already exists.");
            }

            var client = new Client
            {
                Id = Guid.CreateVersion7(),
                Name = command.Name.Trim(),
                OrganizationNumber = organizationNumber,
                VatNumber = ClientRules.NormalizeOptional(command.VatNumber),
                Email = ClientRules.NormalizeOptional(command.Email),
                Phone = ClientRules.NormalizeOptional(command.Phone),
                Website = ClientRules.NormalizeOptional(command.Website),
                Notes = ClientRules.NormalizeOptional(command.Notes),
                CreatedAt = clock.GetUtcNow(),
            };

            db.Clients.Add(client);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ClientDto.From(client);
        }
    }
}
