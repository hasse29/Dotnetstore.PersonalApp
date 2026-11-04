using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Host.Updates;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Host.ViewModels;

/// <summary>Whether to look for a newer release at all; read from settings.json when the app starts.</summary>
internal sealed record UpdateOptions(bool Enabled);

/// <summary>
/// The strip above the page that says a newer version is out. The check runs once per run of the app, so signing out and in
/// again does not ask GitHub again; dismissing the strip hides it until the next run.
/// </summary>
internal sealed partial class UpdateBannerViewModel(IUpdateChecker checker, IUriLauncher launcher, UpdateOptions options, ILocalizationService localization) : ViewModelBase
{
    private bool _checked;
    private UpdateInfo? _update;
    private bool _dismissed;

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [RelayCommand]
    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        if (!_checked && options.Enabled)
        {
            _checked = true;
            _update = await checker.CheckAsync(cancellationToken);
        }

        Show();
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (_update is { } update && Uri.TryCreate(update.Url, UriKind.Absolute, out var uri))
        {
            await launcher.LaunchAsync(uri);
        }
    }

    [RelayCommand]
    private void Dismiss()
    {
        _dismissed = true;
        Show();
    }

    private void Show()
    {
        IsVisible = _update is not null && !_dismissed;
        Message = _update is { } update
            ? localization.Format("Update_Available", update.Version.ToString(3), GitHubUpdateChecker.Current.ToString(3))
            : string.Empty;
    }
}
