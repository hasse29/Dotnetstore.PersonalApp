using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Features.PhoneNumberTypes;

internal sealed partial class PhoneNumberTypesView : UserControl
{
    public PhoneNumberTypesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is PhoneNumberTypesViewModel viewModel && viewModel.PhoneNumberTypes.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
