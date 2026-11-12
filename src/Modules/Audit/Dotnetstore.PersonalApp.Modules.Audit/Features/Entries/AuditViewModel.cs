using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Audit.Features.Entries;

internal sealed partial class AuditViewModel(ListAuditEntries.Handler list, ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<AuditEntryDto> Entries { get; } = [];

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool OnlyChanges { get; set; }

    [ObservableProperty]
    public partial bool OnlyFailed { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListAuditEntries.Query(Search, OnlyChanges ? true : null, OnlyFailed), cancellationToken);

        Entries.Clear();
        foreach (var entry in response.Items)
        {
            Entries.Add(entry);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("Audit_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("Audit_Count", response.TotalCount);
    }
}
