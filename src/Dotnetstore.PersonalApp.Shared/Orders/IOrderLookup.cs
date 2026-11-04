namespace Dotnetstore.PersonalApp.Shared.Orders;

/// <summary>An order in the list a warehouse picks from: its number and who it is to or from.</summary>
public sealed record OrderInfo(Guid Id, string Number, string Party);

/// <summary>One line of an order: what was ordered, in what quantity.</summary>
public sealed record OrderLineInfo(Guid LineId, Guid ProductId, decimal Quantity);

/// <summary>Read access to orders of another module, for receiving and issuing goods against them.</summary>
public interface IOrderLookup
{
    /// <summary>The most recent orders, newest first.</summary>
    Task<IReadOnlyList<OrderInfo>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderLineInfo>> ListLinesAsync(Guid orderId, CancellationToken cancellationToken);
}
