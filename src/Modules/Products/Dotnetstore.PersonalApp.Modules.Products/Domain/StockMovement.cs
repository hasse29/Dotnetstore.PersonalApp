namespace Dotnetstore.PersonalApp.Modules.Products.Domain;

/// <summary>One line of the stock history. Movements are never edited or deleted, so the history always explains the stock levels.</summary>
internal sealed class StockMovement
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid LocationId { get; set; }

    public StockMovementType Type { get; set; }

    /// <summary>Signed: positive when stock increases, negative when it decreases.</summary>
    public decimal Quantity { get; set; }

    /// <summary>For receipts: who delivered. Cleared if the supplier is deleted.</summary>
    public Guid? SupplierId { get; set; }

    public string? Note { get; set; }

    /// <summary>Ties the two halves of a transfer together.</summary>
    public Guid? TransferId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
}
