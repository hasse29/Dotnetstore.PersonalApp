using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;

internal sealed partial class SupplierEditorViewModel : ViewModelBase
{
    private readonly SupplierDto? _existing;
    private readonly CreateSupplier.Handler _create;
    private readonly UpdateSupplier.Handler _update;
    private readonly ILocalizationService _localization;

    public SupplierEditorViewModel(
        SupplierDto? existing,
        CreateSupplier.Handler create,
        UpdateSupplier.Handler update,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        if (existing is not null)
        {
            Name = existing.Name;
            OrganizationNumber = existing.OrganizationNumber ?? string.Empty;
            Email = existing.Email ?? string.Empty;
            Phone = existing.Phone ?? string.Empty;
            Website = existing.Website ?? string.Empty;
            Notes = existing.Notes ?? string.Empty;
            IsActive = existing.IsActive;
        }
    }

    public event EventHandler<SupplierDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "SupplierEditor_New" : "SupplierEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OrganizationNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Phone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Website { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsActive { get; set; } = true;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var fields = new SupplierFields(Name, OrganizationNumber, Email, Phone, Website, Notes, IsActive);
        var result = _existing is null
            ? await _create.HandleAsync(new CreateSupplier.Command(fields), cancellationToken)
            : await _update.HandleAsync(new UpdateSupplier.Command(_existing.Id, fields), cancellationToken);

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
