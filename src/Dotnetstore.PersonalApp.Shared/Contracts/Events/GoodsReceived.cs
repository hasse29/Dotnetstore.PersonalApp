namespace Dotnetstore.PersonalApp.Shared.Contracts.Events;

/// <summary>Published by the Inventory module when goods were received against a purchase order line.</summary>
public sealed record GoodsReceived(Guid PurchaseOrderLineId, decimal Quantity);
