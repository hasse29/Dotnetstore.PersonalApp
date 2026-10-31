using Avalonia.Markup.Xaml;

namespace Dotnetstore.PersonalApp.Shared.Localization;

/// <summary>
/// <c>{loc:Tr Some_Key}</c> in XAML. Resolved when the view is created; changing language rebuilds the screens,
/// so the text does not need to be observable.
/// </summary>
public sealed class TrExtension(string key) : MarkupExtension
{
    public string Key { get; } = key;

    public override object ProvideValue(IServiceProvider serviceProvider) => Localizer.Instance.Get(Key);
}
