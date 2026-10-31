using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

internal enum CvSection
{
    WorkExperience,
    Education,
    Course,
    Language,
}

/// <summary>Removes one entry from a section of the user's own CV.</summary>
internal static class DeleteCvSectionItem
{
    public sealed record Command(Guid CvId, Guid UserId, CvSection Section, Guid ItemId);

    public sealed class Handler(IDbContextFactory<CvsDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (!await db.OwnsCvAsync(command.CvId, command.UserId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("cv_not_found", "CV not found.");
            }

            var deleted = command.Section switch
            {
                CvSection.WorkExperience => await db.WorkExperiences.Where(w => w.Id == command.ItemId && w.CvId == command.CvId)
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false),
                CvSection.Education => await db.Educations.Where(e => e.Id == command.ItemId && e.CvId == command.CvId)
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false),
                CvSection.Course => await db.Courses.Where(c => c.Id == command.ItemId && c.CvId == command.CvId)
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false),
                CvSection.Language => await db.LanguageSkills.Where(l => l.Id == command.ItemId && l.CvId == command.CvId)
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false),
                _ => 0,
            };

            return deleted == 0 ? Error.NotFound("cv_item_not_found", "The entry was not found.") : command.ItemId;
        }
    }
}
