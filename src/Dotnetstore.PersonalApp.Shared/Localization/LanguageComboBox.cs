using Avalonia.Controls;

namespace Dotnetstore.PersonalApp.Shared.Localization;

/// <summary>Drop-in language picker; place it on any screen with <c>&lt;loc:LanguageComboBox/&gt;</c>.</summary>
public sealed class LanguageComboBox : ComboBox
{
    public LanguageComboBox()
    {
        var localizer = Localizer.Instance;
        ItemsSource = localizer.Languages;
        SelectedItem = localizer.Languages.FirstOrDefault(l => l.Culture == localizer.Culture.Name);
        MinWidth = 130;

        SelectionChanged += (_, _) =>
        {
            if (SelectedItem is LanguageOption option)
            {
                localizer.SetCulture(option.Culture);
            }
        };
    }

    protected override Type StyleKeyOverride => typeof(ComboBox);
}
