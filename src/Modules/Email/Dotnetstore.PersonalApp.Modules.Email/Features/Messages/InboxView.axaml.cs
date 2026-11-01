using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

internal sealed partial class InboxView : UserControl
{
    private DispatcherTimer? _timer;

    public InboxView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is InboxViewModel viewModel && viewModel.Accounts.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }

        // The view model decides whether a tick does anything; the view only keeps time while it is on screen.
        StopTimer();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(InboxViewModel.AutoRefreshMinutes) };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        StopTimer();
        base.OnUnloaded(e);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (DataContext is InboxViewModel viewModel)
        {
            viewModel.AutoRefreshCommand.Execute(null);
        }
    }

    private void StopTimer()
    {
        if (_timer is not null)
        {
            _timer.Stop();
            _timer.Tick -= OnTick;
            _timer = null;
        }
    }
}
