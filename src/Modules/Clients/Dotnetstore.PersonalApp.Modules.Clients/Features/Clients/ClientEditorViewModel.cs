using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;

internal sealed partial class ClientEditorViewModel : ViewModelBase
{
    private readonly ClientDto? _existing;
    private readonly CreateClient.Handler _create;
    private readonly UpdateClient.Handler _update;
    private readonly ILocalizationService _localization;

    public ClientEditorViewModel(ClientDto? existing, CreateClient.Handler create, UpdateClient.Handler update, ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        if (existing is not null)
        {
            Name = existing.Name;
            OrganizationNumber = existing.OrganizationNumber;
            VatNumber = existing.VatNumber ?? string.Empty;
            Email = existing.Email ?? string.Empty;
            Phone = existing.Phone ?? string.Empty;
            Website = existing.Website ?? string.Empty;
            Notes = existing.Notes ?? string.Empty;
        }
    }

    public event EventHandler<ClientDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "ClientEditor_New" : "ClientEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OrganizationNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string VatNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Phone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Website { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is null
            ? await _create.HandleAsync(new CreateClient.Command(Name, OrganizationNumber, VatNumber, Email, Phone, Website, Notes), cancellationToken)
            : await _update.HandleAsync(new UpdateClient.Command(_existing.Id, Name, OrganizationNumber, VatNumber, Email, Phone, Website, Notes), cancellationToken);

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
