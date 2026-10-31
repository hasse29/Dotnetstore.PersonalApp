using CommunityToolkit.Mvvm.ComponentModel;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Host.Startup;

namespace Dotnetstore.PersonalApp.Host.ViewModels;

internal sealed partial class SplashViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial double Progress { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = "Starting...";

    [ObservableProperty]
    public partial bool HasFailed { get; set; }

    public void Apply(StartupProgress progress)
    {
        Progress = progress.Fraction * 100;
        Status = progress.Message;
    }

    public void Fail(string message)
    {
        HasFailed = true;
        Status = message;
    }
}
