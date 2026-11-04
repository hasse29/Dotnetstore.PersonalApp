using Dotnetstore.PersonalApp.Modules.Purchasing.Features.ProductVendors;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrderDetails;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.ShipMethods;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.Vendors;
using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Tests;

/// <summary>
/// Real SQLite (in-memory, kept alive by one open connection) with the module's handlers resolved from a service provider.
/// </summary>
internal sealed class PurchasingFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private int _counter;

    public PurchasingFixture()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<PurchasingDbContext>().UseSqlite(_connection).Options;

        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        Dotnetstore.PersonalApp.Shared.Localization.Localizer.Instance.AddResources(Dotnetstore.PersonalApp.Modules.Purchasing.Resources.PurchasingStrings.ResourceManager);
        services.AddSingleton<Dotnetstore.PersonalApp.Shared.Localization.ILocalizationService>(Dotnetstore.PersonalApp.Shared.Localization.Localizer.Instance);
        services.AddSingleton<RecordingEventPublisher>();
        services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<RecordingEventPublisher>());
        services.AddSingleton<IDbContextFactory<PurchasingDbContext>>(new TestDbFactory(options));
        PurchasingModule.AddFeatures(services);
        Services = services.BuildServiceProvider();

        using var db = Services.GetRequiredService<IDbContextFactory<PurchasingDbContext>>().CreateDbContext();
        db.Database.EnsureCreated();
    }

    public ServiceProvider Services { get; }

    public Task<CreateShipMethod.Command> ShipMethodCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateShipMethod.Command(
            $"Ship{n}",
            1m,
            1m));
    }

    public async Task<ShipMethodDto> MakeShipMethodAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateShipMethod.Handler>()
            .HandleAsync(await ShipMethodCommandAsync(cancellationToken), cancellationToken)).Value;

    public Task<CreateVendor.Command> VendorCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateVendor.Command(
            $"Vendor{n}",
            $"Vendor{n}",
            1,
            false,
            false,
            $"Vendor{n}",
            null));
    }

    public async Task<VendorDto> MakeVendorAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateVendor.Handler>()
            .HandleAsync(await VendorCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateProductVendor.Command> ProductVendorCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateProductVendor.Command(
            Guid.NewGuid(),
            (await MakeVendorAsync(cancellationToken)).Id,
            1,
            1m,
            1m,
            new DateOnly(2026, 1, 1),
            1,
            1,
            1,
            Guid.NewGuid());
    }

    public async Task<ProductVendorDto> MakeProductVendorAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateProductVendor.Handler>()
            .HandleAsync(await ProductVendorCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreatePurchaseOrder.Command> PurchaseOrderCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreatePurchaseOrder.Command(
            $"Purchase{n}",
            "P",
            null,
            (await MakeVendorAsync(cancellationToken)).Id,
            (await MakeShipMethodAsync(cancellationToken)).Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            1m,
            1m);
    }

    public async Task<PurchaseOrderDto> MakePurchaseOrderAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreatePurchaseOrder.Handler>()
            .HandleAsync(await PurchaseOrderCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreatePurchaseOrderDetail.Command> PurchaseOrderDetailCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreatePurchaseOrderDetail.Command(
            (await MakePurchaseOrderAsync(cancellationToken)).Id,
            Guid.NewGuid(),
            new DateOnly(2026, 1, 1),
            1,
            1m,
            1m,
            1m);
    }

    public async Task<PurchaseOrderDetailDto> MakePurchaseOrderDetailAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreatePurchaseOrderDetail.Handler>()
            .HandleAsync(await PurchaseOrderDetailCommandAsync(cancellationToken), cancellationToken)).Value;

    public void Dispose()
    {
        Services.Dispose();
        _connection.Dispose();
    }

    private sealed class TestDbFactory(DbContextOptions<PurchasingDbContext> options) : IDbContextFactory<PurchasingDbContext>
    {
        public PurchasingDbContext CreateDbContext() => new(options);
    }
}