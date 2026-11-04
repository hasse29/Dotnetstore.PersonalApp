using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductCategories;

internal sealed partial class ProductCategoryEditorViewModel : ViewModelBase
{
    private readonly ProductCategoryDto? _existing;
    private readonly CreateProductCategory.Handler _create;
    private readonly UpdateProductCategory.Handler _update;
    private readonly ILocalizationService _localization;

    public ProductCategoryEditorViewModel(
        ProductCategoryDto? existing,
        CreateProductCategory.Handler create,
        UpdateProductCategory.Handler update,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;



        if (existing is not null)
        {
            Name = existing.Name;
        }
        else
        {
            // Every field starts empty.
        }
    }

    public event EventHandler<ProductCategoryDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "ProductCategoryEditor_New" : "ProductCategoryEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateProductCategory.Command(Name), cancellationToken)
            : await _update.HandleAsync(new UpdateProductCategory.Command(existing.Id, Name), cancellationToken);

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