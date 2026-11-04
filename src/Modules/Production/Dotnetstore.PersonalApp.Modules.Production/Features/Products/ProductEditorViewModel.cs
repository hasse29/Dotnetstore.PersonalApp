using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.Products;

internal sealed partial class ProductEditorViewModel : ViewModelBase
{
    private readonly ProductDto? _existing;
    private readonly CreateProduct.Handler _create;
    private readonly UpdateProduct.Handler _update;
    private readonly ILocalizationService _localization;

    public ProductEditorViewModel(
        ProductDto? existing,
        CreateProduct.Handler create,
        UpdateProduct.Handler update,
        IReadOnlyList<OptionDto> unitMeasureOptions,
        IReadOnlyList<OptionDto> productSubcategoryOptions,
        IReadOnlyList<OptionDto> productModelOptions,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        SizeUnitOptions = [OptionDto.None(localization.Get("Production_None")), .. unitMeasureOptions];
        WeightUnitOptions = [OptionDto.None(localization.Get("Production_None")), .. unitMeasureOptions];
        ProductLineOptions =
        [
            new ChoiceOption("N", localization.Get("ProductChoice_ProductLine_N")),
            new ChoiceOption("R", localization.Get("ProductChoice_ProductLine_R")),
            new ChoiceOption("M", localization.Get("ProductChoice_ProductLine_M")),
            new ChoiceOption("T", localization.Get("ProductChoice_ProductLine_T")),
            new ChoiceOption("S", localization.Get("ProductChoice_ProductLine_S")),
        ];
        ClassOptions =
        [
            new ChoiceOption("N", localization.Get("ProductChoice_Class_N")),
            new ChoiceOption("H", localization.Get("ProductChoice_Class_H")),
            new ChoiceOption("M", localization.Get("ProductChoice_Class_M")),
            new ChoiceOption("L", localization.Get("ProductChoice_Class_L")),
        ];
        StyleOptions =
        [
            new ChoiceOption("N", localization.Get("ProductChoice_Style_N")),
            new ChoiceOption("W", localization.Get("ProductChoice_Style_W")),
            new ChoiceOption("M", localization.Get("ProductChoice_Style_M")),
            new ChoiceOption("U", localization.Get("ProductChoice_Style_U")),
        ];
        SubcategoryOptions = [OptionDto.None(localization.Get("Production_None")), .. productSubcategoryOptions];
        ModelOptions = [OptionDto.None(localization.Get("Production_None")), .. productModelOptions];

        if (existing is not null)
        {
            Name = existing.Name;
            ProductNumber = existing.ProductNumber;
            MakeFlag = existing.MakeFlag;
            FinishedGoodsFlag = existing.FinishedGoodsFlag;
            Color = existing.Color ?? string.Empty;
            SafetyStockLevel = existing.SafetyStockLevel;
            ReorderPoint = existing.ReorderPoint;
            StandardCost = existing.StandardCost;
            ListPrice = existing.ListPrice;
            Size = existing.Size ?? string.Empty;
            SelectedSizeUnit = SizeUnitOptions.FirstOrDefault(o => o.Id == existing.SizeUnitId) ?? SizeUnitOptions[0];
            Weight = existing.Weight;
            SelectedWeightUnit = WeightUnitOptions.FirstOrDefault(o => o.Id == existing.WeightUnitId) ?? WeightUnitOptions[0];
            DaysToManufacture = existing.DaysToManufacture;
            SelectedProductLine = ProductLineOptions.FirstOrDefault(o => o.Code == existing.ProductLine);
            SelectedClass = ClassOptions.FirstOrDefault(o => o.Code == existing.Class);
            SelectedStyle = StyleOptions.FirstOrDefault(o => o.Code == existing.Style);
            SelectedSubcategory = SubcategoryOptions.FirstOrDefault(o => o.Id == existing.SubcategoryId) ?? SubcategoryOptions[0];
            SelectedModel = ModelOptions.FirstOrDefault(o => o.Id == existing.ModelId) ?? ModelOptions[0];
            SellStartDate = existing.SellStartDate.ToDateTime(TimeOnly.MinValue);
            SellEndDate = existing.SellEndDate?.ToDateTime(TimeOnly.MinValue);
            DiscontinuedDate = existing.DiscontinuedDate?.ToDateTime(TimeOnly.MinValue);
        }
        else
        {
            SelectedSizeUnit = SizeUnitOptions[0];
            SelectedWeightUnit = WeightUnitOptions[0];
            SelectedProductLine = ProductLineOptions[0];
            SelectedClass = ClassOptions[0];
            SelectedStyle = StyleOptions[0];
            SelectedSubcategory = SubcategoryOptions[0];
            SelectedModel = ModelOptions[0];
            SellStartDate = clock.GetLocalNow().Date;
        }
    }

