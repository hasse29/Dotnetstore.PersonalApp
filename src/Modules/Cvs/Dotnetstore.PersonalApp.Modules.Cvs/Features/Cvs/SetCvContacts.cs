using Dotnetstore.PersonalApp.Modules.Cvs.Domain;
using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>
/// Chooses which of the user's addresses, phone numbers and email addresses appear on one of their CVs. Ids that do not
/// belong to the user are ignored, so a CV can never show someone else's details.
/// </summary>
internal static class SetCvContacts
{
    public sealed record Command(Guid CvId, Guid UserId, IReadOnlyList<Guid> ContactIds);

    public sealed class Handler(IDbContextFactory<CvsDbContext> dbFactory, IEnumerable<IContactInfoSource> sources)
    {
        public Task<Result<IReadOnlyList<Guid>>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<IReadOnlyList<Guid>>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (!await db.Cvs.AnyAsync(c => c.Id == command.CvId && c.UserId == command.UserId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("cv_not_found", "CV not found.");
            }

            var available = (await ContactEntries.ListAsync(sources, command.UserId, cancellationToken).ConfigureAwait(false))
                .Select(e => e.Id)
                .ToHashSet();
            var wanted = command.ContactIds.Where(available.Contains).Distinct().ToList();

            var existing = await db.ContactSelections.Where(s => s.CvId == command.CvId).ToListAsync(cancellationToken).ConfigureAwait(false);
            db.ContactSelections.RemoveRange(existing.Where(s => !wanted.Contains(s.ContactId)));
            db.ContactSelections.AddRange(wanted
                .Where(id => existing.All(s => s.ContactId != id))
                .Select(id => new CvContactSelection { Id = Guid.CreateVersion7(), CvId = command.CvId, ContactId = id }));
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result<IReadOnlyList<Guid>>.Success(wanted);
        }
    }
}
