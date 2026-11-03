using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Students.Features.Notes;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.Photos;
using Dotnetstore.PersonalApp.Shared.Students;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Students;

internal sealed partial class StudentDetailViewModel(
    StudentDto student,
    IObjectAddressesPanelFactory? addressesFactory = null,
    IObjectEmailAddressesPanelFactory? emailAddressesFactory = null,
    IObjectPhoneNumbersPanelFactory? phoneNumbersFactory = null,
    Func<string, PhotoPanelViewModel?>? photoFactory = null,
    StudentNotesViewModel? notes = null,
    IStudentGradesPanelFactory? gradesFactory = null) : ViewModelBase
{
    public StudentDto Student { get; } = student;

    /// <summary>The photo with its Change and Remove buttons; null when the host has no file dialog.</summary>
    public PhotoPanelViewModel? Photo { get; } = photoFactory?.Invoke(student.Initials);

    /// <summary>The student's addresses tab, provided by the Addresses module; null when that module is not installed.</summary>
    public ViewModelBase? Addresses { get; } = addressesFactory?.Create(student.Id);

    /// <summary>The email addresses tab, provided by the EmailAddresses module; null when that module is not installed.</summary>
    public ViewModelBase? EmailAddresses { get; } = emailAddressesFactory?.Create(student.Id);

    /// <summary>The phone numbers tab, provided by the PhoneNumbers module; null when that module is not installed.</summary>
    public ViewModelBase? PhoneNumbers { get; } = phoneNumbersFactory?.Create(student.Id);

    /// <summary>The running, dated notes about the student.</summary>
    public StudentNotesViewModel? Notes { get; } = notes;

    /// <summary>The grades tab, provided by the Grades module; null when that module is not installed.</summary>
    public ViewModelBase? Grades { get; } = gradesFactory?.Create(student.Id);

    public event EventHandler? Closed;

    [RelayCommand]
    private void Back() => Closed?.Invoke(this, EventArgs.Empty);
}
