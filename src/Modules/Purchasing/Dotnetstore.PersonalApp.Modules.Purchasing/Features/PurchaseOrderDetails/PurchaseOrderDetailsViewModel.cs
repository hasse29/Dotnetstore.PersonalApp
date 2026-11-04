using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrderDetails;

internal sealed partial class PurchaseOrderDetailsViewModel(
    ListPurchaseOrderDetails.Handler list,
    CreatePurchaseOrderDetail.Handler create,
    UpdatePurchaseOrderDetail.Handler update,
    DeletePurchaseOrderDetail.Handler delete,
    ListPurchaseOrderOptions.Handler listPurchaseOrderOptions,
    ExternalReferences references,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _purchaseOrderOptions = [];
    private IReadOnlyList<OptionDto> _productOptions = [];

    private Guid? _parentId;

    /// <summary>True when the page sits in a detail page of its parent, so it drops its own title and margin.</summary>
    public bool IsEmbedded => _parentId is not null;

    /// <summary>Raised after a row was saved or deleted, so the page around it can refresh what it shows.</summary>
    public event EventHandler? Changed;

    /// <summary>Limits the list to one parent and gives new rows that parent; call before the first load.</summary>
    public PurchaseOrderDetailsViewModel ForParent(Guid parentId)
    {
        _parentId = parentId;
        return this;
    }

    public ObservableCollection<PurchaseOrderDetailDto> PurchaseOrderDetails { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial PurchaseOrderDetailDto? SelectedPurchaseOrderDetail { get; set; }

    [ObservableProperty]
    public partial PurchaseOrderDetailEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedPurchaseOrderDetailChanged(PurchaseOrderDetailDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new PurchaseOrderDetailEditorViewModel(value, create, update, _purchaseOrderOptions, _productOptions, clock, localization, _parentId));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _purchaseOrderOptions = await listPurchaseOrderOptions.HandleAsync(cancellationToken);
        _productOptions = await references.ProductOptionsAsync(cancellationToken);

        var response = await list.HandleAsync(new ListPurchaseOrderDetails.Query(Search, ParentId: _parentId), cancellationToken);

        PurchaseOrderDetails.Clear();
        foreach (var item in response.Items)
        {
            PurchaseOrderDetails.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("PurchaseOrderDetails_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("PurchaseOrderDetails_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedPurchaseOrderDetail = null;
        OpenEditor(new PurchaseOrderDetailEditorViewModel(null, create, update, _purchaseOrderOptions, _productOptions, clock, localization, _parentId));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedPurchaseOrderDetail is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeletePurchaseOrderDetail.Command(selected.Id), cancellationToken);

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

    private bool HasSelection() => SelectedPurchaseOrderDetail is not null;

    private void OpenEditor(PurchaseOrderDetailEditorViewModel editor)
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
        SelectedPurchaseOrderDetail = null;
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
    private async void OnEditorSaved(object? sender, PurchaseOrderDetailDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("PurchaseOrderDetails_Saved");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}