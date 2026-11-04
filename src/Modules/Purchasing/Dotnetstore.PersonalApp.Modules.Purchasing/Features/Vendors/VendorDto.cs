using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Purchasing.Domain;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.Vendors;

internal sealed record VendorDto(Guid Id, string AccountNumber, string Name, int CreditRating, bool PreferredVendorStatus, bool ActiveFlag, string? PurchasingWebServiceUrl, Guid? ClientId, string? ClientName)
{
    public static Expression<Func<Vendor, VendorDto>> Projection { get; } =
        t => new VendorDto(t.Id, t.AccountNumber, t.Name, t.CreditRating, t.PreferredVendorStatus, t.ActiveFlag, t.PurchasingWebServiceUrl, t.ClientId, null);

    public VendorDto WithNames(IReadOnlyDictionary<Guid, string> clientNames) => this with { ClientName = ExternalReferences.NameOf(clientNames, ClientId) };
}