using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Shared.Students;

/// <summary>
/// Lets the Clients module show the students of one school (a client) as a tab on that client's detail page without referencing the Students module.
/// The returned view model is resolved to a view by the host's <c>ViewLocator</c>.
/// </summary>
public interface IClientStudentsPanelFactory
{
    /// <summary>Null while the School module is switched off, which hides the tab.</summary>
    ViewModelBase? Create(Guid clientId);
}
