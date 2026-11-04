using Dotnetstore.PersonalApp.Modules.Purchasing.Features.ProductVendors;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrderDetails;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.ShipMethods;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.Vendors;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Tests;

public sealed class PurchasingHandlerTests : IDisposable
{
    private readonly PurchasingFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ShipMethodCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeShipMethodAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateShipMethod.Handler>()
            .HandleAsync(new UpdateShipMethod.Command(created.Id, created.Name, created.ShipBase, created.ShipRate), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListShipMethods.Handler>()
            .HandleAsync(new ListShipMethods.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteShipMethod.Handler>()
            .HandleAsync(new DeleteShipMethod.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteShipMethod.Handler>()
            .HandleAsync(new DeleteShipMethod.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("ship_method_not_found");
    }

    [Fact]
    public async Task ShipMethodRejectsAMissingName()
    {
        var command = await _fixture.ShipMethodCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateShipMethod.Handler>()
            .HandleAsync(command with { Name = "  " }, Ct);

        result.Error.Code.ShouldBe("ship_method_name_required");
    }

    [Fact]
    public async Task ShipMethodRejectsADuplicate()
    {
        var command = await _fixture.ShipMethodCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateShipMethod.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("ship_method_conflict");
    }

    [Fact]
    public async Task ShipMethodInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeShipMethodAsync(Ct);
        var childCommand = await _fixture.PurchaseOrderCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreatePurchaseOrder.Handler>()
            .HandleAsync(childCommand with { ShipMethodId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteShipMethod.Handler>()
            .HandleAsync(new DeleteShipMethod.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("ship_method_in_use");
    }

    [Fact]
    public async Task VendorCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeVendorAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateVendor.Handler>()
            .HandleAsync(new UpdateVendor.Command(created.Id, created.AccountNumber, created.Name, created.CreditRating, created.PreferredVendorStatus, created.ActiveFlag, created.PurchasingWebServiceUrl, created.ClientId), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListVendors.Handler>()
            .HandleAsync(new ListVendors.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteVendor.Handler>()
            .HandleAsync(new DeleteVendor.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteVendor.Handler>()
            .HandleAsync(new DeleteVendor.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("vendor_not_found");
    }

    [Fact]
    public async Task VendorRejectsAMissingAccountNumber()
    {
        var command = await _fixture.VendorCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateVendor.Handler>()
            .HandleAsync(command with { AccountNumber = "  " }, Ct);

        result.Error.Code.ShouldBe("vendor_account_number_required");
    }

    [Fact]
    public async Task VendorRejectsADuplicate()
    {
        var command = await _fixture.VendorCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateVendor.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("vendor_conflict");
    }

    [Fact]
    public async Task VendorInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeVendorAsync(Ct);
        var childCommand = await _fixture.PurchaseOrderCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreatePurchaseOrder.Handler>()
            .HandleAsync(childCommand with { VendorId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteVendor.Handler>()
            .HandleAsync(new DeleteVendor.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("vendor_in_use");
    }

    [Fact]
    public async Task ProductVendorCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeProductVendorAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateProductVendor.Handler>()
            .HandleAsync(new UpdateProductVendor.Command(created.Id, created.ProductId, created.VendorId, created.AverageLeadTime, created.StandardPrice, created.LastReceiptCost, created.LastReceiptDate, created.MinOrderQty, created.MaxOrderQty, created.OnOrderQty, created.UnitId), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListProductVendors.Handler>()
            .HandleAsync(new ListProductVendors.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteProductVendor.Handler>()
            .HandleAsync(new DeleteProductVendor.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteProductVendor.Handler>()
            .HandleAsync(new DeleteProductVendor.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("product_vendor_not_found");
    }

    [Fact]
    public async Task ProductVendorRejectsADuplicate()
    {
        var command = await _fixture.ProductVendorCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateProductVendor.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("product_vendor_conflict");
    }

    [Fact]
    public async Task PurchaseOrderCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakePurchaseOrderAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdatePurchaseOrder.Handler>()
            .HandleAsync(new UpdatePurchaseOrder.Command(created.Id, created.Number, created.Status, created.BuyerId, created.VendorId, created.ShipMethodId, created.OrderDate, created.ShipDate, created.TaxAmt, created.Freight), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListPurchaseOrders.Handler>()
            .HandleAsync(new ListPurchaseOrders.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeletePurchaseOrder.Handler>()
            .HandleAsync(new DeletePurchaseOrder.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeletePurchaseOrder.Handler>()
            .HandleAsync(new DeletePurchaseOrder.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("purchase_order_not_found");
    }

    [Fact]
    public async Task PurchaseOrderRejectsADuplicate()
    {
        var command = await _fixture.PurchaseOrderCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreatePurchaseOrder.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("purchase_order_conflict");
    }

    [Fact]
    public async Task PurchaseOrderDetailCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakePurchaseOrderDetailAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdatePurchaseOrderDetail.Handler>()
            .HandleAsync(new UpdatePurchaseOrderDetail.Command(created.Id, created.PurchaseOrderId, created.ProductId, created.DueDate, created.OrderQty, created.UnitPrice, created.ReceivedQty, created.RejectedQty), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListPurchaseOrderDetails.Handler>()
            .HandleAsync(new ListPurchaseOrderDetails.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeletePurchaseOrderDetail.Handler>()
            .HandleAsync(new DeletePurchaseOrderDetail.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeletePurchaseOrderDetail.Handler>()
            .HandleAsync(new DeletePurchaseOrderDetail.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("purchase_order_detail_not_found");
    }
}