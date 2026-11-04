namespace Dotnetstore.PersonalApp.Modules.Purchasing.Domain;

/// <summary>vendor.</summary>
internal sealed class Vendor
{
    public Guid Id { get; set; }

    public string AccountNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int CreditRating { get; set; }

    public bool PreferredVendorStatus { get; set; }

    public bool ActiveFlag { get; set; }

    public string? PurchasingWebServiceUrl { get; set; }

    public Guid? ClientId { get; set; }
}