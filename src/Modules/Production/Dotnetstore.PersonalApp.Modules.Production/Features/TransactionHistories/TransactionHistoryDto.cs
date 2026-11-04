using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Production.Domain;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.TransactionHistories;

internal sealed record TransactionHistoryDto(Guid Id, Guid ProductId, string ProductName, string? ReferenceOrder, DateOnly TransactionDate, string TransactionType, int Quantity, decimal ActualCost)
{
    public static Expression<Func<TransactionHistory, TransactionHistoryDto>> Projection { get; } =
        t => new TransactionHistoryDto(t.Id, t.ProductId, t.Product.Name, t.ReferenceOrder, t.TransactionDate, t.TransactionType, t.Quantity, t.ActualCost);
}