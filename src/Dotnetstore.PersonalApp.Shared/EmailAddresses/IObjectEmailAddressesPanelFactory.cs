using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Shared.EmailAddresses;

/// <summary>
/// Lets any module show and manage the email addresses of one of its objects (a user, client, ...) without referencing the
/// EmailAddresses module. The returned view model is resolved to a view by the host's <c>ViewLocator</c>.
/// </summary>
public interface IObjectEmailAddressesPanelFactory
{
    ViewModelBase Create(Guid objectId);
}