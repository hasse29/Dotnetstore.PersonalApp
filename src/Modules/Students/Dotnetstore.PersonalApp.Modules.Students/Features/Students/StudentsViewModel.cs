using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Students;

/// <summary>
/// The students of one school (a tab on the client, <paramref name="clientId"/> set) or of every school (the module's own page, <paramref name="clientId"/> null,
/// with a school filter): list, editor and a detail page per student.
/// </summary>
internal sealed partial class StudentsViewModel(
    Guid? clientId,
    ListStudents.Handler list,
    CreateStudent.Handler create,
    UpdateStudent.Handler update,
    DeleteStudent.Handler delete,
    ILocalizationService localization,
    IObjectAddressesPanelFactory? addressesFactory = null,
    IObjectEmailAddressesPanelFactory? emailAddressesFactory = null,
    IObjectPhoneNumbersPanelFactory? phoneNumbersFactory = null,
    StudentPhotoPanelFactory? photoFactory = null,
    IClientLookup? clients = null) : ViewModelBase
{
    private bool _restoringSelection;
    private Dictionary<Guid, string> _schoolNames = [];

    /// <summary>True on the module's own page, false as a tab on a client; the page adds a title, a school filter and a school column.</summary>
    public bool IsPageLayout => clientId is null;

    /// <summary>The filter choices on the page: an "all schools" entry (empty id) followed by every school.</summary>
    public ObservableCollection<ClientReference> Schools { get; } = [];

    [ObservableProperty]
    public partial ClientReference? SelectedSchoolFilter { get; set; }

    partial void OnSelectedSchoolFilterChanged(ClientReference? value)
    {
        // Rebuilding the list of schools makes the combo box report null for a moment; only a real choice reloads.
        if (_restoringSelection || value is null)
        {
            return;
        }

        CloseEditor();
        LoadCommand.Execute(null);
    }

    public ObservableCollection<StudentDto> Students { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenDetailCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial StudentDto? SelectedStudent { get; set; }

    [ObservableProperty]
    public partial StudentEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial StudentDetailViewModel? Detail { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedStudentChanged(StudentDto? value)
    {
        if (_restoringSelection)
        {
            return;
        }

        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new StudentEditorViewModel(clientId, value, create, update, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        await RefreshSchoolsAsync(cancellationToken);

        var schoolId = clientId ?? (SelectedSchoolFilter is { } filter && filter.Id != Guid.Empty ? filter.Id : null);
        var students = (await list.HandleAsync(new ListStudents.Query(schoolId), cancellationToken))
            .Select(s => IsPageLayout ? s with { SchoolName = _schoolNames.GetValueOrDefault(s.ClientId, "-") } : s)
            .ToList();

        // Reloading must not reopen the editor, so the selection is put back without the change handler acting on it.
        var selectedId = SelectedStudent?.Id;
        _restoringSelection = true;
        try
        {
            Students.Clear();
            foreach (var student in students)
            {
                Students.Add(student);
            }

            SelectedStudent = Students.FirstOrDefault(p => p.Id == selectedId);
        }
        finally
        {
            _restoringSelection = false;
        }

        StatusMessage = localization.Format("Students_Count", students.Count);
    }

    [RelayCommand]
    private void New()
    {
        SelectedStudent = null;
        OpenEditor(new StudentEditorViewModel(clientId, null, create, update, localization, Schools.Where(s => s.Id != Guid.Empty).ToList(),
            SelectedSchoolFilter is { } filter && filter.Id != Guid.Empty ? filter.Id : null));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenDetail()
    {
        if (SelectedStudent is not { } student)
        {
            return;
        }

        var detail = new StudentDetailViewModel(student, addressesFactory, emailAddressesFactory, phoneNumbersFactory,
            photoFactory is null ? null : initials => photoFactory.Create(student.Id, initials));
        detail.Closed += OnDetailClosed;
        Detail = detail;
    }

    private void OnDetailClosed(object? sender, EventArgs e)
    {
        if (Detail is { } detail)
        {
            detail.Closed -= OnDetailClosed;
        }

        Detail = null;

        // The photo may have changed, and the list shows its thumbnail.
        LoadCommand.Execute(null);
    }

    // On the page the school names and the filter come from the Clients module; they are refreshed on every load so new schools show up.
    private async Task RefreshSchoolsAsync(CancellationToken cancellationToken)
    {
        if (!IsPageLayout || clients is null)
        {
            return;
        }

        var schools = await clients.ListAsync(cancellationToken);
        _schoolNames = schools.ToDictionary(s => s.Id, s => s.Name);

        var all = new ClientReference(Guid.Empty, localization.Get("Students_AllSchools"));
        var wanted = new[] { all }.Concat(schools).ToList();
        if (Schools.Select(s => s.Id).SequenceEqual(wanted.Select(s => s.Id)))
        {
            return;
        }

        var selectedId = SelectedSchoolFilter?.Id ?? Guid.Empty;
        _restoringSelection = true;
        try
        {
            Schools.Clear();
            foreach (var school in wanted)
            {
                Schools.Add(school);
            }

            SelectedSchoolFilter = Schools.FirstOrDefault(s => s.Id == selectedId) ?? Schools[0];
        }
        finally
        {
            _restoringSelection = false;
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedStudent is not { } student)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteStudent.Command(student.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedStudent is not null;

    private void OpenEditor(StudentEditorViewModel editor)
    {
        DetachEditor();
        Editor = editor;
        editor.Saved += OnEditorSaved;
        editor.Cancelled += OnEditorCancelled;
    }

    private void CloseEditor()
    {
        DetachEditor();
        Editor = null;
        SelectedStudent = null;
        IsConfirmingDelete = false;
    }

    private void DetachEditor()
    {
        if (Editor is { } editor)
        {
            editor.Saved -= OnEditorSaved;
            editor.Cancelled -= OnEditorCancelled;
        }
    }

    // Event handler, so it has to be async void; LoadAsync reports its own outcome on the status line.
    private async void OnEditorSaved(object? sender, StudentDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Format("Students_Saved", saved.FullName);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
