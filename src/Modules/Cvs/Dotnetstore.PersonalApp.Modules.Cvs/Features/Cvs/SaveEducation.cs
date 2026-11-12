using Dotnetstore.PersonalApp.Modules.Cvs.Domain;
using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>Adds an education to the user's own CV, or changes one when <see cref="Command.Id"/> is given.</summary>
internal static class SaveEducation
{
    public sealed record Command(Guid CvId, Guid UserId, Guid? Id, string School, string Programme, DateOnly StartDate, DateOnly? EndDate, string? Description);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.CvId).CvIdRules();
            RuleFor(c => c.School).RequiredText("cv_school_required", "cv_school_too_long", CvSectionRules.MaxNameLength);
            RuleFor(c => c.Programme).RequiredText("cv_programme_required", "cv_programme_too_long", CvSectionRules.MaxNameLength);
            RuleFor(c => c.StartDate).StartDateRules();
            RuleFor(c => c.EndDate).EndDateRules(c => c.StartDate);
            RuleFor(c => c.Description).OptionalText("cv_description_too_long", CvSectionRules.MaxDescriptionLength);
        }
    }

    public sealed class Handler(IDbContextFactory<CvsDbContext> dbFactory, IValidator<Command> validator)
    {
        public Task<Result<EducationDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<EducationDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
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

            Education education;
            if (command.Id is { } id)
            {
                var existing = await db.Educations.FirstOrDefaultAsync(e => e.Id == id && e.CvId == command.CvId, cancellationToken).ConfigureAwait(false);
                if (existing is null)
                {
                    return Error.NotFound("cv_item_not_found", "The entry was not found.");
                }

                education = existing;
            }
            else
            {
                education = new Education { Id = Guid.CreateVersion7(), CvId = command.CvId };
                db.Educations.Add(education);
            }

            education.School = command.School.Trim();
            education.Programme = command.Programme.Trim();
            education.StartDate = command.StartDate;
            education.EndDate = command.EndDate;
            education.Description = CvRules.NormalizeOptional(command.Description);

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return EducationDto.From(education);
        }
    }
}
