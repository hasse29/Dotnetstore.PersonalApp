using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Offer> Offers => Set<Offer>();

    public DbSet<OfferLine> OfferLines => Set<OfferLine>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    public DbSet<PickList> PickLists => Set<PickList>();

    public DbSet<PickListLine> PickListLines => Set<PickListLine>();

    public DbSet<PickAllocation> PickAllocations => Set<PickAllocation>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<CreditEntry> CreditEntries => Set<CreditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);
    }
}
