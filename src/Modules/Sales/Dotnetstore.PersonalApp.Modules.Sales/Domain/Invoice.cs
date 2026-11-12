namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>Where an invoice is. Stored by name. A cancelled invoice keeps its number; the order can be invoiced again with a new one.</summary>
internal enum InvoiceStatus
{
    Unpaid,
    PartiallyPaid,
    Paid,
    Cancelled,
}

/// <summary>An invoice for a delivered order. Everything is copied from the order, so the invoice is a fixed document.</summary>
internal sealed class Invoice
{
    public Guid Id { get; set; }

    /// <summary>Unique, for example INV202611120001 (INV, the date and a four-digit counter of the day). Numbers are never reused, not even after a cancellation.</summary>
    public string Number { get; set; } = string.Empty;

    public Guid OrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerAddress { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public DateOnly Date { get; set; }

    public DateOnly DueDate { get; set; }

    public InvoiceStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<InvoiceLine> Lines { get; set; } = [];

    public List<Payment> Payments { get; set; } = [];
}

internal sealed class InvoiceLine
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    public int Position { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = "Piece";

    public decimal UnitPrice { get; set; }

    public decimal VatRate { get; set; }

    public decimal DiscountPercent { get; set; }
}