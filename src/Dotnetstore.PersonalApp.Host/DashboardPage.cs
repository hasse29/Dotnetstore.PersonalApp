using Dotnetstore.PersonalApp.Host.ViewModels;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Host;

/// <summary>The page you land on after signing in. It is registered before every module, so it is the first entry in the menu.</summary>
internal sealed class DashboardPage(IServiceProvider services) : IModulePage
{
    public string Title => Localizer.Instance.Get("Dashboard_Title");

    // Material "dashboard" icon
    public string IconData => "M3 13h8V3H3v10zm0 8h8v-6H3v6zm10 0h8V11h-8v10zm0-18v6h8V3h-8z";

    public ViewModelBase CreateViewModel() => services.GetRequiredService<DashboardViewModel>();
}
