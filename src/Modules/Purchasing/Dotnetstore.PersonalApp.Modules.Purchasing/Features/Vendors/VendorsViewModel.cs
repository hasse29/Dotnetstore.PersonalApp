using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.Vendors;

internal sealed partial class VendorsViewModel(
    ListVendors.Handler list,
    CreateVendor.Handler create,
    UpdateVendor.Handler update,
    DeleteVendor.Handler delete,
    IDetailFactory<VendorDto> detailFactory,
    ExternalReferences references,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _clientOptions = [];

    [ObservableProperty]
    public partial DetailViewModel? Detail { get; set; }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenDetail()
    {
        if (SelectedVendor is not { } selected)
        {
            return;
        }

        var detail = detailFactory.Create(selected);
        detail.Closed += OnDetailClosed;
        Detail = detail;
    }

    // Event handler, so it has to be async void; LoadAsync reports its own outcome on the status line.
    private async void OnDetailClosed(object? sender, EventArgs e)
    {
        if (Detail is { } detail)
        {
            detail.Closed -= OnDetailClosed;
        }

        Detail = null;
        CloseEditor();
        await LoadAsync(CancellationToken.None);
    }

    public ObservableCollection<VendorDto> Vendors { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenDetailCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial VendorDto? SelectedVendor { get; set; }

    [ObservableProperty]
    public partial VendorEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedVendorChanged(VendorDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new VendorEditorViewModel(value, create, update, _clientOptions, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _clientOptions = await references.ClientOptionsAsync(cancellationToken);

        var response = await list.HandleAsync(new ListVendors.Query(Search), cancellationToken);

        Vendors.Clear();
        foreach (var item in response.Items)
        {
            Vendors.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("Vendors_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("Vendors_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedVendor = null;
        OpenEditor(new VendorEditorViewModel(null, create, update, _clientOptions, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedVendor is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteVendor.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedVendor is not null;

    private void OpenEditor(VendorEditorViewModel editor)
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
        SelectedVendor = null;
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
    private async void OnEditorSaved(object? sender, VendorDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("Vendors_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}