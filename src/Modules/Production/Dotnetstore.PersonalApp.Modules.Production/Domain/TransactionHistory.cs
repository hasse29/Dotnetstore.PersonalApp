namespace Dotnetstore.PersonalApp.Modules.Production.Domain;

/// <summary>inventory transaction.</summary>
internal sealed class TransactionHistory
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string? ReferenceOrder { get; set; }

    public DateOnly TransactionDate { get; set; }

    public string TransactionType { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal ActualCost { get; set; }
}