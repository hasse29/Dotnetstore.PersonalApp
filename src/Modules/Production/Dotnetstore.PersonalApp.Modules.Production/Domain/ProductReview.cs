namespace Dotnetstore.PersonalApp.Modules.Production.Domain;

/// <summary>product review.</summary>
internal sealed class ProductReview
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string ReviewerName { get; set; } = string.Empty;

    public DateOnly ReviewDate { get; set; }

    public string EmailAddress { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string? Comments { get; set; }
}