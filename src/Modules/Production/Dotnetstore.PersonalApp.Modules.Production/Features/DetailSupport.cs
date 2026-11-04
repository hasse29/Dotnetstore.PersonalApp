using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Production.Features;

/// <summary>A page with tabs that a list page shows in place of itself; Back raises <see cref="Closed"/>.</summary>
internal abstract partial class DetailViewModel : ViewModelBase
{
    public event EventHandler? Closed;

    [RelayCommand]
    private void Back() => Closed?.Invoke(this, EventArgs.Empty);
}

internal interface IDetailFactory<in TDto>
{
    DetailViewModel Create(TDto dto);
}

/// <summary>Builds the detail page for a row; the page's other constructor arguments come from the container.</summary>
internal sealed class DetailFactory<TDto, TDetail>(IServiceProvider services) : IDetailFactory<TDto>
    where TDetail : DetailViewModel
{
    public DetailViewModel Create(TDto dto) => ActivatorUtilities.CreateInstance<TDetail>(services, dto!);
}