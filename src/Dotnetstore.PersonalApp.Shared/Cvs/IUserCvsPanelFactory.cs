using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Shared.Cvs;

/// <summary>
/// Lets the Users module show the CVs of one user as a tab on that user's detail page without referencing the Cvs module.
/// The returned view model is resolved to a view by the host's <c>ViewLocator</c>.
/// </summary>
public interface IUserCvsPanelFactory
{
    ViewModelBase Create(Guid userId);
}
