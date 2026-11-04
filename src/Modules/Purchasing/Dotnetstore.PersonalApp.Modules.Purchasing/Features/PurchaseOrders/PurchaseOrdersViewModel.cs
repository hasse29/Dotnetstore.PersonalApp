using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.ShipMethods;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.Vendors;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;

internal sealed partial class PurchaseOrdersViewModel(
    ListPurchaseOrders.Handler list,
    CreatePurchaseOrder.Handler create,
    UpdatePurchaseOrder.Handler update,
    DeletePurchaseOrder.Handler delete,
    IDetailFactory<PurchaseOrderDto> detailFactory,
    ListVendorOptions.Handler listVendorOptions,
    ListShipMethodOptions.Handler listShipMethodOptions,
    ExternalReferences references,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _vendorOptions = [];
    private IReadOnlyList<OptionDto> _shipMethodOptions = [];
    private IReadOnlyList<OptionDto> _userOptions = [];

    private Guid? _parentId;

    /// <summary>True when the page sits in a detail page of its parent, so it drops its own title and margin.</summary>
    public bool IsEmbedded => _parentId is not null;

    /// <summary>Raised after a row was saved or deleted, so the page around it can refresh what it shows.</summary>
    public event EventHandler? Changed;

    /// <summary>Limits the list to one parent and gives new rows that parent; call before the first load.</summary>
    public PurchaseOrdersViewModel ForParent(Guid parentId)
    {
        _parentId = parentId;
        return this;
    }

    [ObservableProperty]
    public partial DetailViewModel? Detail { get; set; }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenDetail()
    {
        if (SelectedPurchaseOrder is not { } selected)
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
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public ObservableCollection<PurchaseOrderDto> PurchaseOrders { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenDetailCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial PurchaseOrderDto? SelectedPurchaseOrder { get; set; }

    [ObservableProperty]
    public partial PurchaseOrderEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedPurchaseOrderChanged(PurchaseOrderDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new PurchaseOrderEditorViewModel(value, create, update, _vendorOptions, _shipMethodOptions, _userOptions, clock, localization, _parentId));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _vendorOptions = await listVendorOptions.HandleAsync(cancellationToken);
        _shipMethodOptions = await listShipMethodOptions.HandleAsync(cancellationToken);
        _userOptions = await references.UserOptionsAsync(cancellationToken);

        var response = await list.HandleAsync(new ListPurchaseOrders.Query(Search, ParentId: _parentId), cancellationToken);

        PurchaseOrders.Clear();
        foreach (var item in response.Items)
        {
            PurchaseOrders.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("PurchaseOrders_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("PurchaseOrders_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedPurchaseOrder = null;
        OpenEditor(new PurchaseOrderEditorViewModel(null, create, update, _vendorOptions, _shipMethodOptions, _userOptions, clock, localization, _parentId));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedPurchaseOrder is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeletePurchaseOrder.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
        else
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool HasSelection() => SelectedPurchaseOrder is not null;

    private void OpenEditor(PurchaseOrderEditorViewModel editor)
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
        SelectedPurchaseOrder = null;
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
    private async void OnEditorSaved(object? sender, PurchaseOrderDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("PurchaseOrders_Saved");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}