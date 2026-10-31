using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumberTypes;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumbers;

/// <summary>The addresses of one object (e.g. a user), shown as a tab on that object's detail page.</summary>
internal sealed partial class ObjectPhoneNumbersViewModel(
    Guid objectId,
    ListPhoneNumbers.Handler list,
    ListPhoneNumberTypes.Handler listTypes,
    CreatePhoneNumber.Handler create,
    UpdatePhoneNumber.Handler update,
    DeletePhoneNumber.Handler delete,
    DeletePhoneNumbersForObject.Handler deleteAll,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<PhoneNumberTypeDto> _types = [];

    public ObservableCollection<PhoneNumberDto> PhoneNumbers { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial PhoneNumberDto? SelectedPhoneNumber { get; set; }

    [ObservableProperty]
    public partial PhoneNumberEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfirmingDelete), nameof(IsConfirmingDeleteAll), nameof(IsIdle))]
    public partial DeleteMode Mode { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteAllCommand))]
    public partial bool HasPhoneNumbers { get; set; }

    public bool IsConfirmingDelete => Mode == DeleteMode.One;

    public bool IsConfirmingDeleteAll => Mode == DeleteMode.All;

    public bool IsIdle => Mode == DeleteMode.None;

    internal enum DeleteMode
    {
        None,
        One,
        All,
    }

    partial void OnSelectedPhoneNumberChanged(PhoneNumberDto? value)
    {
        Mode = DeleteMode.None;
        if (value is not null)
        {
            OpenEditor(new PhoneNumberEditorViewModel(value, objectId, _types, create, update, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _types = (await listTypes.HandleAsync(new ListPhoneNumberTypes.Query(), cancellationToken)).Items;
        var response = await list.HandleAsync(new ListPhoneNumbers.Query(objectId), cancellationToken);

        PhoneNumbers.Clear();
        foreach (var address in response.Items)
        {
            PhoneNumbers.Add(address);
        }

        HasPhoneNumbers = PhoneNumbers.Count > 0;
        StatusMessage = localization.Format("PhoneNumbersPanel_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        if (_types.Count == 0)
        {
            StatusMessage = localization.Get("PhoneNumbersPanel_NoTypes");
            return;
        }

        SelectedPhoneNumber = null;
        OpenEditor(new PhoneNumberEditorViewModel(null, objectId, _types, create, update, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => Mode = DeleteMode.One;

    [RelayCommand(CanExecute = nameof(HasPhoneNumbers))]
    private void RequestDeleteAll() => Mode = DeleteMode.All;

    [RelayCommand]
    private void CancelDelete() => Mode = DeleteMode.None;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedPhoneNumber is not { } address)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeletePhoneNumber.Command(address.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private async Task ConfirmDeleteAllAsync(CancellationToken cancellationToken)
    {
        var result = await deleteAll.HandleAsync(new DeletePhoneNumbersForObject.Command(objectId), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        StatusMessage = result.IsSuccess
            ? localization.Format("PhoneNumbersPanel_Deleted", result.Value)
            : localization.Describe(result.Error);
    }

    private bool HasSelection() => SelectedPhoneNumber is not null;

    private void OpenEditor(PhoneNumberEditorViewModel editor)
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
        SelectedPhoneNumber = null;
        Mode = DeleteMode.None;
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
    private async void OnEditorSaved(object? sender, PhoneNumberDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Format("PhoneNumbersPanel_Saved", saved.PhoneNumberTypeName);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
