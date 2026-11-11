using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

internal static class GetInvoice
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, TimeProvider clock)
    {
        public async Task<Result<InvoiceDetailDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var invoice = await InvoicePayments.LoadAsync(db, query.Id, cancellationToken).ConfigureAwait(false);

            return invoice is null
                ? Error.NotFound("invoice_not_found", "Invoice not found.")
                : await InvoicePayments.DetailAsync(db, invoice, DateOnly.FromDateTime(clock.GetLocalNow().DateTime), cancellationToken).ConfigureAwait(false);
        }
    }
}
