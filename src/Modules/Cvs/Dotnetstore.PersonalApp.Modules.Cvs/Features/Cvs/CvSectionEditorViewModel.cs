using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>
/// The shared save / cancel / delete behaviour of the editors on the detail page. An editor never touches a handler: it is
/// given a delegate that performs the save (and, for an existing entry, the delete) and reports a failure as an <see cref="Error"/>.
/// </summary>
internal abstract partial class CvSectionEditorViewModel(ILocalizationService localization) : ViewModelBase
{
    protected ILocalizationService Localization => localization;

    public event EventHandler? Saved;

    public event EventHandler? Deleted;

    public event EventHandler? Cancelled;

    public abstract string Title { get; }

    /// <summary>True when an existing entry is edited, so it can be deleted.</summary>
    public virtual bool CanDelete => false;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    /// <returns>The failure, or null when the entry was saved.</returns>
    protected abstract Task<Error?> SaveCoreAsync(CancellationToken cancellationToken);

    protected virtual Task<Error?> DeleteCoreAsync(CancellationToken cancellationToken) => Task.FromResult<Error?>(null);

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var error = await SaveCoreAsync(cancellationToken);
        if (error is null)
        {
            Saved?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            ErrorMessage = localization.Describe(error);
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        var error = await DeleteCoreAsync(cancellationToken);
        if (error is null)
        {
            Deleted?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            IsConfirmingDelete = false;
            ErrorMessage = localization.Describe(error);
        }
    }
}
