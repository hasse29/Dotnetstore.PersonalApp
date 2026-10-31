using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Shared.PhoneNumbers;

/// <summary>
/// Lets any module show and manage the phone numbers of one of its objects (a user, client, ...) without referencing the
/// PhoneNumbers module. The returned view model is resolved to a view by the host's <c>ViewLocator</c>.
/// </summary>
public interface IObjectPhoneNumbersPanelFactory
{
    ViewModelBase Create(Guid objectId);
}