using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOffers;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderDetails;

internal sealed partial class SalesOrderDetailsViewModel(
    ListSalesOrderDetails.Handler list,
    CreateSalesOrderDetail.Handler create,
    UpdateSalesOrderDetail.Handler update,
    DeleteSalesOrderDetail.Handler delete,
    ListSalesOrderOptions.Handler listSalesOrderOptions,
    ListSpecialOfferOptions.Handler listSpecialOfferOptions,
    ExternalReferences references,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _salesOrderOptions = [];
    private IReadOnlyList<OptionDto> _specialOfferOptions = [];
    private IReadOnlyList<OptionDto> _productOptions = [];

    private Guid? _parentId;

    /// <summary>True when the page sits in a detail page of its parent, so it drops its own title and margin.</summary>
    public bool IsEmbedded => _parentId is not null;

    /// <summary>Raised after a row was saved or deleted, so the page around it can refresh what it shows.</summary>
    public event EventHandler? Changed;

    /// <summary>Limits the list to one parent and gives new rows that parent; call before the first load.</summary>
    public SalesOrderDetailsViewModel ForParent(Guid parentId)
    {
        _parentId = parentId;
        return this;
    }

    public ObservableCollection<SalesOrderDetailDto> SalesOrderDetails { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial SalesOrderDetailDto? SelectedSalesOrderDetail { get; set; }

    [ObservableProperty]
    public partial SalesOrderDetailEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedSalesOrderDetailChanged(SalesOrderDetailDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new SalesOrderDetailEditorViewModel(value, create, update, _salesOrderOptions, _specialOfferOptions, _productOptions, clock, localization, _parentId));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _salesOrderOptions = await listSalesOrderOptions.HandleAsync(cancellationToken);
        _specialOfferOptions = await listSpecialOfferOptions.HandleAsync(cancellationToken);
        _productOptions = await references.ProductOptionsAsync(cancellationToken);

        var response = await list.HandleAsync(new ListSalesOrderDetails.Query(Search, ParentId: _parentId), cancellationToken);

        SalesOrderDetails.Clear();
        foreach (var item in response.Items)
        {
            SalesOrderDetails.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("SalesOrderDetails_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("SalesOrderDetails_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedSalesOrderDetail = null;
        OpenEditor(new SalesOrderDetailEditorViewModel(null, create, update, _salesOrderOptions, _specialOfferOptions, _productOptions, clock, localization, _parentId));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedSalesOrderDetail is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteSalesOrderDetail.Command(selected.Id), cancellationToken);

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

    private bool HasSelection() => SelectedSalesOrderDetail is not null;

    private void OpenEditor(SalesOrderDetailEditorViewModel editor)
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
        SelectedSalesOrderDetail = null;
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
    private async void OnEditorSaved(object? sender, SalesOrderDetailDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("SalesOrderDetails_Saved");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}