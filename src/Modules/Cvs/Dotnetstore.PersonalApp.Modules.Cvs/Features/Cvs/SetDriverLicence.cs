using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>Replaces the driver licence categories on the user's own CV; an empty list means no licence.</summary>
internal static class SetDriverLicence
{
    public sealed record Command(Guid CvId, Guid UserId, IReadOnlyList<string> Categories);

    public sealed class Handler(IDbContextFactory<CvsDbContext> dbFactory)
    {
        public async Task<Result<IReadOnlyList<string>>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            if (command.Categories.Any(c => !DriverLicenceCategories.All.Contains(c, StringComparer.OrdinalIgnoreCase)))
            {
                return Error.Validation("cv_driver_licence_invalid", "Unknown driver licence category.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var cv = await db.Cvs.FirstOrDefaultAsync(c => c.Id == command.CvId && c.UserId == command.UserId, cancellationToken).ConfigureAwait(false);
            if (cv is null)
            {
                return Error.NotFound("cv_not_found", "CV not found.");
            }

            cv.DriverLicence = DriverLicenceCategories.Join(command.Categories);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result<IReadOnlyList<string>>.Success(DriverLicenceCategories.Parse(cv.DriverLicence));
        }
    }
}
