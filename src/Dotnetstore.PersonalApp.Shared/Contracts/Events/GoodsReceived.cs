namespace Dotnetstore.PersonalApp.Shared.Contracts.Events;

/// <summary>The warehouse's total received so far against one purchase order line. It is a total rather than an amount to add, so receiving it twice does no harm.</summary>
public sealed record GoodsReceived(Guid PurchaseOrderLineId, decimal TotalQuantity);
