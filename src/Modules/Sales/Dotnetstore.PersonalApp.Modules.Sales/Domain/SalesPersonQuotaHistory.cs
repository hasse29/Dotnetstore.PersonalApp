namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>quota history entry.</summary>
internal sealed class SalesPersonQuotaHistory
{
    public Guid Id { get; set; }

    public Guid SalesPersonId { get; set; }

    public SalesPerson SalesPerson { get; set; } = null!;

    public DateOnly QuotaDate { get; set; }

    public decimal SalesQuota { get; set; }
}