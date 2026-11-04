namespace Dotnetstore.PersonalApp.Shared.Contracts.Events;

/// <summary>The warehouse's total issued so far against one sales order line. It is a total rather than an amount to add, so receiving it twice does no harm.</summary>
public sealed record GoodsIssued(Guid SalesOrderLineId, decimal TotalQuantity);
