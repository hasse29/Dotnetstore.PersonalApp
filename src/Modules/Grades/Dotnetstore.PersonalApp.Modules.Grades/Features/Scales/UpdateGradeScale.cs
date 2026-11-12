using Dotnetstore.PersonalApp.Modules.Grades.Domain;
using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;

internal static class UpdateGradeScale
{
    public sealed record Command(Guid Id, string Name, string? Country, string? Description, IReadOnlyList<GradeScaleItemInput> Items);

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

    public sealed class Handler(IDbContextFactory<GradesDbContext> dbFactory, IValidator<Command> validator)
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

            var scale = await db.GradeScales.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (scale is null)
            {
                return Error.NotFound("grade_scale_not_found", "Grade scale not found.");
            }

            var existing = scale.Items.ToDictionary(i => i.Id);
            if (command.Items.Any(i => i.Id is { } id && !existing.ContainsKey(id)))
            {
                return Error.NotFound("grade_item_not_found", "Grade not found.");
            }

            // Steps left out of the editor are removed, but not while a grade still uses them.
            var keptIds = command.Items.Where(i => i.Id is not null).Select(i => i.Id!.Value).ToHashSet();
            var removed = scale.Items.Where(i => !keptIds.Contains(i.Id)).ToList();
            if (removed.Count > 0)
            {
                var removedIds = removed.Select(i => i.Id).ToList();
                if (await db.Grades.AnyAsync(g => removedIds.Contains(g.ScaleItemId), cancellationToken).ConfigureAwait(false)
                    || await db.TestResults.AnyAsync(r => r.ScaleItemId != null && removedIds.Contains(r.ScaleItemId.Value), cancellationToken).ConfigureAwait(false))
                {
                    return Error.Conflict("grade_item_in_use", "A grade that is in use cannot be removed.");
                }

                db.GradeScaleItems.RemoveRange(removed);
            }

            scale.Name = command.Name.Trim();
            scale.Country = GradeScaleRules.NormalizeOptional(command.Country);
            scale.Description = GradeScaleRules.NormalizeOptional(command.Description);

            foreach (var input in command.Items)
            {
                if (input.Id is { } id)
                {
                    var item = existing[id];
                    item.Label = input.Label.Trim();
                    item.Rank = input.Rank;
                    item.Description = GradeScaleRules.NormalizeOptional(input.Description);
                    item.IsPassing = input.IsPassing;
                }
                else
                {
                    // Added through the set: a new entity with a key already filled in would otherwise be tracked as an existing row.
                    db.GradeScaleItems.Add(new GradeScaleItem
                    {
                        Id = Guid.CreateVersion7(),
                        ScaleId = scale.Id,
                        Label = input.Label.Trim(),
                        Rank = input.Rank,
                        Description = GradeScaleRules.NormalizeOptional(input.Description),
                        IsPassing = input.IsPassing,
                    });
                }
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return GradeScaleDto.From(scale);
        }
    }
}
