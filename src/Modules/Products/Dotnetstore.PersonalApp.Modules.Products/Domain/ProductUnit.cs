namespace Dotnetstore.PersonalApp.Modules.Products.Domain;

/// <summary>What <see cref="Product.UnitPrice"/> is a price per. Stored by name.</summary>
internal enum ProductUnit
{
    Piece,
    Hour,
    Day,
    Month,
    License,
}
