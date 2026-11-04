using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductReviews;

internal static class CreateProductReview
{
    public sealed record Command(Guid ProductId, string ReviewerName, DateOnly ReviewDate, string EmailAddress, int Rating, string? Comments);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.ProductId).NotEqual(Guid.Empty).WithErrorCode("product_review_product_required").WithMessage("Product is required.");
            RuleFor(c => c.ReviewerName).NotEmpty().WithErrorCode("product_review_reviewer_name_required").WithMessage("Reviewer is required.").MaximumLength(50).WithErrorCode("product_review_reviewer_name_too_long").WithMessage("Reviewer is too long.");
            RuleFor(c => c.EmailAddress).NotEmpty().WithErrorCode("product_review_email_address_required").WithMessage("Email address is required.").MaximumLength(50).WithErrorCode("product_review_email_address_too_long").WithMessage("Email address is too long.");
            RuleFor(c => c.Rating).InclusiveBetween(1, 5).WithErrorCode("product_review_rating_out_of_range").WithMessage("Rating (1-5) is out of range.");
            RuleFor(c => c.Comments).MaximumLength(3850).WithErrorCode("product_review_comments_too_long").WithMessage("Comments is too long.");
        }
    }

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<ProductReviewDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var reviewerNameValue = command.ReviewerName.Trim();
            var emailAddressValue = command.EmailAddress.Trim();
            var commentsValue = ProductionRules.NormalizeOptional(command.Comments);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);


            if (!await db.Products.AnyAsync(x => x.Id == command.ProductId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("product_review_product_not_found", "Product not found.");
            }



            var entity = new ProductReview
            {
                Id = Guid.CreateVersion7(),
                ProductId = command.ProductId,
                ReviewerName = reviewerNameValue,
                ReviewDate = command.ReviewDate,
                EmailAddress = emailAddressValue,
                Rating = command.Rating,
                Comments = commentsValue,
            };

            db.ProductReviews.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.ProductReviews.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(ProductReviewDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}