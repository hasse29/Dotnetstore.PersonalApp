namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>Where a payment on an invoice came from. Stored by name.</summary>
internal enum PaymentKind
{
    /// <summary>Money that arrived from the customer.</summary>
    Received,

    /// <summary>Credit the customer already had, used to pay this invoice.</summary>
    CreditApplied,
}

/// <summary>A payment on an invoice. An invoice can have several, and can be paid too much.</summary>
internal sealed class Payment
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    public PaymentKind Kind { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>The part that went to the invoice. What the invoice has been paid is the sum of these.</summary>
    public decimal Amount { get; set; }

    /// <summary>The part of a received payment that was more than the invoice needed; it became customer credit.</summary>
    public decimal Excess { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

internal enum CreditEntryKind
{
    /// <summary>Added when a payment was more than the invoice needed.</summary>
    Overpayment,

    /// <summary>Taken when credit was used to pay an invoice.</summary>
    Applied,

    /// <summary>Taken when the money was paid back to the customer.</summary>
    Refund,
}

/// <summary>
/// One line in a customer's credit ledger. Amounts are signed: an overpayment adds, applying and refunding take away. The balance is
/// their sum and never goes below zero.
/// </summary>
internal sealed class CreditEntry
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public CreditEntryKind Kind { get; set; }

    public DateOnly Date { get; set; }

    public decimal Amount { get; set; }

    /// <summary>The payment that created or used this credit; null for a refund.</summary>
    public Guid? PaymentId { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
