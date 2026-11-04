using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductSubcategories;

internal sealed partial class ProductSubcategoryEditorViewModel : ViewModelBase
{
    private readonly ProductSubcategoryDto? _existing;
    private readonly CreateProductSubcategory.Handler _create;
    private readonly UpdateProductSubcategory.Handler _update;
    private readonly ILocalizationService _localization;

    public ProductSubcategoryEditorViewModel(
        ProductSubcategoryDto? existing,
        CreateProductSubcategory.Handler create,
        UpdateProductSubcategory.Handler update,
        IReadOnlyList<OptionDto> productCategoryOptions,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        CategoryOptions = productCategoryOptions;

        if (existing is not null)
        {
            SelectedCategory = CategoryOptions.FirstOrDefault(o => o.Id == existing.CategoryId);
            Name = existing.Name;
        }
        else
        {
            // Every field starts empty.
        }
    }

    public event EventHandler<ProductSubcategoryDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "ProductSubcategoryEditor_New" : "ProductSubcategoryEditor_Edit");

    public IReadOnlyList<OptionDto> CategoryOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedCategory { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateProductSubcategory.Command(SelectedCategory?.Id ?? Guid.Empty, Name), cancellationToken)
            : await _update.HandleAsync(new UpdateProductSubcategory.Command(existing.Id, SelectedCategory?.Id ?? Guid.Empty, Name), cancellationToken);

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

}