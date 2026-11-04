using Dotnetstore.PersonalApp.Modules.Production.Features.BillOfMaterials;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductCostHistories;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductInventories;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductListPriceHistories;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductReviews;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.Products;

/// <summary>A product with the things that hang off it, each on its own tab.</summary>
internal sealed class ProductDetailViewModel(
    ProductDto product,
    ProductCostHistoriesViewModel costHistory,
    ProductListPriceHistoriesViewModel listPrices,
    ProductInventoriesViewModel inventory,
    ProductReviewsViewModel reviews,
    BillOfMaterialsViewModel billOfMaterials) : DetailViewModel
{
    public ProductDto Product { get; } = product;

    public ProductCostHistoriesViewModel CostHistory { get; } = costHistory.ForParent(product.Id);

    public ProductListPriceHistoriesViewModel ListPrices { get; } = listPrices.ForParent(product.Id);

    public ProductInventoriesViewModel Inventory { get; } = inventory.ForParent(product.Id);

    public ProductReviewsViewModel Reviews { get; } = reviews.ForParent(product.Id);

    /// <summary>The components of this product, when it is an assembly.</summary>
    public BillOfMaterialsViewModel BillOfMaterials { get; } = billOfMaterials.ForParent(product.Id);
}
