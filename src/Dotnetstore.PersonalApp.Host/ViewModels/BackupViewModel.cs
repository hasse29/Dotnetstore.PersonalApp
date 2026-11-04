using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Host.Backup;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Host.ViewModels;

/// <summary>Settings page for backing up all databases to a zip and restoring them from one.</summary>
internal sealed partial class BackupViewModel(
    DatabaseBackup backup,
    IFileSaveService fileSaver,
    IFilePickService filePicker,
    IAppRestarter restarter,
    ILocalizationService localization) : ViewModelBase
{
    // The zip the user chose, held until they confirm that it replaces the current data.
    private byte[]? _pendingArchive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    [NotifyCanExecuteChangedFor(nameof(BackupCommand))]
    [NotifyCanExecuteChangedFor(nameof(ChooseRestoreCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool StatusIsError { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    [NotifyCanExecuteChangedFor(nameof(BackupCommand))]
    [NotifyCanExecuteChangedFor(nameof(ChooseRestoreCommand))]
    public partial bool IsConfirmingRestore { get; set; }

    [ObservableProperty]
    public partial string? PendingFileName { get; set; }

    /// <summary>True after a restore: the running app still shows the old data, so it has to be restarted.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    [NotifyCanExecuteChangedFor(nameof(BackupCommand))]
    [NotifyCanExecuteChangedFor(nameof(ChooseRestoreCommand))]
    public partial bool NeedsRestart { get; set; }

    public bool CanStart => !IsBusy && !IsConfirmingRestore && !NeedsRestart;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task BackupAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var result = await backup.CreateAsync(cancellationToken);
            if (!result.IsSuccess)
            {
                ShowError(result.Error);
                return;
            }

            var saved = await fileSaver.SaveAsync(localization.Get("Backup_SaveDialogTitle"), result.Value.FileName, result.Value.Content, cancellationToken);
            if (saved is not null)
            {
                ShowSuccess(localization.Format("Backup_Created", result.Value.DatabaseCount, saved));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task ChooseRestoreAsync(CancellationToken cancellationToken)
    {
        var picked = await filePicker.PickFilesAsync(localization.Get("Backup_PickTitle"), DatabaseBackup.MaxBackupBytes, cancellationToken);
        if (picked.Skipped.Count > 0)
        {
            ShowError(Error.Validation("backup_file_too_large", "The backup is too large."));
            return;
        }

        if (picked.Files.Count == 0)
        {
            return;
        }

        _pendingArchive = picked.Files[0].Content.ToArray();
        PendingFileName = picked.Files[0].Name;
        StatusMessage = null;
        IsConfirmingRestore = true;
    }

    [RelayCommand]
    private void CancelRestore()
    {
        _pendingArchive = null;
        PendingFileName = null;
        IsConfirmingRestore = false;
    }

    [RelayCommand]
    private async Task ConfirmRestoreAsync(CancellationToken cancellationToken)
    {
        if (_pendingArchive is not { } archive)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await backup.RestoreAsync(archive, cancellationToken);
            CancelRestore();
            if (!result.IsSuccess)
            {
                ShowError(result.Error);
                return;
            }

            NeedsRestart = true;
            ShowSuccess(result.Value.SafetyCopyPath is { } safety
                ? localization.Format("Backup_RestoredWithCopy", result.Value.DatabaseCount, safety)
                : localization.Format("Backup_Restored", result.Value.DatabaseCount));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Restart() => restarter.Restart();

    private void ShowError(Error error)
    {
        StatusIsError = true;
        StatusMessage = localization.Describe(error);
    }

    private void ShowSuccess(string message)
    {
        StatusIsError = false;
        StatusMessage = message;
    }
}
