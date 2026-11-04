using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Production.Domain;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductReviews;

internal sealed record ProductReviewDto(Guid Id, Guid ProductId, string ProductName, string ReviewerName, DateOnly ReviewDate, string EmailAddress, int Rating, string? Comments)
{
    public static Expression<Func<ProductReview, ProductReviewDto>> Projection { get; } =
        t => new ProductReviewDto(t.Id, t.ProductId, t.Product.Name, t.ReviewerName, t.ReviewDate, t.EmailAddress, t.Rating, t.Comments);
}