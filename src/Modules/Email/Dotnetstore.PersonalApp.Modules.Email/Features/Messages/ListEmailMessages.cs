using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>Lists the stored messages of one account the user may use, newest first, without their bodies.</summary>
internal static class ListEmailMessages
{
    public const int MaxPageSize = 50;

    public sealed record Query(Guid UserId, Guid AccountId, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<EmailMessageDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<EmailDbContext> dbFactory)
    {
        public async Task<Result<Response>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var found = await EmailAccountAccess.FindAsync(db, query.AccountId, query.UserId, cancellationToken).ConfigureAwait(false);
            if (!found.IsSuccess)
            {
                return found.Error;
            }

            var messages = db.Messages.AsNoTracking().Where(m => m.AccountId == query.AccountId);

            var total = await messages.CountAsync(cancellationToken).ConfigureAwait(false);
            var rows = await messages
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(rows.ConvertAll(EmailMessageDto.Summary), total);
        }
    }
}
