using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.TimeReports.Features.WorkTimeTypes;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Dotnetstore.PersonalApp.Shared.Email;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;

/// <summary>
/// The signed-in user's time reports: for every client on the module's page, or for one client on that client's detail page
/// (<paramref name="fixedClientId"/>).
/// </summary>
internal sealed partial class TimeReportsViewModel(
    Guid? fixedClientId,
    ICurrentUser currentUser,
    ListTimeReports.Handler list,
    CreateTimeReport.Handler create,
    UpdateTimeReport.Handler update,
    DeleteTimeReport.Handler delete,
    ExportTimeReports.Handler export,
    ExportKilometerSummary.Handler exportKilometers,
    IFileSaveService fileSaver,
    ListWorkTimeTypes.Handler workTimeTypes,
    IClientLookup clientLookup,
    ILocalizationService localization,
    TimeProvider clock,
    ISystemMailSender? mailSender = null,
    IContactPersonLookup? contactPersons = null,
    IEnumerable<IContactInfoSource>? contactSources = null) : ViewModelBase
{
    private IReadOnlyList<ClientReference> _clients = [];
    private IReadOnlyList<WorkTimeTypeDto> _workTimeTypes = [];
    private ClientReference? _allClients;
    private bool _loading;

    public ObservableCollection<TimeReportDto> Reports { get; } = [];

    /// <summary>The client filter: an "all clients" entry followed by every client.</summary>
    public ObservableCollection<ClientReference> FilterClients { get; } = [];

    /// <summary>True on the module's own page, false when shown as a tab on a client's detail page.</summary>
    public bool IsPageLayout => fixedClientId is null;

    [ObservableProperty]
    public partial ClientReference? SelectedFilter { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial TimeReportDto? SelectedReport { get; set; }

    /// <summary>The calendar year to export; a decimal because that is what NumericUpDown binds to.</summary>
    [ObservableProperty]
    public partial decimal? ExportYear { get; set; } = clock.GetLocalNow().Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    public partial TimeReportEditorViewModel? Editor { get; set; }

    /// <summary>The send-by-email form, shown in place of the editor.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    public partial SendTimeReportViewModel? Mail { get; set; }

    public bool ShowEmptyState => Editor is null && Mail is null;

    /// <summary>False when the Email module is not installed.</summary>
    public bool CanEmail => mailSender is not null;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedFilterChanged(ClientReference? value)
    {
        if (!_loading)
        {
            CloseEditor();
            LoadCommand.Execute(null);
        }
    }

    partial void OnSelectedReportChanged(TimeReportDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(NewEditor(value));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (currentUser.User is not { } user)
        {
            StatusMessage = localization.Get("TimeReports_NotSignedIn");
            return;
        }

        _loading = true;
        try
        {
            _clients = await clientLookup.ListAsync(cancellationToken);
            _workTimeTypes = (await workTimeTypes.HandleAsync(new ListWorkTimeTypes.Query(), cancellationToken)).Items;
            RefreshFilterClients();

            var clientId = fixedClientId ?? (SelectedFilter == _allClients ? null : SelectedFilter?.Id);
            var response = await list.HandleAsync(new ListTimeReports.Query(user.Id, clientId), cancellationToken);

            Reports.Clear();
            foreach (var report in response.Items)
            {
                Reports.Add(report);
            }

            StatusMessage = localization.Format(
                "TimeReports_Summary",
                response.TotalCount,
                response.TotalHours.ToString("0.##", CultureInfo.CurrentCulture),
                response.TotalKilometers.ToString("0.#", CultureInfo.CurrentCulture));
        }
        finally
        {
            _loading = false;
        }
    }

    [RelayCommand]
    private void New()
    {
        if (currentUser.User is null)
        {
            StatusMessage = localization.Get("TimeReports_NotSignedIn");
            return;
        }

        if (_clients.Count == 0)
        {
            StatusMessage = localization.Get("TimeReports_NoClients");
            return;
        }

        if (_workTimeTypes.Count == 0)
        {
            StatusMessage = localization.Get("TimeReports_NoWorkTimeTypes");
            return;
        }

        SelectedReport = null;
        OpenEditor(NewEditor(null));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Copy()
    {
        if (SelectedReport is not { } report)
        {
            return;
        }

        // Deselect first so the delete buttons cannot act on the report being copied.
        SelectedReport = null;
        OpenEditor(NewEditor(report, isCopy: true));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedReport is not { } report || currentUser.User is not { } user)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteTimeReport.Command(report.Id, user.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private async Task ExportAsync(CancellationToken cancellationToken)
    {
        if (currentUser.User is not { } user)
        {
            StatusMessage = localization.Get("TimeReports_NotSignedIn");
            return;
        }

        var clientId = fixedClientId ?? (SelectedFilter is { } filter && filter != _allClients ? filter.Id : (Guid?)null);
        if (clientId is null)
        {
            StatusMessage = localization.Get("TimeReports_ExportSelectClient");
            return;
        }

        if (ExportYear is not { } year)
        {
            StatusMessage = localization.Describe(Error.Validation("time_report_year_invalid", "The year is not valid."));
            return;
        }

        var result = await export.HandleAsync(new ExportTimeReports.Query(user.Id, clientId.Value, (int)year), cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
            return;
        }

        await SaveExportAsync(result.Value.FileName, result.Value.Content, result.Value.ReportCount, cancellationToken);
    }

    /// <summary>Opens the form for mailing the export of the chosen client and year to the client or one of its contact persons.</summary>
    [RelayCommand]
    private async Task EmailExportAsync(CancellationToken cancellationToken)
    {
        if (mailSender is null)
        {
            return;
        }

        if (currentUser.User is not { } user)
        {
            StatusMessage = localization.Get("TimeReports_NotSignedIn");
            return;
        }

        var client = fixedClientId is { } fixedId
            ? _clients.FirstOrDefault(c => c.Id == fixedId)
            : SelectedFilter is { } filter && filter != _allClients ? filter : null;
        if (client is null)
        {
            StatusMessage = localization.Get("TimeReports_ExportSelectClient");
            return;
        }

        if (ExportYear is not { } year)
        {
            StatusMessage = localization.Describe(Error.Validation("time_report_year_invalid", "The year is not valid."));
            return;
        }

        var mail = new SendTimeReportViewModel(
            user.Id, client.Id, client.Name, (int)year, export, mailSender, contactPersons, [.. contactSources ?? []], localization);
        await mail.InitializeAsync(cancellationToken);

        CloseEditor();
        mail.Sent += OnMailSent;
        mail.Closed += OnMailClosed;
        Mail = mail;
    }

    private void OnMailSent(object? sender, string address)
    {
        CloseMail();
        StatusMessage = localization.Format("TimeReports_MailSent", address);
    }

    private void OnMailClosed(object? sender, EventArgs e) => CloseMail();

    private void CloseMail()
    {
        if (Mail is { } mail)
        {
            mail.Sent -= OnMailSent;
            mail.Closed -= OnMailClosed;
        }

        Mail = null;
    }

    /// <summary>Driving per client and month for the chosen year, whatever the client filter says.</summary>
    [RelayCommand]
    private async Task ExportKilometersAsync(CancellationToken cancellationToken)
    {
        if (currentUser.User is not { } user)
        {
            StatusMessage = localization.Get("TimeReports_NotSignedIn");
            return;
        }

        if (ExportYear is not { } year)
        {
            StatusMessage = localization.Describe(Error.Validation("time_report_year_invalid", "The year is not valid."));
            return;
        }

        var result = await exportKilometers.HandleAsync(new ExportKilometerSummary.Query(user.Id, (int)year), cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
            return;
        }

        await SaveExportAsync(result.Value.FileName, result.Value.Content, result.Value.ReportCount, cancellationToken);
    }

    private async Task SaveExportAsync(string fileName, byte[] content, int reportCount, CancellationToken cancellationToken)
    {
        var saved = await fileSaver.SaveAsync(localization.Get("TimeReports_ExportDialogTitle"), fileName, content, cancellationToken);
        if (saved is not null)
        {
            StatusMessage = localization.Format("TimeReports_Exported", reportCount, saved);
        }
    }

    private bool HasSelection() => SelectedReport is not null;

    private void RefreshFilterClients()
    {
        _allClients ??= new ClientReference(Guid.Empty, localization.Get("TimeReports_AllClients"));
        var selected = SelectedFilter ?? _allClients;

        FilterClients.Clear();
        FilterClients.Add(_allClients);
        foreach (var client in _clients)
        {
            FilterClients.Add(client);
        }

        SelectedFilter = FilterClients.FirstOrDefault(c => c.Id == selected.Id) ?? _allClients;
    }

    private TimeReportEditorViewModel NewEditor(TimeReportDto? existing, bool isCopy = false) => new(
        existing,
        isCopy,
        currentUser.User?.Id ?? Guid.Empty,
        fixedClientId,
        _clients,
        _workTimeTypes,
        DateOnly.FromDateTime(clock.GetLocalNow().Date),
        create,
        update,
        localization);

    private void OpenEditor(TimeReportEditorViewModel editor)
    {
        CloseMail();
        DetachEditor();
        Editor = editor;
        editor.Saved += OnEditorSaved;
        editor.Cancelled += OnEditorCancelled;
    }

    private void CloseEditor()
    {
        DetachEditor();
        Editor = null;
        SelectedReport = null;
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
    private async void OnEditorSaved(object? sender, TimeReportDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("TimeReports_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}