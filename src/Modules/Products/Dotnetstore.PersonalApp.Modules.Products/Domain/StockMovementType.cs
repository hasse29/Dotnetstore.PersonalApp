namespace Dotnetstore.PersonalApp.Modules.Products.Domain;

internal enum StockMovementType
{
    /// <summary>Goods came in, usually from a supplier.</summary>
    Receipt,

    /// <summary>Goods left: sold, used or thrown away.</summary>
    Issue,

    /// <summary>The receiving half of a move between two locations.</summary>
    TransferIn,

    /// <summary>The sending half of a move between two locations.</summary>
    TransferOut,

    /// <summary>Posting an inventory count corrected the quantity, up or down.</summary>
    CountAdjustment,
}
