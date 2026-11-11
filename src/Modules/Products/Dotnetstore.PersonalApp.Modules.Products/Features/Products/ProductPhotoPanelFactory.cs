using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Photos;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Products;

/// <summary>Builds the photo panel of a product's detail page; absent when the host offers no file dialog.</summary>
internal sealed class ProductPhotoPanelFactory(
    GetProductPhoto.Handler get,
    SetProductPhoto.Handler set,
    ILocalizationService localization,
    IFileOpenService? fileOpen = null)
{
    public PhotoPanelViewModel? Create(Guid productId, string initials) => fileOpen is null
        ? null
        : new PhotoPanelViewModel(
            initials,
            async ct => (await get.HandleAsync(new GetProductPhoto.Query(productId), ct)) is { IsSuccess: true } result ? result.Value : null,
            (photo, ct) => set.HandleAsync(new SetProductPhoto.Command(productId, photo), ct),
            fileOpen,
            localization);
}
