using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>Everything the CV's detail page shows: the CV, its owner's details and all its sections.</summary>
internal static class GetCvDetail
{
    public sealed record Query(Guid Id, Guid UserId);

    public sealed class Handler(IDbContextFactory<CvsDbContext> dbFactory, IUserLookup users, IEnumerable<IContactInfoSource> contactSources)
    {
        public Task<Result<CvDetailDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<CvDetailDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var cv = await db.Cvs.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == query.Id && c.UserId == query.UserId, cancellationToken)
                .ConfigureAwait(false);
            if (cv is null)
            {
                return Error.NotFound("cv_not_found", "CV not found.");
            }

            var work = await db.WorkExperiences.AsNoTracking().Where(w => w.CvId == cv.Id)
                .OrderByDescending(w => w.StartDate).ToListAsync(cancellationToken).ConfigureAwait(false);
            var education = await db.Educations.AsNoTracking().Where(e => e.CvId == cv.Id)
                .OrderByDescending(e => e.StartDate).ToListAsync(cancellationToken).ConfigureAwait(false);
            var courses = await db.Courses.AsNoTracking().Where(c => c.CvId == cv.Id)
                .OrderByDescending(c => c.CompletedYear).ThenBy(c => c.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
            var languages = await db.LanguageSkills.AsNoTracking().Where(l => l.CvId == cv.Id)
                .OrderByDescending(l => l.Level).ThenBy(l => l.Language).ToListAsync(cancellationToken).ConfigureAwait(false);

            var selected = await db.ContactSelections.AsNoTracking().Where(s => s.CvId == cv.Id).Select(s => s.ContactId)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var contacts = await ContactEntries.ListAsync(contactSources, cv.UserId, cancellationToken).ConfigureAwait(false);

            var user = await users.GetAsync(cv.UserId, cancellationToken).ConfigureAwait(false);

            return new CvDetailDto(
                CvDto.From(cv),
                user,
                cv.Photo,
                DriverLicenceCategories.Parse(cv.DriverLicence),
                work.ConvertAll(WorkExperienceDto.From),
                education.ConvertAll(EducationDto.From),
                courses.ConvertAll(CourseDto.From),
                languages.ConvertAll(LanguageSkillDto.From),
                contacts,
                selected);
        }
    }
}
