using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>Changes one of the user's own CVs; someone else's CV is reported as not found.</summary>
internal static class UpdateCv
{
    public sealed record Command(Guid Id, Guid UserId, string Title, string? TargetJob, string? Summary, string? LinkedIn = null);

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

            var cv = await db.Cvs
                .FirstOrDefaultAsync(c => c.Id == command.Id && c.UserId == command.UserId, cancellationToken)
                .ConfigureAwait(false);
            if (cv is null)
            {
                return Error.NotFound("cv_not_found", "CV not found.");
            }

            var title = command.Title.Trim();
            if (await db.Cvs.AnyAsync(c => c.UserId == command.UserId && c.Title == title && c.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("cv_conflict", $"A CV titled '{title}' already exists.");
            }

            cv.Title = title;
            cv.TargetJob = CvRules.NormalizeOptional(command.TargetJob);
            cv.Summary = CvRules.NormalizeOptional(command.Summary);
            cv.LinkedIn = CvRules.NormalizeLinkedIn(command.LinkedIn);
            cv.UpdatedAt = clock.GetUtcNow();

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return CvDto.From(cv);
        }
    }
}
