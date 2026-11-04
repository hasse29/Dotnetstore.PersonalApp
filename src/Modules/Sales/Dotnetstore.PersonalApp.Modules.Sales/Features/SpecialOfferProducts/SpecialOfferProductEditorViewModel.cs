using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOfferProducts;

internal sealed partial class SpecialOfferProductEditorViewModel : ViewModelBase
{
    private readonly SpecialOfferProductDto? _existing;
    private readonly CreateSpecialOfferProduct.Handler _create;
    private readonly UpdateSpecialOfferProduct.Handler _update;
    private readonly ILocalizationService _localization;

    public SpecialOfferProductEditorViewModel(
        SpecialOfferProductDto? existing,
        CreateSpecialOfferProduct.Handler create,
        UpdateSpecialOfferProduct.Handler update,
        IReadOnlyList<OptionDto> specialOfferOptions,
        IReadOnlyList<OptionDto> productOptions,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        SpecialOfferOptions = specialOfferOptions;
        ProductOptions = productOptions;

        if (existing is not null)
        {
            SelectedSpecialOffer = SpecialOfferOptions.FirstOrDefault(o => o.Id == existing.SpecialOfferId);
            SelectedProduct = ProductOptions.FirstOrDefault(o => o.Id == existing.ProductId);
        }
        else
        {
            // Every field starts empty.
        }
    }

    public event EventHandler<SpecialOfferProductDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "SpecialOfferProductEditor_New" : "SpecialOfferProductEditor_Edit");

    public IReadOnlyList<OptionDto> SpecialOfferOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedSpecialOffer { get; set; }

    public IReadOnlyList<OptionDto> ProductOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateSpecialOfferProduct.Command(SelectedSpecialOffer?.Id ?? Guid.Empty, SelectedProduct?.Id ?? Guid.Empty), cancellationToken)
            : await _update.HandleAsync(new UpdateSpecialOfferProduct.Command(existing.Id, SelectedSpecialOffer?.Id ?? Guid.Empty, SelectedProduct?.Id ?? Guid.Empty), cancellationToken);

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