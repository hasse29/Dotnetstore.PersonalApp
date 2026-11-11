using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Shared.Photos;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Products;

/// <summary>A product as the list shows it: the DTO plus its kind and price in the current UI language.</summary>
internal sealed record ProductRow(ProductDto Product, string KindLabel, string PriceText)
{
    /// <summary>Shown in the list's avatar while the product has no photo.</summary>
    public string Initials => PhotoInitials.From(Product.Name);
}

/// <summary>A kind choice for the editor's picker and the list's filter; a null <see cref="Kind"/> means all kinds.</summary>
internal sealed record ProductKindOption(ProductKind? Kind, string Label);

internal sealed record ProductUnitOption(ProductUnit Unit, string Label);

/// <summary>A category choice for the editor's picker; a null <see cref="Id"/> means no category.</summary>
internal sealed record ProductCategoryOption(Guid? Id, string Name);
