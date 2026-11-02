using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Shared.Subjects;

/// <summary>
/// Lets the Users module show the subjects one teacher teaches as a tab on that user's detail page without referencing the Subjects module.
/// The returned view model is resolved to a view by the host's <c>ViewLocator</c>.
/// </summary>
public interface IUserSubjectsPanelFactory
{
    ViewModelBase Create(Guid userId);
}
