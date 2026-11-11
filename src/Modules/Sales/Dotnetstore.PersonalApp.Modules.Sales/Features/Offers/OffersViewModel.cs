using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Products;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

/// <summary>An offer as the list shows it: the DTO plus its texts in the current UI language.</summary>
internal sealed record OfferRow(OfferDto Offer, string StatusText, string DateText, string ValidUntilText, string TotalText);

internal sealed record OfferStatusOption(OfferStatus? Status, string Label);

/// <summary>The offers list; opening or starting an offer replaces the list with the offer screen.</summary>
internal sealed partial class OffersViewModel : ViewModelBase
{
    private readonly ListOffers.Handler _list;
    private readonly CreateOffer.Handler _create;
    private readonly UpdateOffer.Handler _update;
    private readonly GetOffer.Handler _get;
    private readonly ChangeOfferStatus.Handler _changeStatus;
    private readonly DeleteOffer.Handler _delete;
    private readonly ListCustomers.Handler _customers;
    private readonly IProductCatalog _catalog;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _clock;
    private readonly bool _ready;

    public OffersViewModel(
        ListOffers.Handler list,
        CreateOffer.Handler create,
        UpdateOffer.Handler update,
        GetOffer.Handler get,
        ChangeOfferStatus.Handler changeStatus,
        DeleteOffer.Handler delete,
        ListCustomers.Handler customers,
        IProductCatalog catalog,
        ILocalizationService localization,
        TimeProvider clock)
    {
        _list = list;
        _create = create;
        _update = update;
        _get = get;
        _changeStatus = changeStatus;
        _delete = delete;
        _customers = customers;
        _catalog = catalog;
        _localization = localization;
        _clock = clock;

        StatusOptions =
        [
            new OfferStatusOption(null, localization.Get("Offers_AllStatuses")),
            .. Enum.GetValues<OfferStatus>().Select(s => new OfferStatusOption(s, localization.Get($"OfferStatus_{s}"))),
        ];
        SelectedStatus = StatusOptions[0];
        _ready = true;
    }

    public IReadOnlyList<OfferStatusOption> StatusOptions { get; }

    public ObservableCollection<OfferRow> Offers { get; } = [];

    public bool HasScreen => Screen is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasScreen))]
    public partial OfferScreenViewModel? Screen { get; set; }

    [ObservableProperty]
    public partial OfferRow? SelectedOffer { get; set; }

    [ObservableProperty]
    public partial OfferStatusOption SelectedStatus { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    partial void OnSelectedOfferChanged(OfferRow? value)
    {
        if (value is not null)
        {
            OpenScreen(value.Offer.Id);
        }
    }

    partial void OnSelectedStatusChanged(OfferStatusOption value)
    {
        if (_ready)
        {
            LoadCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await _list.HandleAsync(new ListOffers.Query(Search, SelectedStatus.Status), cancellationToken);

        Offers.Clear();
        foreach (var offer in response.Items)
        {
            Offers.Add(new OfferRow(
                offer,
                _localization.Get(offer.IsExpired ? "OfferStatus_Expired" : $"OfferStatus_{offer.Status}"),
                offer.Date.ToString("d", CultureInfo.CurrentCulture),
                offer.ValidUntil.ToString("d", CultureInfo.CurrentCulture),
                offer.Total.ToString("N2", CultureInfo.CurrentCulture)));
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? _localization.Format("Offers_ShowingPartial", response.Items.Count, response.TotalCount)
            : _localization.Format("Offers_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New() => OpenScreen(null);

    private void OpenScreen(Guid? offerId)
    {
        var screen = new OfferScreenViewModel(offerId, _create, _update, _get, _changeStatus, _delete, _customers, _catalog, _localization, _clock);
        screen.Closed += OnScreenClosed;
        Screen = screen;
        screen.LoadCommand.Execute(null);
    }

    // Event handler, so it has to be async void; LoadAsync reports its own outcome on the status line.
    private async void OnScreenClosed(object? sender, bool reload)
    {
        if (sender is OfferScreenViewModel screen)
        {
            screen.Closed -= OnScreenClosed;
        }

        Screen = null;
        SelectedOffer = null;
        if (reload)
        {
            await LoadAsync(CancellationToken.None);
        }
    }
}
