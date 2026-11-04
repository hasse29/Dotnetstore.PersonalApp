using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Production.Persistence;

internal sealed class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("ProductReviews");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.ReviewerName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReviewDate).IsRequired();
        builder.Property(x => x.EmailAddress).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Rating).IsRequired();
        builder.Property(x => x.Comments).HasMaxLength(3850);
    }
}