    public event EventHandler<ProductDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "ProductEditor_New" : "ProductEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ProductNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool MakeFlag { get; set; }

    [ObservableProperty]
    public partial bool FinishedGoodsFlag { get; set; }

    [ObservableProperty]
    public partial string Color { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal? SafetyStockLevel { get; set; }

    [ObservableProperty]
    public partial decimal? ReorderPoint { get; set; }

    [ObservableProperty]
    public partial decimal? StandardCost { get; set; }

    [ObservableProperty]
    public partial decimal? ListPrice { get; set; }

    [ObservableProperty]
    public partial string Size { get; set; } = string.Empty;

    public IReadOnlyList<OptionDto> SizeUnitOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedSizeUnit { get; set; }

    [ObservableProperty]
    public partial decimal? Weight { get; set; }

    public IReadOnlyList<OptionDto> WeightUnitOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedWeightUnit { get; set; }

    [ObservableProperty]
    public partial decimal? DaysToManufacture { get; set; }

    public IReadOnlyList<ChoiceOption> ProductLineOptions { get; }

    [ObservableProperty]
    public partial ChoiceOption? SelectedProductLine { get; set; }

    public IReadOnlyList<ChoiceOption> ClassOptions { get; }

    [ObservableProperty]
    public partial ChoiceOption? SelectedClass { get; set; }

    public IReadOnlyList<ChoiceOption> StyleOptions { get; }

    [ObservableProperty]
    public partial ChoiceOption? SelectedStyle { get; set; }

    public IReadOnlyList<OptionDto> SubcategoryOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedSubcategory { get; set; }

    public IReadOnlyList<OptionDto> ModelOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedModel { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? SellStartDate { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? SellEndDate { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? DiscontinuedDate { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateProduct.Command(Name, ProductNumber, MakeFlag, FinishedGoodsFlag, Color, (int)(SafetyStockLevel ?? 0), (int)(ReorderPoint ?? 0), StandardCost ?? 0m, ListPrice ?? 0m, Size, ToId(SelectedSizeUnit), Weight, ToId(SelectedWeightUnit), (int)(DaysToManufacture ?? 0), SelectedProductLine?.Code ?? string.Empty, SelectedClass?.Code ?? string.Empty, SelectedStyle?.Code ?? string.Empty, ToId(SelectedSubcategory), ToId(SelectedModel), DateOnly.FromDateTime(SellStartDate ?? DateTime.MinValue), ToDate(SellEndDate), ToDate(DiscontinuedDate)), cancellationToken)
            : await _update.HandleAsync(new UpdateProduct.Command(existing.Id, Name, ProductNumber, MakeFlag, FinishedGoodsFlag, Color, (int)(SafetyStockLevel ?? 0), (int)(ReorderPoint ?? 0), StandardCost ?? 0m, ListPrice ?? 0m, Size, ToId(SelectedSizeUnit), Weight, ToId(SelectedWeightUnit), (int)(DaysToManufacture ?? 0), SelectedProductLine?.Code ?? string.Empty, SelectedClass?.Code ?? string.Empty, SelectedStyle?.Code ?? string.Empty, ToId(SelectedSubcategory), ToId(SelectedModel), DateOnly.FromDateTime(SellStartDate ?? DateTime.MinValue), ToDate(SellEndDate), ToDate(DiscontinuedDate)), cancellationToken);

        if (result.IsSuccess)
        {
            Saved?.Invoke(this, result.Value);
        }
        else
        {
            ErrorMessage = _localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);

    private static DateOnly? ToDate(DateTime? value) => value is { } v ? DateOnly.FromDateTime(v) : null;

    private static Guid? ToId(OptionDto? option) => option is { } o && o.Id != Guid.Empty ? o.Id : null;
}