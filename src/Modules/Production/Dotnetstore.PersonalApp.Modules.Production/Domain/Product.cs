namespace Dotnetstore.PersonalApp.Modules.Production.Domain;

/// <summary>product.</summary>
internal sealed class Product
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ProductNumber { get; set; } = string.Empty;

    public bool MakeFlag { get; set; }

    public bool FinishedGoodsFlag { get; set; }

    public string? Color { get; set; }

    public int SafetyStockLevel { get; set; }

    public int ReorderPoint { get; set; }

    public decimal StandardCost { get; set; }

    public decimal ListPrice { get; set; }

    public string? Size { get; set; }

    public Guid? SizeUnitId { get; set; }

    public UnitMeasure? SizeUnit { get; set; }

    public decimal? Weight { get; set; }

    public Guid? WeightUnitId { get; set; }

    public UnitMeasure? WeightUnit { get; set; }

    public int DaysToManufacture { get; set; }

    public string ProductLine { get; set; } = string.Empty;

    public string Class { get; set; } = string.Empty;

    public string Style { get; set; } = string.Empty;

    public Guid? SubcategoryId { get; set; }

    public ProductSubcategory? Subcategory { get; set; }

    public Guid? ModelId { get; set; }

    public ProductModel? Model { get; set; }

    public DateOnly SellStartDate { get; set; }

    public DateOnly? SellEndDate { get; set; }

    public DateOnly? DiscontinuedDate { get; set; }
}