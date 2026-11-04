using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrderDetails;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.ShipMethods;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.Vendors;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;

/// <summary>One purchase order on one screen: the header on the left, its totals and lines on the right.</summary>
internal sealed partial class PurchaseOrderScreenViewModel : DetailViewModel
{
    private readonly ListPurchaseOrders.Handler _list;
    private readonly CreatePurchaseOrder.Handler _create;
    private readonly UpdatePurchaseOrder.Handler _update;
    private readonly ListVendorOptions.Handler _listVendorOptions;
    private readonly ListShipMethodOptions.Handler _listShipMethodOptions;
    private readonly ExternalReferences _references;
    private readonly TimeProvider _clock;
    private readonly ILocalizationService _localization;

    private IReadOnlyList<OptionDto> _vendorOptions = [];
    private IReadOnlyList<OptionDto> _shipMethodOptions = [];
    private IReadOnlyList<OptionDto> _userOptions = [];

    public PurchaseOrderScreenViewModel(
        PurchaseOrderDto order,
        ListPurchaseOrders.Handler list,
        CreatePurchaseOrder.Handler create,
        UpdatePurchaseOrder.Handler update,
        ListVendorOptions.Handler listVendorOptions,
        ListShipMethodOptions.Handler listShipMethodOptions,
        ExternalReferences references,
        PurchaseOrderDetailsViewModel lines,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _list = list;
        _create = create;
        _update = update;
        _listVendorOptions = listVendorOptions;
        _listShipMethodOptions = listShipMethodOptions;
        _references = references;
        _clock = clock;
        _localization = localization;

        Order = order;
        Lines = lines.ForParent(order.Id);
        Lines.Changed += OnLinesChanged;
    }

    public PurchaseOrderDetailsViewModel Lines { get; }

    /// <summary>The order as last saved, with the totals the lines add up to.</summary>
    [ObservableProperty]
    public partial PurchaseOrderDto Order { get; set; }

    [ObservableProperty]
    public partial PurchaseOrderEditorViewModel? Editor { get; set; }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _vendorOptions = await _listVendorOptions.HandleAsync(cancellationToken);
        _shipMethodOptions = await _listShipMethodOptions.HandleAsync(cancellationToken);
        _userOptions = await _references.UserOptionsAsync(cancellationToken);

        OpenEditor(Order);
        Lines.LoadCommand.Execute(null);
    }

    private void OpenEditor(PurchaseOrderDto order)
    {
        if (Editor is { } old)
        {
            old.Saved -= OnEditorSaved;
            old.Cancelled -= OnEditorCancelled;
        }

        var editor = new PurchaseOrderEditorViewModel(order, _create, _update, _vendorOptions, _shipMethodOptions, _userOptions, _clock, _localization);
        editor.Saved += OnEditorSaved;
        editor.Cancelled += OnEditorCancelled;
        Editor = editor;
    }

    private void OnEditorSaved(object? sender, PurchaseOrderDto saved)
    {
        Order = saved;
        OpenEditor(saved);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => OpenEditor(Order);

    // Event handler, so it has to be async void; a failed refresh leaves the totals as they were.
    private async void OnLinesChanged(object? sender, EventArgs e)
    {
        var response = await _list.HandleAsync(new ListPurchaseOrders.Query(Order.Number, ParentId: Order.VendorId), CancellationToken.None);
        if (response.Items.FirstOrDefault(o => o.Id == Order.Id) is { } fresh)
        {
            Order = fresh;
        }
    }
}
