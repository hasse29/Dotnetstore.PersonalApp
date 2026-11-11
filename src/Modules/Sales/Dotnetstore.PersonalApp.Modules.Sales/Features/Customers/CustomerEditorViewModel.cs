using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal sealed partial class CustomerEditorViewModel : ViewModelBase
{
    private readonly CustomerDto? _existing;
    private readonly CreateCustomer.Handler _create;
    private readonly UpdateCustomer.Handler _update;
    private readonly ILocalizationService _localization;

    public CustomerEditorViewModel(
        CustomerDto? existing,
        CreateCustomer.Handler create,
        UpdateCustomer.Handler update,
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
            Street = existing.Street ?? string.Empty;
            PostalCode = existing.PostalCode ?? string.Empty;
            City = existing.City ?? string.Empty;
            Notes = existing.Notes ?? string.Empty;
            IsActive = existing.IsActive;
        }
    }

    public event EventHandler<CustomerDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "CustomerEditor_New" : "CustomerEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OrganizationNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Phone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Street { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PostalCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string City { get; set; } = string.Empty;

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

        var fields = new CustomerFields(Name, OrganizationNumber, Email, Phone, Street, PostalCode, City, Notes, IsActive);
        var result = _existing is null
            ? await _create.HandleAsync(new CreateCustomer.Command(fields), cancellationToken)
            : await _update.HandleAsync(new UpdateCustomer.Command(_existing.Id, fields), cancellationToken);

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
