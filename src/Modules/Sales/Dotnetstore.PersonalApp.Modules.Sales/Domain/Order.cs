namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>Where an order is. Stored by name. An order is Open until everything on its pick list is picked, then ReadyToInvoice.</summary>
internal enum OrderStatus
{
    Open,
    ReadyToInvoice,
    Invoiced,
    Cancelled,
}

internal enum PickListStatus
{
    Open,
    Complete,
    Cancelled,
}

/// <summary>An accepted offer that is being delivered. Everything on it is copied from the offer, so the order stays as it was agreed.</summary>
internal sealed class Order
{
    public Guid Id { get; set; }

    /// <summary>Unique and sequential per year, for example ORD-2026-0001.</summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>The offer the order was made from. Not unique: a cancelled order lets the offer be ordered again.</summary>
    public Guid OfferId { get; set; }

    public string OfferNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerAddress { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public DateOnly Date { get; set; }

    public OrderStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<OrderLine> Lines { get; set; } = [];

    public PickList? PickList { get; set; }
}

internal sealed class OrderLine
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public int Position { get; set; }

    public Guid? ProductId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = "Piece";

    public decimal UnitPrice { get; set; }

    public decimal VatRate { get; set; }

    public decimal DiscountPercent { get; set; }

    /// <summary>Whether goods have to be picked for the line. Free text and consulting hours are not.</summary>
    public bool IsPhysical { get; set; }
}

/// <summary>What has to be picked for an order: its physical lines, and how much of each has been picked so far.</summary>
internal sealed class PickList
{
    public Guid Id { get; set; }

    /// <summary>The order number with PL instead of ORD, for example PL-2026-0001.</summary>
    public string Number { get; set; } = string.Empty;

    public Guid OrderId { get; set; }

    public PickListStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<PickListLine> Lines { get; set; } = [];
}

internal sealed class PickListLine
{
    public Guid Id { get; set; }

    public Guid PickListId { get; set; }

    public int Position { get; set; }

    public Guid? ProductId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Unit { get; set; } = "Piece";

    public decimal Quantity { get; set; }

    public decimal PickedQuantity { get; set; }

    /// <summary>Where the picked goods were taken from, so that they can be put back if less is picked or the order is cancelled.</summary>
    public List<PickAllocation> Allocations { get; set; } = [];
}

/// <summary>Some of a pick list line taken from one storage location.</summary>
internal sealed class PickAllocation
{
    public Guid Id { get; set; }

    public Guid PickListLineId { get; set; }

    /// <summary>A storage location in Products. Not a foreign key: it lives in another database.</summary>
    public Guid LocationId { get; set; }

    public decimal Quantity { get; set; }
}
