using Dotnetstore.PersonalApp.Modules.Clients.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;

internal static class UpdateClient
{
    public sealed record Command(
        Guid Id,
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

    public sealed class Handler(IDbContextFactory<ClientsDbContext> dbFactory, IValidator<Command> validator)
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

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (client is null)
            {
                return Error.NotFound("client_not_found", "Client not found.");
            }

            var organizationNumber = ClientRules.NormalizeOrganizationNumber(command.OrganizationNumber);
            if (await db.Clients.AnyAsync(c => c.OrganizationNumber == organizationNumber && c.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("client_conflict", $"A client with organization number '{organizationNumber}' already exists.");
            }

            client.Name = command.Name.Trim();
            client.OrganizationNumber = organizationNumber;
            client.VatNumber = ClientRules.NormalizeOptional(command.VatNumber);
            client.Email = ClientRules.NormalizeOptional(command.Email);
            client.Phone = ClientRules.NormalizeOptional(command.Phone);
            client.Website = ClientRules.NormalizeOptional(command.Website);
            client.Notes = ClientRules.NormalizeOptional(command.Notes);

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ClientDto.From(client);
        }
    }
}
