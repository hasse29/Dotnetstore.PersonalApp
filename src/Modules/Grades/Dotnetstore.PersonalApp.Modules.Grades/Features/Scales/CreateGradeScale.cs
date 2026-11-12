using Dotnetstore.PersonalApp.Modules.Grades.Domain;
using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;

internal static class CreateGradeScale
{
    public sealed record Command(string Name, string? Country, string? Description, IReadOnlyList<GradeScaleItemInput> Items);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NameRules();
            RuleFor(c => c.Country).CountryRules();
            RuleFor(c => c.Description).DescriptionRules();
            RuleFor(c => c.Items).ItemsRules();
        }
    }

    public sealed class Handler(IDbContextFactory<GradesDbContext> dbFactory, IValidator<Command> validator, TimeProvider clock)
    {
        public Task<Result<GradeScaleDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<GradeScaleDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var scale = new GradeScale
            {
                Id = Guid.CreateVersion7(),
                Name = command.Name.Trim(),
                Country = GradeScaleRules.NormalizeOptional(command.Country),
                Description = GradeScaleRules.NormalizeOptional(command.Description),
                CreatedAt = clock.GetUtcNow(),
                Items = [.. command.Items.Select(i => new GradeScaleItem
                {
                    Id = Guid.CreateVersion7(),
                    Label = i.Label.Trim(),
                    Rank = i.Rank,
                    Description = GradeScaleRules.NormalizeOptional(i.Description),
                    IsPassing = i.IsPassing,
                })],
            };

            db.GradeScales.Add(scale);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return GradeScaleDto.From(scale);
        }
    }
}
