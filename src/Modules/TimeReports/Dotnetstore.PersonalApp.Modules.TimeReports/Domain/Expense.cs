namespace Dotnetstore.PersonalApp.Modules.TimeReports.Domain;

/// <summary>
/// Money one user spent, such as a hotel night. <see cref="ClientId"/> and <see cref="UserId"/> refer to objects owned by other
/// modules; they are not foreign keys, and expenses are kept (they are tax records) if the client or user is later deleted.
/// </summary>
internal sealed class Expense
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>The client the expense was incurred for, if any.</summary>
    public Guid? ClientId { get; set; }

    public DateOnly Date { get; set; }

    public ExpenseCategory Category { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>The total paid, including <see cref="VatAmount"/>.</summary>
    public decimal Amount { get; set; }

    public decimal VatAmount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ExpenseReceipt? Receipt { get; set; }
}
