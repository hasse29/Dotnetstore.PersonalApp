using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Students;

internal sealed partial class StudentEditorViewModel : ViewModelBase
{
    private readonly Guid? _clientId;
    private readonly StudentDto? _existing;
    private readonly CreateStudent.Handler _create;
    private readonly UpdateStudent.Handler _update;
    private readonly ILocalizationService _localization;

    /// <param name="clientId">The school when the editor sits in a school's tab; null on the Students page, where the user picks one.</param>
    /// <param name="schools">The schools to pick from when <paramref name="clientId"/> is null.</param>
    /// <param name="defaultSchool">The school to preselect in the picker, e.g. the one the list is filtered on.</param>
    public StudentEditorViewModel(
        Guid? clientId,
        StudentDto? existing,
        CreateStudent.Handler create,
        UpdateStudent.Handler update,
        ILocalizationService localization,
        IReadOnlyList<ClientReference>? schools = null,
        Guid? defaultSchool = null)
    {
        _clientId = clientId;
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;
        Schools = schools ?? [];
        SelectedSchool = Schools.FirstOrDefault(s => s.Id == defaultSchool);

        if (existing is not null)
        {
            FirstName = existing.FirstName;
            LastName = existing.LastName;
            ClassName = existing.ClassName ?? string.Empty;
            Notes = existing.Notes ?? string.Empty;
        }
    }

    public event EventHandler<StudentDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "StudentEditor_New" : "StudentEditor_Edit");

    public IReadOnlyList<ClientReference> Schools { get; }

    /// <summary>A student cannot move to another school, so the picker is only for new students outside a school's tab.</summary>
    public bool ShowSchoolPicker => _clientId is null && _existing is null;

    [ObservableProperty]
    public partial ClientReference? SelectedSchool { get; set; }

    [ObservableProperty]
    public partial string FirstName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LastName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ClassName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        Result<StudentDto> result;
        if (_existing is not null)
        {
            result = await _update.HandleAsync(new UpdateStudent.Command(_existing.Id, FirstName, LastName, ClassName, Notes), cancellationToken);
        }
        else if ((_clientId ?? SelectedSchool?.Id) is { } school)
        {
            result = await _create.HandleAsync(new CreateStudent.Command(school, FirstName, LastName, ClassName, Notes), cancellationToken);
        }
        else
        {
            ErrorMessage = _localization.Describe(Error.Validation("student_school_required", "Choose a school."));
            return;
        }

        if (result.IsSuccess)
        {
            Saved?.Invoke(this, result.Value);
        }
        else
        {
            ErrorMessage = _localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);
}
