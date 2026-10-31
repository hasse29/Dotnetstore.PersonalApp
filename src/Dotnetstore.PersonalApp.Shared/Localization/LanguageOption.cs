namespace Dotnetstore.PersonalApp.Shared.Localization;

/// <param name="Culture">Culture name, e.g. <c>sv</c>.</param>
/// <param name="DisplayName">The language written in itself, so it is readable whatever the current language is.</param>
public sealed record LanguageOption(string Culture, string DisplayName)
{
    public override string ToString() => DisplayName;
}
