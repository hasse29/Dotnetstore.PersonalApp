namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>Where an offer is in its life. Stored by name. An offer that is sent and past its valid-until date is shown as expired, but stays Sent.</summary>
internal enum OfferStatus
{
    Draft,
    Sent,
    Accepted,
    Declined,
}

/// <summary>An offer to a customer. Only a draft can be edited; an accepted offer is what an order is created from.</summary>
internal sealed class Offer
{
    public Guid Id { get; set; }

    /// <summary>Unique, for example OFF202611120001 (OFF, the date and a four-digit counter of the day).</summary>
    public string Number { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    /// <summary>The name of the customer when the offer was made.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>The address of the customer when the offer was made (street, then postal code and city).</summary>
    public string? CustomerAddress { get; set; }

    public DateOnly Date { get; set; }

    public DateOnly ValidUntil { get; set; }

    public OfferStatus Status { get; set; }

    /// <summary>The reference of the customer, shown on the offer.</summary>
    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<OfferLine> Lines { get; set; } = [];
}

/// <summary>One row of an offer. Product details are copied, so later price changes in Products do not touch the offer.</summary>
internal sealed class OfferLine
{
    public Guid Id { get; set; }

    public Guid OfferId { get; set; }

    /// <summary>Order of the lines on the offer, from 1.</summary>
    public int Position { get; set; }

    /// <summary>The product the line was made from; null for a free-text line. Not a foreign key: it lives in another module.</summary>
    public Guid? ProductId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    /// <summary>A unit key such as Piece or Hour, translated when shown.</summary>
    public string Unit { get; set; } = "Piece";

    public decimal UnitPrice { get; set; }

    /// <summary>VAT in percent.</summary>
    public decimal VatRate { get; set; }

    /// <summary>Discount in percent, 0 to 100.</summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>Whether goods have to be picked for the line when it is ordered. Free text and consulting hours are not.</summary>
    public bool IsPhysical { get; set; }
}
