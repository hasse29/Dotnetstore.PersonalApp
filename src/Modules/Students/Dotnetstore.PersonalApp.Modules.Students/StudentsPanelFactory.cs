using Dotnetstore.PersonalApp.Modules.Students.Features.Students;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Students;

namespace Dotnetstore.PersonalApp.Modules.Students;

internal sealed class StudentsPanelFactory(StudentsViewModelFactory factory, IModuleAvailability? availability = null) : IClientStudentsPanelFactory
{
    public ViewModelBase? Create(Guid clientId) => availability?.IsEnabled(SchoolGroup.ModuleKey) == false ? null : factory.Create(clientId);
}
