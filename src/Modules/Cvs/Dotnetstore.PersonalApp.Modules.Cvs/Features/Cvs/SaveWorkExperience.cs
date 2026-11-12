using Dotnetstore.PersonalApp.Modules.Cvs.Domain;
using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>Adds a job to the user's own CV, or changes one when <see cref="Command.Id"/> is given.</summary>
internal static class SaveWorkExperience
{
    public sealed record Command(Guid CvId, Guid UserId, Guid? Id, string Employer, string Role, DateOnly StartDate, DateOnly? EndDate, string? Description, IReadOnlyList<string> Technologies);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.CvId).CvIdRules();
            RuleFor(c => c.Employer).RequiredText("cv_employer_required", "cv_employer_too_long", CvSectionRules.MaxNameLength);
            RuleFor(c => c.Role).RequiredText("cv_role_required", "cv_role_too_long", CvSectionRules.MaxNameLength);
            RuleFor(c => c.StartDate).StartDateRules();
            RuleFor(c => c.EndDate).EndDateRules(c => c.StartDate);
            RuleFor(c => c.Description).OptionalText("cv_description_too_long", CvSectionRules.MaxDescriptionLength);
            RuleFor(c => c.Technologies)
                .Must(t => TechnologyTags.Normalize(t).Count <= TechnologyTags.MaxTags)
                .WithErrorCode("cv_technologies_too_many").WithMessage("Too many technologies.")
                .Must(t => TechnologyTags.Normalize(t).All(tag => tag.Length <= TechnologyTags.MaxTagLength))
                .WithErrorCode("cv_technology_too_long").WithMessage("A technology is too long.");
        }
    }

    public sealed class Handler(IDbContextFactory<CvsDbContext> dbFactory, IValidator<Command> validator)
    {
        public Task<Result<WorkExperienceDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<WorkExperienceDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (!await db.OwnsCvAsync(command.CvId, command.UserId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("cv_not_found", "CV not found.");
            }

            WorkExperience job;
            if (command.Id is { } id)
            {
                var existing = await db.WorkExperiences.FirstOrDefaultAsync(w => w.Id == id && w.CvId == command.CvId, cancellationToken).ConfigureAwait(false);
                if (existing is null)
                {
                    return Error.NotFound("cv_item_not_found", "The entry was not found.");
                }

                job = existing;
            }
            else
            {
                job = new WorkExperience { Id = Guid.CreateVersion7(), CvId = command.CvId };
                db.WorkExperiences.Add(job);
            }

            job.Employer = command.Employer.Trim();
            job.Role = command.Role.Trim();
            job.StartDate = command.StartDate;
            job.EndDate = command.EndDate;
            job.Description = CvRules.NormalizeOptional(command.Description);
            job.Technologies = TechnologyTags.Join(command.Technologies);

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return WorkExperienceDto.From(job);
        }
    }
}
