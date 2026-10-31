using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumbers;

internal sealed partial class ObjectPhoneNumbersView : UserControl
{
    public ObjectPhoneNumbersView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ObjectPhoneNumbersViewModel viewModel && viewModel.PhoneNumbers.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
