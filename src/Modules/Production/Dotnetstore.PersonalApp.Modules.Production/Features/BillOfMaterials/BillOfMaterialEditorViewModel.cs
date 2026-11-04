using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.BillOfMaterials;

internal sealed partial class BillOfMaterialEditorViewModel : ViewModelBase
{
    private readonly BillOfMaterialDto? _existing;
    private readonly CreateBillOfMaterial.Handler _create;
    private readonly UpdateBillOfMaterial.Handler _update;
    private readonly ILocalizationService _localization;

    public BillOfMaterialEditorViewModel(
        BillOfMaterialDto? existing,
        CreateBillOfMaterial.Handler create,
        UpdateBillOfMaterial.Handler update,
        IReadOnlyList<OptionDto> productOptions,
        IReadOnlyList<OptionDto> unitMeasureOptions,
        TimeProvider clock,
        ILocalizationService localization,
        Guid? fixedParentId = null)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        ProductAssemblyOptions = [OptionDto.None(localization.Get("Production_None")), .. productOptions];
        ComponentOptions = productOptions;
        UnitOptions = unitMeasureOptions;

        if (existing is not null)
        {
            SelectedProductAssembly = ProductAssemblyOptions.FirstOrDefault(o => o.Id == existing.ProductAssemblyId) ?? ProductAssemblyOptions[0];
            SelectedComponent = ComponentOptions.FirstOrDefault(o => o.Id == existing.ComponentId);
            StartDate = existing.StartDate.ToDateTime(TimeOnly.MinValue);
            EndDate = existing.EndDate?.ToDateTime(TimeOnly.MinValue);
            SelectedUnit = UnitOptions.FirstOrDefault(o => o.Id == existing.UnitId);
            BomLevel = existing.BomLevel;
            PerAssemblyQty = existing.PerAssemblyQty;
        }
        else
        {
            SelectedProductAssembly = ProductAssemblyOptions[0];
            StartDate = clock.GetLocalNow().Date;
        }

        if (fixedParentId is { } parentId)
        {
            SelectedProductAssembly = ProductAssemblyOptions.FirstOrDefault(o => o.Id == parentId) ?? SelectedProductAssembly;
            IsParentFixed = true;
        }
    }

    public event EventHandler<BillOfMaterialDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "BillOfMaterialEditor_New" : "BillOfMaterialEditor_Edit");

    public bool IsParentFixed { get; }

    public IReadOnlyList<OptionDto> ProductAssemblyOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedProductAssembly { get; set; }

    public IReadOnlyList<OptionDto> ComponentOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedComponent { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? StartDate { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? EndDate { get; set; }

    public IReadOnlyList<OptionDto> UnitOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedUnit { get; set; }

    [ObservableProperty]
    public partial decimal? BomLevel { get; set; }

    [ObservableProperty]
    public partial decimal? PerAssemblyQty { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateBillOfMaterial.Command(ToId(SelectedProductAssembly), SelectedComponent?.Id ?? Guid.Empty, DateOnly.FromDateTime(StartDate ?? DateTime.MinValue), ToDate(EndDate), SelectedUnit?.Id ?? Guid.Empty, (int)(BomLevel ?? 0), PerAssemblyQty ?? 0m), cancellationToken)
            : await _update.HandleAsync(new UpdateBillOfMaterial.Command(existing.Id, ToId(SelectedProductAssembly), SelectedComponent?.Id ?? Guid.Empty, DateOnly.FromDateTime(StartDate ?? DateTime.MinValue), ToDate(EndDate), SelectedUnit?.Id ?? Guid.Empty, (int)(BomLevel ?? 0), PerAssemblyQty ?? 0m), cancellationToken);

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

    private static DateOnly? ToDate(DateTime? value) => value is { } v ? DateOnly.FromDateTime(v) : null;

    private static Guid? ToId(OptionDto? option) => option is { } o && o.Id != Guid.Empty ? o.Id : null;
}