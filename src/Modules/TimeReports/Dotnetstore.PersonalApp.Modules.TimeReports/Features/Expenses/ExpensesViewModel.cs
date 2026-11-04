using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.Expenses;

/// <summary>The signed-in user's expenses, with receipts, and the yearly Excel export for the tax return.</summary>
internal sealed partial class ExpensesViewModel(
    ICurrentUser currentUser,
    ListExpenses.Handler list,
    CreateExpense.Handler create,
    UpdateExpense.Handler update,
    DeleteExpense.Handler delete,
    GetExpenseReceipt.Handler getReceipt,
    ExportExpenses.Handler export,
    IFilePickService filePicker,
    IFileSaveService fileSaver,
    IClientLookup clientLookup,
    ILocalizationService localization,
    TimeProvider clock) : ViewModelBase
{
    /// <summary>Rows per page; keeps the table a fixed height however many expenses there are.</summary>
    private const int PageSize = 15;

    private IReadOnlyList<ClientReference> _clients = [];

    public ObservableCollection<ExpenseRow> Expenses { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial ExpenseRow? SelectedExpense { get; set; }

    /// <summary>The calendar year to export; a decimal because that is what NumericUpDown binds to.</summary>
    [ObservableProperty]
    public partial decimal? ExportYear { get; set; } = clock.GetLocalNow().Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    public partial ExpenseEditorViewModel? Editor { get; set; }

    public bool ShowEmptyState => Editor is null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageLabel))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageLabel))]
    [NotifyPropertyChangedFor(nameof(ShowPager))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial int PageCount { get; set; } = 1;

    public string PageLabel => localization.Format("TimeReports_PageOf", CurrentPage, PageCount);

    public bool ShowPager => PageCount > 1;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedExpenseChanged(ExpenseRow? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(NewEditor(value.Expense));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (currentUser.User is not { } user)
        {
            StatusMessage = localization.Get("Expenses_NotSignedIn");
            return;
        }

        _clients = await clientLookup.ListAsync(cancellationToken);

        var response = await list.HandleAsync(new ListExpenses.Query(user.Id, CurrentPage, PageSize), cancellationToken);

        // Deleting the last expense on the last page leaves the current page empty: step back to the new last page.
        var pageCount = Math.Max(1, (response.TotalCount + PageSize - 1) / PageSize);
        if (CurrentPage > pageCount)
        {
            CurrentPage = pageCount;
            response = await list.HandleAsync(new ListExpenses.Query(user.Id, CurrentPage, PageSize), cancellationToken);
        }

        PageCount = pageCount;

        Expenses.Clear();
        foreach (var expense in response.Items)
        {
            Expenses.Add(new ExpenseRow(expense, localization.Get($"ExpenseCategory_{expense.Category}")));
        }

        StatusMessage = localization.Format("Expenses_Summary", response.TotalCount, response.TotalAmount.ToString("N2", CultureInfo.CurrentCulture));
    }

    [RelayCommand]
    private void New()
    {
        if (currentUser.User is null)
        {
            StatusMessage = localization.Get("Expenses_NotSignedIn");
            return;
        }

        SelectedExpense = null;
        OpenEditor(NewEditor(null));
    }

    [RelayCommand(CanExecute = nameof(HasPreviousPage))]
    private async Task PreviousPageAsync(CancellationToken cancellationToken) => await GoToPageAsync(CurrentPage - 1, cancellationToken);

    [RelayCommand(CanExecute = nameof(HasNextPage))]
    private async Task NextPageAsync(CancellationToken cancellationToken) => await GoToPageAsync(CurrentPage + 1, cancellationToken);

    private bool HasPreviousPage() => CurrentPage > 1;

    private bool HasNextPage() => CurrentPage < PageCount;

    private async Task GoToPageAsync(int page, CancellationToken cancellationToken)
    {
        CloseEditor();
        CurrentPage = page;
        await LoadAsync(cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedExpense is not { } row || currentUser.User is not { } user)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteExpense.Command(row.Expense.Id, user.Id), cancellationToken);

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
            StatusMessage = localization.Get("Expenses_NotSignedIn");
            return;
        }

        if (ExportYear is not { } year)
        {
            StatusMessage = localization.Describe(Error.Validation("time_report_year_invalid", "The year is not valid."));
            return;
        }

        var result = await export.HandleAsync(new ExportExpenses.Query(user.Id, (int)year), cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
            return;
        }

        var saved = await fileSaver.SaveAsync(localization.Get("Expenses_ExportDialogTitle"), result.Value.FileName, result.Value.Content, cancellationToken);
        if (saved is not null)
        {
            StatusMessage = localization.Format("Expenses_Exported", result.Value.ExpenseCount, saved);
        }
    }

    private bool HasSelection() => SelectedExpense is not null;

    private ExpenseEditorViewModel NewEditor(ExpenseDto? existing)
    {
        var categories = Enum.GetValues<ExpenseCategory>()
            .Select(c => new ExpenseCategoryOption(c, localization.Get($"ExpenseCategory_{c}")))
            .ToList();

        return new ExpenseEditorViewModel(
            existing,
            currentUser.User?.Id ?? Guid.Empty,
            _clients,
            categories,
            DateOnly.FromDateTime(clock.GetLocalNow().Date),
            create,
            update,
            getReceipt,
            filePicker,
            fileSaver,
            localization);
    }

    private void OpenEditor(ExpenseEditorViewModel editor)
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
        SelectedExpense = null;
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
    private async void OnEditorSaved(object? sender, ExpenseDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("Expenses_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
