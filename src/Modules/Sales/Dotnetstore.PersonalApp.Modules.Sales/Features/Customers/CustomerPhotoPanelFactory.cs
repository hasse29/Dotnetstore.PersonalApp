using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Photos;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

/// <summary>Builds the photo panel of a customer's detail page; absent when the host offers no file dialog.</summary>
internal sealed class CustomerPhotoPanelFactory(
    GetCustomerPhoto.Handler get,
    SetCustomerPhoto.Handler set,
    ILocalizationService localization,
    IFileOpenService? fileOpen = null)
{
    public PhotoPanelViewModel? Create(Guid customerId, string initials) => fileOpen is null
        ? null
        : new PhotoPanelViewModel(
            initials,
            async ct => (await get.HandleAsync(new GetCustomerPhoto.Query(customerId), ct)) is { IsSuccess: true } result ? result.Value : null,
            (photo, ct) => set.HandleAsync(new SetCustomerPhoto.Command(customerId, photo), ct),
            fileOpen,
            localization);
}
