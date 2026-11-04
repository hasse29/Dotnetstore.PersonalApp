using Dotnetstore.PersonalApp.Modules.Sales.Features.CountryRegionCurrencies;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Currencies;
using Dotnetstore.PersonalApp.Modules.Sales.Features.CurrencyRates;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderDetails;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderReasons;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPeople;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPersonQuotaHistories;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesReasons;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTaxRates;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritories;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritoryHistories;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOfferProducts;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOffers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Stores;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

/// <summary>
/// Real SQLite (in-memory, kept alive by one open connection) with the module's handlers resolved from a service provider.
/// </summary>
internal sealed class SalesFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private int _counter;

    public SalesFixture()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<SalesDbContext>().UseSqlite(_connection).Options;

        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        Dotnetstore.PersonalApp.Shared.Localization.Localizer.Instance.AddResources(Dotnetstore.PersonalApp.Modules.Sales.Resources.SalesStrings.ResourceManager);
        services.AddSingleton<Dotnetstore.PersonalApp.Shared.Localization.ILocalizationService>(Dotnetstore.PersonalApp.Shared.Localization.Localizer.Instance);
        services.AddSingleton<RecordingEventPublisher>();
        services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<RecordingEventPublisher>());
        services.AddSingleton<IDbContextFactory<SalesDbContext>>(new TestDbFactory(options));
        SalesModule.AddFeatures(services);
        Services = services.BuildServiceProvider();

        using var db = Services.GetRequiredService<IDbContextFactory<SalesDbContext>>().CreateDbContext();
        db.Database.EnsureCreated();
    }

    public ServiceProvider Services { get; }

    public Task<CreateSalesTerritory.Command> SalesTerritoryCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateSalesTerritory.Command(
            $"Sales{n}",
            $"Sales{n}",
            $"Sales{n}",
            1m,
            1m,
            1m,
            1m));
    }

    public async Task<SalesTerritoryDto> MakeSalesTerritoryAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateSalesTerritory.Handler>()
            .HandleAsync(await SalesTerritoryCommandAsync(cancellationToken), cancellationToken)).Value;

    public Task<CreateSalesPerson.Command> SalesPersonCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateSalesPerson.Command(
            $"Sales{n}",
            null,
            null,
            1m,
            1m,
            1m,
            1m,
            1m));
    }

    public async Task<SalesPersonDto> MakeSalesPersonAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateSalesPerson.Handler>()
            .HandleAsync(await SalesPersonCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateSalesPersonQuotaHistory.Command> SalesPersonQuotaHistoryCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateSalesPersonQuotaHistory.Command(
            (await MakeSalesPersonAsync(cancellationToken)).Id,
            new DateOnly(2026, 1, 1),
            1m);
    }

    public async Task<SalesPersonQuotaHistoryDto> MakeSalesPersonQuotaHistoryAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateSalesPersonQuotaHistory.Handler>()
            .HandleAsync(await SalesPersonQuotaHistoryCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateSalesTerritoryHistory.Command> SalesTerritoryHistoryCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateSalesTerritoryHistory.Command(
            (await MakeSalesPersonAsync(cancellationToken)).Id,
            (await MakeSalesTerritoryAsync(cancellationToken)).Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1));
    }

    public async Task<SalesTerritoryHistoryDto> MakeSalesTerritoryHistoryAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateSalesTerritoryHistory.Handler>()
            .HandleAsync(await SalesTerritoryHistoryCommandAsync(cancellationToken), cancellationToken)).Value;

    public Task<CreateSalesTaxRate.Command> SalesTaxRateCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateSalesTaxRate.Command(
            $"Sales{n}",
            $"Sales{n}",
            "R",
            1m));
    }

    public async Task<SalesTaxRateDto> MakeSalesTaxRateAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateSalesTaxRate.Handler>()
            .HandleAsync(await SalesTaxRateCommandAsync(cancellationToken), cancellationToken)).Value;

    public Task<CreateCurrency.Command> CurrencyCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateCurrency.Command(
            $"S{n}",
            $"Currency{n}"));
    }

    public async Task<CurrencyDto> MakeCurrencyAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateCurrency.Handler>()
            .HandleAsync(await CurrencyCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateCurrencyRate.Command> CurrencyRateCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateCurrencyRate.Command(
            new DateOnly(2026, 1, 1),
            (await MakeCurrencyAsync(cancellationToken)).Id,
            (await MakeCurrencyAsync(cancellationToken)).Id,
            1m,
            1m);
    }

    public async Task<CurrencyRateDto> MakeCurrencyRateAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateCurrencyRate.Handler>()
            .HandleAsync(await CurrencyRateCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateCountryRegionCurrency.Command> CountryRegionCurrencyCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateCountryRegionCurrency.Command(
            $"Country{n}",
            (await MakeCurrencyAsync(cancellationToken)).Id);
    }

    public async Task<CountryRegionCurrencyDto> MakeCountryRegionCurrencyAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateCountryRegionCurrency.Handler>()
            .HandleAsync(await CountryRegionCurrencyCommandAsync(cancellationToken), cancellationToken)).Value;

    public Task<CreateSalesReason.Command> SalesReasonCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateSalesReason.Command(
            $"Sales{n}",
            "M"));
    }

    public async Task<SalesReasonDto> MakeSalesReasonAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateSalesReason.Handler>()
            .HandleAsync(await SalesReasonCommandAsync(cancellationToken), cancellationToken)).Value;

    public Task<CreateSpecialOffer.Command> SpecialOfferCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateSpecialOffer.Command(
            $"Special{n}",
            1m,
            $"Special{n}",
            $"Special{n}",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            1,
            1));
    }

    public async Task<SpecialOfferDto> MakeSpecialOfferAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateSpecialOffer.Handler>()
            .HandleAsync(await SpecialOfferCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateSpecialOfferProduct.Command> SpecialOfferProductCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateSpecialOfferProduct.Command(
            (await MakeSpecialOfferAsync(cancellationToken)).Id,
            Guid.NewGuid());
    }

    public async Task<SpecialOfferProductDto> MakeSpecialOfferProductAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateSpecialOfferProduct.Handler>()
            .HandleAsync(await SpecialOfferProductCommandAsync(cancellationToken), cancellationToken)).Value;

    public Task<CreateStore.Command> StoreCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateStore.Command(
            $"Store{n}",
            null));
    }

    public async Task<StoreDto> MakeStoreAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateStore.Handler>()
            .HandleAsync(await StoreCommandAsync(cancellationToken), cancellationToken)).Value;

    public Task<CreateCustomer.Command> CustomerCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateCustomer.Command(
            $"Customer{n}",
            $"Customer{n}",
            null,
            null,
            null));
    }

    public async Task<CustomerDto> MakeCustomerAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateCustomer.Handler>()
            .HandleAsync(await CustomerCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateSalesOrder.Command> SalesOrderCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateSalesOrder.Command(
            $"Sales{n}",
            "I",
            false,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            $"Sales{n}",
            (await MakeCustomerAsync(cancellationToken)).Id,
            null,
            null,
            null,
            1m,
            1m,
            $"Sales{n}");
    }

    public async Task<SalesOrderDto> MakeSalesOrderAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateSalesOrder.Handler>()
            .HandleAsync(await SalesOrderCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateSalesOrderDetail.Command> SalesOrderDetailCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateSalesOrderDetail.Command(
            (await MakeSalesOrderAsync(cancellationToken)).Id,
            Guid.NewGuid(),
            $"Sales{n}",
            1,
            null,
            1m,
            1m);
    }

    public async Task<SalesOrderDetailDto> MakeSalesOrderDetailAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateSalesOrderDetail.Handler>()
            .HandleAsync(await SalesOrderDetailCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateSalesOrderReason.Command> SalesOrderReasonCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateSalesOrderReason.Command(
            (await MakeSalesOrderAsync(cancellationToken)).Id,
            (await MakeSalesReasonAsync(cancellationToken)).Id);
    }

    public async Task<SalesOrderReasonDto> MakeSalesOrderReasonAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateSalesOrderReason.Handler>()
            .HandleAsync(await SalesOrderReasonCommandAsync(cancellationToken), cancellationToken)).Value;

    public void Dispose()
    {
        Services.Dispose();
        _connection.Dispose();
    }

    private sealed class TestDbFactory(DbContextOptions<SalesDbContext> options) : IDbContextFactory<SalesDbContext>
    {
        public SalesDbContext CreateDbContext() => new(options);
    }
}