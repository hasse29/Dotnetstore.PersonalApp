using Dotnetstore.PersonalApp.Modules.Students.Features.Students;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Students;

namespace Dotnetstore.PersonalApp.Modules.Students;

internal sealed class StudentsPanelFactory(StudentsViewModelFactory factory) : IClientStudentsPanelFactory
{
    public ViewModelBase Create(Guid clientId) => factory.Create(clientId);
}
