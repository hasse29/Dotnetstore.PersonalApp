using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOffers;

internal sealed partial class SpecialOfferEditorViewModel : ViewModelBase
{
    private readonly SpecialOfferDto? _existing;
    private readonly CreateSpecialOffer.Handler _create;
    private readonly UpdateSpecialOffer.Handler _update;
    private readonly ILocalizationService _localization;

    public SpecialOfferEditorViewModel(
        SpecialOfferDto? existing,
        CreateSpecialOffer.Handler create,
        UpdateSpecialOffer.Handler update,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;



        if (existing is not null)
        {
            Description = existing.Description;
            DiscountPct = existing.DiscountPct;
            OfferType = existing.OfferType;
            Category = existing.Category;
            StartDate = existing.StartDate.ToDateTime(TimeOnly.MinValue);
            EndDate = existing.EndDate.ToDateTime(TimeOnly.MinValue);
            MinQty = existing.MinQty;
            MaxQty = existing.MaxQty;
        }
        else
        {
            StartDate = clock.GetLocalNow().Date;
            EndDate = clock.GetLocalNow().Date;
        }
    }

    public event EventHandler<SpecialOfferDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "SpecialOfferEditor_New" : "SpecialOfferEditor_Edit");

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal? DiscountPct { get; set; }

    [ObservableProperty]
    public partial string OfferType { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Category { get; set; } = string.Empty;

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? StartDate { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? EndDate { get; set; }

    [ObservableProperty]
    public partial decimal? MinQty { get; set; }

    [ObservableProperty]
    public partial decimal? MaxQty { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateSpecialOffer.Command(Description, DiscountPct ?? 0m, OfferType, Category, DateOnly.FromDateTime(StartDate ?? DateTime.MinValue), DateOnly.FromDateTime(EndDate ?? DateTime.MinValue), (int)(MinQty ?? 0), (int?)MaxQty), cancellationToken)
            : await _update.HandleAsync(new UpdateSpecialOffer.Command(existing.Id, Description, DiscountPct ?? 0m, OfferType, Category, DateOnly.FromDateTime(StartDate ?? DateTime.MinValue), DateOnly.FromDateTime(EndDate ?? DateTime.MinValue), (int)(MinQty ?? 0), (int?)MaxQty), cancellationToken);

        if (result.IsSuccess)
        {
            Saved?.Invoke(this, result.Value);
        }
        else
        {
            ErrorMessage = _localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);

}