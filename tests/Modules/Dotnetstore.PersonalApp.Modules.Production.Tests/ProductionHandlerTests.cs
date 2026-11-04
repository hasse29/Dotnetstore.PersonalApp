using Dotnetstore.PersonalApp.Modules.Production.Features.BillOfMaterials;
using Dotnetstore.PersonalApp.Modules.Production.Features.Locations;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductCategories;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductCostHistories;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductInventories;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductListPriceHistories;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductModels;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductReviews;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductSubcategories;
using Dotnetstore.PersonalApp.Modules.Production.Features.Products;
using Dotnetstore.PersonalApp.Modules.Production.Features.ScrapReasons;
using Dotnetstore.PersonalApp.Modules.Production.Features.TransactionHistories;
using Dotnetstore.PersonalApp.Modules.Production.Features.UnitMeasures;
using Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrderRoutings;
using Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrders;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Production.Tests;

public sealed class ProductionHandlerTests : IDisposable
{
    private readonly ProductionFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task UnitMeasureCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeUnitMeasureAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateUnitMeasure.Handler>()
            .HandleAsync(new UpdateUnitMeasure.Command(created.Id, created.Code, created.Name), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListUnitMeasures.Handler>()
            .HandleAsync(new ListUnitMeasures.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteUnitMeasure.Handler>()
            .HandleAsync(new DeleteUnitMeasure.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteUnitMeasure.Handler>()
            .HandleAsync(new DeleteUnitMeasure.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("unit_measure_not_found");
    }

    [Fact]
    public async Task UnitMeasureRejectsAMissingCode()
    {
        var command = await _fixture.UnitMeasureCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateUnitMeasure.Handler>()
            .HandleAsync(command with { Code = "  " }, Ct);

        result.Error.Code.ShouldBe("unit_measure_code_required");
    }

    [Fact]
    public async Task UnitMeasureRejectsADuplicate()
    {
        var command = await _fixture.UnitMeasureCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateUnitMeasure.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("unit_measure_conflict");
    }

    [Fact]
    public async Task UnitMeasureInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeUnitMeasureAsync(Ct);
        var childCommand = await _fixture.ProductCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateProduct.Handler>()
            .HandleAsync(childCommand with { SizeUnitId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteUnitMeasure.Handler>()
            .HandleAsync(new DeleteUnitMeasure.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("unit_measure_in_use");
    }

    [Fact]
    public async Task ProductCategoryCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeProductCategoryAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateProductCategory.Handler>()
            .HandleAsync(new UpdateProductCategory.Command(created.Id, created.Name), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListProductCategories.Handler>()
            .HandleAsync(new ListProductCategories.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteProductCategory.Handler>()
            .HandleAsync(new DeleteProductCategory.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteProductCategory.Handler>()
            .HandleAsync(new DeleteProductCategory.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("product_category_not_found");
    }

    [Fact]
    public async Task ProductCategoryRejectsAMissingName()
    {
        var command = await _fixture.ProductCategoryCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateProductCategory.Handler>()
            .HandleAsync(command with { Name = "  " }, Ct);

        result.Error.Code.ShouldBe("product_category_name_required");
    }

    [Fact]
    public async Task ProductCategoryRejectsADuplicate()
    {
        var command = await _fixture.ProductCategoryCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateProductCategory.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("product_category_conflict");
    }

    [Fact]
    public async Task ProductCategoryInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeProductCategoryAsync(Ct);
        var childCommand = await _fixture.ProductSubcategoryCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateProductSubcategory.Handler>()
            .HandleAsync(childCommand with { CategoryId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteProductCategory.Handler>()
            .HandleAsync(new DeleteProductCategory.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("product_category_in_use");
    }

    [Fact]
    public async Task ProductSubcategoryCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeProductSubcategoryAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateProductSubcategory.Handler>()
            .HandleAsync(new UpdateProductSubcategory.Command(created.Id, created.CategoryId, created.Name), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListProductSubcategories.Handler>()
            .HandleAsync(new ListProductSubcategories.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteProductSubcategory.Handler>()
            .HandleAsync(new DeleteProductSubcategory.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteProductSubcategory.Handler>()
            .HandleAsync(new DeleteProductSubcategory.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("product_subcategory_not_found");
    }

    [Fact]
    public async Task ProductSubcategoryRejectsAMissingName()
    {
        var command = await _fixture.ProductSubcategoryCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateProductSubcategory.Handler>()
            .HandleAsync(command with { Name = "  " }, Ct);

        result.Error.Code.ShouldBe("product_subcategory_name_required");
    }

    [Fact]
    public async Task ProductSubcategoryRejectsADuplicate()
    {
        var command = await _fixture.ProductSubcategoryCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateProductSubcategory.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("product_subcategory_conflict");
    }

    [Fact]
    public async Task ProductSubcategoryInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeProductSubcategoryAsync(Ct);
        var childCommand = await _fixture.ProductCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateProduct.Handler>()
            .HandleAsync(childCommand with { SubcategoryId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteProductSubcategory.Handler>()
            .HandleAsync(new DeleteProductSubcategory.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("product_subcategory_in_use");
    }

    [Fact]
    public async Task ProductModelCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeProductModelAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateProductModel.Handler>()
            .HandleAsync(new UpdateProductModel.Command(created.Id, created.Name, created.Description, created.Instructions), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListProductModels.Handler>()
            .HandleAsync(new ListProductModels.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteProductModel.Handler>()
            .HandleAsync(new DeleteProductModel.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteProductModel.Handler>()
            .HandleAsync(new DeleteProductModel.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("product_model_not_found");
    }

    [Fact]
    public async Task ProductModelRejectsAMissingName()
    {
        var command = await _fixture.ProductModelCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateProductModel.Handler>()
            .HandleAsync(command with { Name = "  " }, Ct);

        result.Error.Code.ShouldBe("product_model_name_required");
    }

    [Fact]
    public async Task ProductModelRejectsADuplicate()
    {
        var command = await _fixture.ProductModelCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateProductModel.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("product_model_conflict");
    }

    [Fact]
    public async Task ProductModelInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeProductModelAsync(Ct);
        var childCommand = await _fixture.ProductCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateProduct.Handler>()
            .HandleAsync(childCommand with { ModelId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteProductModel.Handler>()
            .HandleAsync(new DeleteProductModel.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("product_model_in_use");
    }

    [Fact]
    public async Task ScrapReasonCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeScrapReasonAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateScrapReason.Handler>()
            .HandleAsync(new UpdateScrapReason.Command(created.Id, created.Name), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListScrapReasons.Handler>()
            .HandleAsync(new ListScrapReasons.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteScrapReason.Handler>()
            .HandleAsync(new DeleteScrapReason.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteScrapReason.Handler>()
            .HandleAsync(new DeleteScrapReason.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("scrap_reason_not_found");
    }

    [Fact]
    public async Task ScrapReasonRejectsAMissingName()
    {
        var command = await _fixture.ScrapReasonCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateScrapReason.Handler>()
            .HandleAsync(command with { Name = "  " }, Ct);

        result.Error.Code.ShouldBe("scrap_reason_name_required");
    }

    [Fact]
    public async Task ScrapReasonRejectsADuplicate()
    {
        var command = await _fixture.ScrapReasonCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateScrapReason.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("scrap_reason_conflict");
    }

    [Fact]
    public async Task ScrapReasonInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeScrapReasonAsync(Ct);
        var childCommand = await _fixture.WorkOrderCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateWorkOrder.Handler>()
            .HandleAsync(childCommand with { ScrapReasonId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteScrapReason.Handler>()
            .HandleAsync(new DeleteScrapReason.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("scrap_reason_in_use");
    }

    [Fact]
    public async Task LocationCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeLocationAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateLocation.Handler>()
            .HandleAsync(new UpdateLocation.Command(created.Id, created.Name, created.CostRate, created.Availability), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListLocations.Handler>()
            .HandleAsync(new ListLocations.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteLocation.Handler>()
            .HandleAsync(new DeleteLocation.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteLocation.Handler>()
            .HandleAsync(new DeleteLocation.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("location_not_found");
    }

    [Fact]
    public async Task LocationRejectsAMissingName()
    {
        var command = await _fixture.LocationCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateLocation.Handler>()
            .HandleAsync(command with { Name = "  " }, Ct);

        result.Error.Code.ShouldBe("location_name_required");
    }

    [Fact]
    public async Task LocationRejectsADuplicate()
    {
        var command = await _fixture.LocationCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateLocation.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("location_conflict");
    }

    [Fact]
    public async Task LocationInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeLocationAsync(Ct);
        var childCommand = await _fixture.ProductInventoryCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateProductInventory.Handler>()
            .HandleAsync(childCommand with { LocationId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteLocation.Handler>()
            .HandleAsync(new DeleteLocation.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("location_in_use");
    }

    [Fact]
    public async Task ProductCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeProductAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateProduct.Handler>()
            .HandleAsync(new UpdateProduct.Command(created.Id, created.Name, created.ProductNumber, created.MakeFlag, created.FinishedGoodsFlag, created.Color, created.SafetyStockLevel, created.ReorderPoint, created.StandardCost, created.ListPrice, created.Size, created.SizeUnitId, created.Weight, created.WeightUnitId, created.DaysToManufacture, created.ProductLine, created.Class, created.Style, created.SubcategoryId, created.ModelId, created.SellStartDate, created.SellEndDate, created.DiscontinuedDate), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListProducts.Handler>()
            .HandleAsync(new ListProducts.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteProduct.Handler>()
            .HandleAsync(new DeleteProduct.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteProduct.Handler>()
            .HandleAsync(new DeleteProduct.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("product_not_found");
    }

    [Fact]
    public async Task ProductRejectsAMissingName()
    {
        var command = await _fixture.ProductCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateProduct.Handler>()
            .HandleAsync(command with { Name = "  " }, Ct);

        result.Error.Code.ShouldBe("product_name_required");
    }

    [Fact]
    public async Task ProductRejectsADuplicate()
    {
        var command = await _fixture.ProductCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateProduct.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("product_conflict");
    }

    [Fact]
    public async Task ProductInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeProductAsync(Ct);
        var childCommand = await _fixture.BillOfMaterialCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateBillOfMaterial.Handler>()
            .HandleAsync(childCommand with { ProductAssemblyId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteProduct.Handler>()
            .HandleAsync(new DeleteProduct.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("product_in_use");
    }

    [Fact]
    public async Task ProductCostHistoryCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeProductCostHistoryAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateProductCostHistory.Handler>()
            .HandleAsync(new UpdateProductCostHistory.Command(created.Id, created.ProductId, created.StartDate, created.EndDate, created.StandardCost), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListProductCostHistories.Handler>()
            .HandleAsync(new ListProductCostHistories.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteProductCostHistory.Handler>()
            .HandleAsync(new DeleteProductCostHistory.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteProductCostHistory.Handler>()
            .HandleAsync(new DeleteProductCostHistory.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("product_cost_history_not_found");
    }

    [Fact]
    public async Task ProductListPriceHistoryCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeProductListPriceHistoryAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateProductListPriceHistory.Handler>()
            .HandleAsync(new UpdateProductListPriceHistory.Command(created.Id, created.ProductId, created.StartDate, created.EndDate, created.ListPrice), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListProductListPriceHistories.Handler>()
            .HandleAsync(new ListProductListPriceHistories.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteProductListPriceHistory.Handler>()
            .HandleAsync(new DeleteProductListPriceHistory.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteProductListPriceHistory.Handler>()
            .HandleAsync(new DeleteProductListPriceHistory.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("product_list_price_history_not_found");
    }

    [Fact]
    public async Task ProductInventoryCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeProductInventoryAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateProductInventory.Handler>()
            .HandleAsync(new UpdateProductInventory.Command(created.Id, created.ProductId, created.LocationId, created.Shelf, created.Bin, created.Quantity), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListProductInventories.Handler>()
            .HandleAsync(new ListProductInventories.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteProductInventory.Handler>()
            .HandleAsync(new DeleteProductInventory.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteProductInventory.Handler>()
            .HandleAsync(new DeleteProductInventory.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("product_inventory_not_found");
    }

    [Fact]
    public async Task ProductInventoryRejectsAMissingShelf()
    {
        var command = await _fixture.ProductInventoryCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateProductInventory.Handler>()
            .HandleAsync(command with { Shelf = "  " }, Ct);

        result.Error.Code.ShouldBe("product_inventory_shelf_required");
    }

    [Fact]
    public async Task ProductInventoryRejectsADuplicate()
    {
        var command = await _fixture.ProductInventoryCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateProductInventory.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("product_inventory_conflict");
    }

    [Fact]
    public async Task ProductReviewCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeProductReviewAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateProductReview.Handler>()
            .HandleAsync(new UpdateProductReview.Command(created.Id, created.ProductId, created.ReviewerName, created.ReviewDate, created.EmailAddress, created.Rating, created.Comments), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListProductReviews.Handler>()
            .HandleAsync(new ListProductReviews.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteProductReview.Handler>()
            .HandleAsync(new DeleteProductReview.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteProductReview.Handler>()
            .HandleAsync(new DeleteProductReview.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("product_review_not_found");
    }

    [Fact]
    public async Task ProductReviewRejectsAMissingReviewerName()
    {
        var command = await _fixture.ProductReviewCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateProductReview.Handler>()
            .HandleAsync(command with { ReviewerName = "  " }, Ct);

        result.Error.Code.ShouldBe("product_review_reviewer_name_required");
    }

    [Fact]
    public async Task BillOfMaterialCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeBillOfMaterialAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateBillOfMaterial.Handler>()
            .HandleAsync(new UpdateBillOfMaterial.Command(created.Id, created.ProductAssemblyId, created.ComponentId, created.StartDate, created.EndDate, created.UnitId, created.BomLevel, created.PerAssemblyQty), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListBillOfMaterials.Handler>()
            .HandleAsync(new ListBillOfMaterials.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteBillOfMaterial.Handler>()
            .HandleAsync(new DeleteBillOfMaterial.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteBillOfMaterial.Handler>()
            .HandleAsync(new DeleteBillOfMaterial.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("bill_of_material_not_found");
    }

    [Fact]
    public async Task WorkOrderCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeWorkOrderAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateWorkOrder.Handler>()
            .HandleAsync(new UpdateWorkOrder.Command(created.Id, created.Number, created.ProductId, created.OrderQty, created.ScrappedQty, created.StartDate, created.EndDate, created.DueDate, created.ScrapReasonId), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListWorkOrders.Handler>()
            .HandleAsync(new ListWorkOrders.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteWorkOrder.Handler>()
            .HandleAsync(new DeleteWorkOrder.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteWorkOrder.Handler>()
            .HandleAsync(new DeleteWorkOrder.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("work_order_not_found");
    }

    [Fact]
    public async Task WorkOrderRejectsADuplicate()
    {
        var command = await _fixture.WorkOrderCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateWorkOrder.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("work_order_conflict");
    }

    [Fact]
    public async Task WorkOrderRoutingCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeWorkOrderRoutingAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateWorkOrderRouting.Handler>()
            .HandleAsync(new UpdateWorkOrderRouting.Command(created.Id, created.WorkOrderId, created.OperationSequence, created.LocationId, created.ScheduledStartDate, created.ScheduledEndDate, created.ActualStartDate, created.ActualEndDate, created.ActualResourceHrs, created.PlannedCost, created.ActualCost), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListWorkOrderRoutings.Handler>()
            .HandleAsync(new ListWorkOrderRoutings.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteWorkOrderRouting.Handler>()
            .HandleAsync(new DeleteWorkOrderRouting.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteWorkOrderRouting.Handler>()
            .HandleAsync(new DeleteWorkOrderRouting.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("work_order_routing_not_found");
    }

    [Fact]
    public async Task WorkOrderRoutingRejectsADuplicate()
    {
        var command = await _fixture.WorkOrderRoutingCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateWorkOrderRouting.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("work_order_routing_conflict");
    }

    [Fact]
    public async Task TransactionHistoryCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeTransactionHistoryAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateTransactionHistory.Handler>()
            .HandleAsync(new UpdateTransactionHistory.Command(created.Id, created.ProductId, created.ReferenceOrder, created.TransactionDate, created.TransactionType, created.Quantity, created.ActualCost), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListTransactionHistories.Handler>()
            .HandleAsync(new ListTransactionHistories.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteTransactionHistory.Handler>()
            .HandleAsync(new DeleteTransactionHistory.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteTransactionHistory.Handler>()
            .HandleAsync(new DeleteTransactionHistory.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("transaction_history_not_found");
    }
}