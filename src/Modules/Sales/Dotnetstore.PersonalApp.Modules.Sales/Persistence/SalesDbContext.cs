using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    public DbSet<SalesTerritory> SalesTerritories => Set<SalesTerritory>();

    public DbSet<SalesPerson> SalesPeople => Set<SalesPerson>();

    public DbSet<SalesPersonQuotaHistory> SalesPersonQuotaHistories => Set<SalesPersonQuotaHistory>();

    public DbSet<SalesTerritoryHistory> SalesTerritoryHistories => Set<SalesTerritoryHistory>();

    public DbSet<SalesTaxRate> SalesTaxRates => Set<SalesTaxRate>();

    public DbSet<Currency> Currencies => Set<Currency>();

    public DbSet<CurrencyRate> CurrencyRates => Set<CurrencyRate>();

    public DbSet<CountryRegionCurrency> CountryRegionCurrencies => Set<CountryRegionCurrency>();

    public DbSet<SalesReason> SalesReasons => Set<SalesReason>();

    public DbSet<SpecialOffer> SpecialOffers => Set<SpecialOffer>();

    public DbSet<SpecialOfferProduct> SpecialOfferProducts => Set<SpecialOfferProduct>();

    public DbSet<Store> Stores => Set<Store>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();

    public DbSet<SalesOrderDetail> SalesOrderDetails => Set<SalesOrderDetail>();

    public DbSet<SalesOrderReason> SalesOrderReasons => Set<SalesOrderReason>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);
    }
}