using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumberTypes;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers;

/// <summary>The PhoneNumbers module exposes phone number type management here; the phone numbers themselves are managed on the owners' detail pages.</summary>
internal sealed class PhoneNumbersPage(IServiceProvider services) : IModulePage
{
    public string Title => Localizer.Instance.Get("PhoneNumberTypes_Title");

    // Material "phone" icon
    public string IconData =>
        "M6.62 10.79c1.44 2.83 3.76 5.14 6.59 6.59l2.2-2.2c.27-.27.67-.36 1.02-.24 1.12.37 2.33.57 3.57.57.55 0 1 .45 1 1V20c0 .55-.45 1-1 1-9.39 0-17-7.61-17-17 0-.55.45-1 1-1h3.5c.55 0 1 .45 1 1 0 1.25.2 2.45.57 3.57.11.35.03.74-.25 1.02l-2.2 2.2z";

    public ViewModelBase CreateViewModel() => services.GetRequiredService<PhoneNumberTypesViewModel>();
}
