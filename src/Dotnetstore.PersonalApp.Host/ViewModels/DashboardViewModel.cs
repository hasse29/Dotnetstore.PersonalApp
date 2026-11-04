using System.Collections.ObjectModel;
using System.Data.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Dashboard;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Host.ViewModels;

/// <summary>The landing page: a greeting and one panel for every enabled module that has a dashboard widget.</summary>
internal sealed partial class DashboardViewModel(
    IEnumerable<IDashboardWidget> widgets,
    ICurrentUser currentUser,
    ModuleSettings moduleSettings,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<DashboardPanel> Panels { get; } = [];

    public string Greeting
    {
        get
        {
            var hour = clock.GetLocalNow().Hour;
            var key = hour < 12 ? "Dashboard_GoodMorning" : hour < 18 ? "Dashboard_GoodAfternoon" : "Dashboard_GoodEvening";
            return localization.Format(key, currentUser.User?.FullName ?? string.Empty).Trim();
        }
    }

    public string DateText => clock.GetLocalNow().ToString("D", localization.Culture);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmpty))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmpty))]
    public partial bool HasPanels { get; set; }

    public bool ShowEmpty => !HasPanels && !IsLoading;

    [ObservableProperty]
    public partial bool SomeUnavailable { get; set; }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        SomeUnavailable = false;

        var context = new DashboardContext(currentUser.User?.Id ?? Guid.Empty, DateOnly.FromDateTime(clock.GetLocalNow().DateTime));
        var enabled = widgets
            .Where(widget => moduleSettings.IsEnabled(widget.ModuleKey))
            .OrderBy(widget => widget.Order)
            .ToList();

        // Each widget reads its own module's database, so they run side by side; one that cannot be read must not hide the rest.
        var loading = enabled.Select(widget => LoadOneAsync(widget, context, cancellationToken)).ToList();
        var loaded = await Task.WhenAll(loading);

        Panels.Clear();
        foreach (var panel in loaded.OfType<DashboardPanel>())
        {
            Panels.Add(panel);
        }

        SomeUnavailable = loaded.Any(panel => panel is null);
        HasPanels = Panels.Count > 0;
        IsLoading = false;
    }

    private static async Task<DashboardPanel?> LoadOneAsync(IDashboardWidget widget, DashboardContext context, CancellationToken cancellationToken)
    {
        try
        {
            return await widget.LoadAsync(context, cancellationToken);
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException or IOException)
        {
            return null;
        }
    }
}
