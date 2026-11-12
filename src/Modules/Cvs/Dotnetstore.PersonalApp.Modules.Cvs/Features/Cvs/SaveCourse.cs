using Dotnetstore.PersonalApp.Modules.Cvs.Domain;
using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>Adds a short course to the user's own CV, or changes one when <see cref="Command.Id"/> is given.</summary>
internal static class SaveCourse
{
    public sealed record Command(Guid CvId, Guid UserId, Guid? Id, string Name, string? Provider, int? CompletedYear);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator(TimeProvider clock)
        {
            RuleFor(c => c.CvId).CvIdRules();
            RuleFor(c => c.Name).RequiredText("cv_course_name_required", "cv_course_name_too_long", CvSectionRules.MaxNameLength);
            RuleFor(c => c.Provider).OptionalText("cv_provider_too_long", CvSectionRules.MaxNameLength);
            RuleFor(c => c.CompletedYear)
                .Must(year => year is null || (year >= CvSectionRules.MinCourseYear && year <= clock.GetUtcNow().Year))
                .WithErrorCode("cv_course_year_invalid").WithMessage("The year is not valid.");
        }
    }

    public sealed class Handler(IDbContextFactory<CvsDbContext> dbFactory, IValidator<Command> validator)
    {
        public Task<Result<CourseDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<CourseDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
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

            Course course;
            if (command.Id is { } id)
            {
                var existing = await db.Courses.FirstOrDefaultAsync(c => c.Id == id && c.CvId == command.CvId, cancellationToken).ConfigureAwait(false);
                if (existing is null)
                {
                    return Error.NotFound("cv_item_not_found", "The entry was not found.");
                }

                course = existing;
            }
            else
            {
                course = new Course { Id = Guid.CreateVersion7(), CvId = command.CvId };
                db.Courses.Add(course);
            }

            course.Name = command.Name.Trim();
            course.Provider = CvRules.NormalizeOptional(command.Provider);
            course.CompletedYear = command.CompletedYear;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return CourseDto.From(course);
        }
    }
}
