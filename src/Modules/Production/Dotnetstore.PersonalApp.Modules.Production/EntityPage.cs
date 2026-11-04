using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Production;

/// <summary>One sidebar page of the module, showing the view model <typeparamref name="TViewModel"/>.</summary>
internal sealed class EntityPage<TViewModel>(IServiceProvider services, string titleKey) : IModulePage
    where TViewModel : ViewModelBase
{
    public string? ModuleKey => ProductionGroup.ModuleKey;

    public ModulePageGroup? Group => ProductionGroup.Create();

    public string Title => Localizer.Instance.Get(titleKey);

    // Material "list" icon
    public string IconData => "M3 13h2v-2H3v2zm0 4h2v-2H3v2zm0-8h2V7H3v2zm4 4h14v-2H7v2zm0 4h14v-2H7v2zM7 7v2h14V7H7z";

    public ViewModelBase CreateViewModel() => services.GetRequiredService<TViewModel>();
}