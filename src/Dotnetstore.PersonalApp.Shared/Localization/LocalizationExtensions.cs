using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Shared.Localization;

public static class LocalizationExtensions
{
    /// <summary>
    /// The user-facing text for an error: the <c>Error_{code}</c> resource, or the error's own (English) message
    /// when no translation exists.
    /// </summary>
    public static string Describe(this ILocalizationService localization, Error error)
    {
        ArgumentNullException.ThrowIfNull(localization);
        ArgumentNullException.ThrowIfNull(error);

        return localization.Find($"Error_{error.Code}") ?? error.Message;
    }
}
