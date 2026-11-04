using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.Expenses;

internal sealed partial class ExpenseEditorViewModel : ViewModelBase
{
    private readonly ExpenseDto? _existing;
    private readonly Guid _userId;
    private readonly ClientReference _noClient;
    private readonly CreateExpense.Handler _create;
    private readonly UpdateExpense.Handler _update;
    private readonly GetExpenseReceipt.Handler _getReceipt;
    private readonly IFilePickService _filePicker;
    private readonly IFileSaveService _fileSaver;
    private readonly ILocalizationService _localization;

    // A receipt picked in this form and not saved yet, and whether the saved one is to be dropped.
    private ReceiptFile? _newReceipt;
    private bool _removeSavedReceipt;

    public ExpenseEditorViewModel(
        ExpenseDto? existing,
        Guid userId,
        IReadOnlyList<ClientReference> clients,
        IReadOnlyList<ExpenseCategoryOption> categories,
        DateOnly today,
        CreateExpense.Handler create,
        UpdateExpense.Handler update,
        GetExpenseReceipt.Handler getReceipt,
        IFilePickService filePicker,
        IFileSaveService fileSaver,
        ILocalizationService localization)
    {
        _existing = existing;
        _userId = userId;
        _create = create;
        _update = update;
        _getReceipt = getReceipt;
        _filePicker = filePicker;
        _fileSaver = fileSaver;
        _localization = localization;

        // Expenses may belong to no client, which the picker shows as an entry of its own.
        _noClient = new ClientReference(Guid.Empty, localization.Get("Expenses_NoClient"));
        Clients = [_noClient, .. clients];
        Categories = categories;

        SelectedClient = Clients.FirstOrDefault(c => c.Id == existing?.ClientId) ?? _noClient;
        SelectedCategory = categories.FirstOrDefault(c => c.Category == existing?.Category) ?? categories[0];
        Date = (existing?.Date ?? today).ToDateTime(TimeOnly.MinValue);
        Description = existing?.Description ?? string.Empty;
        Amount = existing?.Amount;
        VatAmount = existing?.VatAmount ?? 0m;
        ReceiptName = existing?.ReceiptFileName;
    }

    public event EventHandler<ExpenseDto>? Saved;

    public event EventHandler? Cancelled;

    public IReadOnlyList<ClientReference> Clients { get; }

    public IReadOnlyList<ExpenseCategoryOption> Categories { get; }

    public string Title => _localization.Get(_existing is null ? "ExpenseEditor_New" : "ExpenseEditor_Edit");

    [ObservableProperty]
    public partial ClientReference? SelectedClient { get; set; }

    [ObservableProperty]
    public partial ExpenseCategoryOption? SelectedCategory { get; set; }

    [ObservableProperty]
    public partial DateTime? Date { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal? Amount { get; set; }

    [ObservableProperty]
    public partial decimal? VatAmount { get; set; }

    /// <summary>The file name of the receipt that will be kept, or null when there is none.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReceipt))]
    [NotifyCanExecuteChangedFor(nameof(SaveReceiptCopyCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveReceiptCommand))]
    public partial string? ReceiptName { get; set; }

    public bool HasReceipt => ReceiptName is not null;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? ReceiptStatus { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var missing = FirstMissingField();
        if (missing is not null)
        {
            ErrorMessage = _localization.Describe(missing);
            return;
        }

        var clientId = SelectedClient!.Id == Guid.Empty ? (Guid?)null : SelectedClient.Id;
        var date = DateOnly.FromDateTime(Date!.Value);
        var category = SelectedCategory!.Category;

        var result = _existing is null
            ? await _create.HandleAsync(
                new CreateExpense.Command(_userId, clientId, date, category, Description, Amount!.Value, VatAmount ?? 0m, _newReceipt), cancellationToken)
            : await _update.HandleAsync(
                new UpdateExpense.Command(_existing.Id, _userId, clientId, date, category, Description, Amount!.Value, VatAmount ?? 0m, _newReceipt, _removeSavedReceipt), cancellationToken);

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

    [RelayCommand]
    private async Task AttachReceiptAsync(CancellationToken cancellationToken)
    {
        ReceiptStatus = null;

        var picked = await _filePicker.PickFilesAsync(_localization.Get("ExpenseEditor_AttachTitle"), ExpenseRules.MaxReceiptBytes, cancellationToken);
        if (picked.Skipped.Count > 0)
        {
            ReceiptStatus = _localization.Describe(Error.Validation("expense_receipt_too_large", "The receipt file is too large."));
            return;
        }

        if (picked.Files.Count == 0)
        {
            return;
        }

        var file = picked.Files[0];
        if (!ExpenseRules.HasAllowedType(file.Name))
        {
            ReceiptStatus = _localization.Describe(Error.Validation("expense_receipt_type_invalid", "The receipt must be a PDF, PNG or JPEG file."));
            return;
        }

        _newReceipt = new ReceiptFile(file.Name, file.Content.ToArray());
        ReceiptName = ExpenseRules.ReceiptName(file.Name);
    }

    [RelayCommand(CanExecute = nameof(HasReceipt))]
    private void RemoveReceipt()
    {
        _newReceipt = null;
        _removeSavedReceipt = _existing?.HasReceipt == true;
        ReceiptName = null;
        ReceiptStatus = null;
    }

    /// <summary>Saves a copy of the receipt to a place of the user's choosing, so it can be opened or sent on.</summary>
    [RelayCommand(CanExecute = nameof(HasReceipt))]
    private async Task SaveReceiptCopyAsync(CancellationToken cancellationToken)
    {
        ReceiptStatus = null;

        var receipt = _newReceipt;
        if (receipt is null && _existing is not null)
        {
            var result = await _getReceipt.HandleAsync(new GetExpenseReceipt.Query(_existing.Id, _userId), cancellationToken);
            if (!result.IsSuccess)
            {
                ReceiptStatus = _localization.Describe(result.Error);
                return;
            }

            receipt = result.Value;
        }

        if (receipt is null)
        {
            return;
        }

        var saved = await _fileSaver.SaveAsync(_localization.Get("ExpenseEditor_SaveCopyTitle"), ExpenseRules.ReceiptName(receipt.FileName), receipt.Content, cancellationToken);
        if (saved is not null)
        {
            ReceiptStatus = _localization.Format("ExpenseEditor_ReceiptSaved", saved);
        }
    }

    // The pickers can be empty, which the handlers' value types cannot express, so the form reports those cases itself.
    private Error? FirstMissingField()
    {
        if (SelectedCategory is null)
        {
            return Error.Validation("expense_category_invalid", "Category is not valid.");
        }

        if (Date is null)
        {
            return Error.Validation("expense_date_required", "Date is required.");
        }

        return Amount is null ? Error.Validation("expense_amount_required", "Amount is required.") : null;
    }
}
