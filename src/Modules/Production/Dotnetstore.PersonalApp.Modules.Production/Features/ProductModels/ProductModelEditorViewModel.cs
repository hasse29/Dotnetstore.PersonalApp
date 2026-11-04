using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductModels;

internal sealed partial class ProductModelEditorViewModel : ViewModelBase
{
    private readonly ProductModelDto? _existing;
    private readonly CreateProductModel.Handler _create;
    private readonly UpdateProductModel.Handler _update;
    private readonly ILocalizationService _localization;

    public ProductModelEditorViewModel(
        ProductModelDto? existing,
        CreateProductModel.Handler create,
        UpdateProductModel.Handler update,
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
            Description = existing.Description ?? string.Empty;
            Instructions = existing.Instructions ?? string.Empty;
        }
        else
        {
            // Every field starts empty.
        }
    }

    public event EventHandler<ProductModelDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "ProductModelEditor_New" : "ProductModelEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Instructions { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateProductModel.Command(Name, Description, Instructions), cancellationToken)
            : await _update.HandleAsync(new UpdateProductModel.Command(existing.Id, Name, Description, Instructions), cancellationToken);

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