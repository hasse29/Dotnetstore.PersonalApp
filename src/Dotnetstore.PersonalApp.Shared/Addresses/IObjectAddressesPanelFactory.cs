using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Shared.Addresses;

/// <summary>
/// Lets any module show and manage the addresses of one of its objects (a user, client, ...) without referencing the
/// Addresses module. The returned view model is resolved to a view by the host's <c>ViewLocator</c>.
/// </summary>
public interface IObjectAddressesPanelFactory
{
    ViewModelBase Create(Guid objectId);
}
