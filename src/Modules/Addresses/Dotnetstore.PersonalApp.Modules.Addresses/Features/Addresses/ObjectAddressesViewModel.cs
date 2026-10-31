using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Addresses.Features.AddressTypes;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Features.Addresses;

/// <summary>The addresses of one object (e.g. a user), shown as a tab on that object's detail page.</summary>
internal sealed partial class ObjectAddressesViewModel(
    Guid objectId,
    ListAddresses.Handler list,
    ListAddressTypes.Handler listTypes,
    CreateAddress.Handler create,
    UpdateAddress.Handler update,
    DeleteAddress.Handler delete,
    DeleteAddressesForObject.Handler deleteAll,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<AddressTypeDto> _types = [];

    public ObservableCollection<AddressDto> Addresses { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial AddressDto? SelectedAddress { get; set; }

    [ObservableProperty]
    public partial AddressEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfirmingDelete), nameof(IsConfirmingDeleteAll), nameof(IsIdle))]
    public partial DeleteMode Mode { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteAllCommand))]
    public partial bool HasAddresses { get; set; }

    public bool IsConfirmingDelete => Mode == DeleteMode.One;

    public bool IsConfirmingDeleteAll => Mode == DeleteMode.All;

    public bool IsIdle => Mode == DeleteMode.None;

    internal enum DeleteMode
    {
        None,
        One,
        All,
    }

    partial void OnSelectedAddressChanged(AddressDto? value)
    {
        Mode = DeleteMode.None;
        if (value is not null)
        {
            OpenEditor(new AddressEditorViewModel(value, objectId, _types, create, update, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _types = (await listTypes.HandleAsync(new ListAddressTypes.Query(), cancellationToken)).Items;
        var response = await list.HandleAsync(new ListAddresses.Query(objectId), cancellationToken);

        Addresses.Clear();
        foreach (var address in response.Items)
        {
            Addresses.Add(address);
        }

        HasAddresses = Addresses.Count > 0;
        StatusMessage = localization.Format("AddressesPanel_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        if (_types.Count == 0)
        {
            StatusMessage = localization.Get("AddressesPanel_NoTypes");
            return;
        }

        SelectedAddress = null;
        OpenEditor(new AddressEditorViewModel(null, objectId, _types, create, update, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => Mode = DeleteMode.One;

    [RelayCommand(CanExecute = nameof(HasAddresses))]
    private void RequestDeleteAll() => Mode = DeleteMode.All;

    [RelayCommand]
    private void CancelDelete() => Mode = DeleteMode.None;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedAddress is not { } address)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteAddress.Command(address.Id), cancellationToken);

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
        var result = await deleteAll.HandleAsync(new DeleteAddressesForObject.Command(objectId), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        StatusMessage = result.IsSuccess
            ? localization.Format("AddressesPanel_Deleted", result.Value)
            : localization.Describe(result.Error);
    }

    private bool HasSelection() => SelectedAddress is not null;

    private void OpenEditor(AddressEditorViewModel editor)
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
        SelectedAddress = null;
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
    private async void OnEditorSaved(object? sender, AddressDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Format("AddressesPanel_Saved", saved.AddressTypeName);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
