using Dotnetstore.PersonalApp.Modules.Cvs.Domain;
using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

internal static class CreateCv
{
    public sealed record Command(Guid UserId, string Title, string? TargetJob, string? Summary, string? LinkedIn = null);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.UserId).UserIdRules();
            RuleFor(c => c.Title).TitleRules();
            RuleFor(c => c.TargetJob).TargetJobRules();
            RuleFor(c => c.Summary).SummaryRules();
            RuleFor(c => c.LinkedIn).LinkedInRules();
        }
    }

    public sealed class Handler(IDbContextFactory<CvsDbContext> dbFactory, IValidator<Command> validator, TimeProvider clock)
    {
        public Task<Result<CvDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<CvDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var title = command.Title.Trim();
            if (await db.Cvs.AnyAsync(c => c.UserId == command.UserId && c.Title == title, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("cv_conflict", $"A CV titled '{title}' already exists.");
            }

            var now = clock.GetUtcNow();
            var cv = new Cv
            {
                Id = Guid.CreateVersion7(),
                UserId = command.UserId,
                Title = title,
                TargetJob = CvRules.NormalizeOptional(command.TargetJob),
                Summary = CvRules.NormalizeOptional(command.Summary),
                LinkedIn = CvRules.NormalizeLinkedIn(command.LinkedIn),
                CreatedAt = now,
                UpdatedAt = now,
            };

            db.Cvs.Add(cv);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return CvDto.From(cv);
        }
    }
}
