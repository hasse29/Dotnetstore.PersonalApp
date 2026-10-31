using CommunityToolkit.Mvvm.ComponentModel;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

internal sealed partial class LicenceOption : ObservableObject
{
    public LicenceOption(string code, bool isSelected)
    {
        Code = code;
        IsSelected = isSelected;
    }

    public string Code { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>Ticks the licence categories the user holds; saving with none ticked clears the licence.</summary>
internal sealed class DriverLicenceEditorViewModel : CvSectionEditorViewModel
{
    private readonly Func<IReadOnlyList<string>, CancellationToken, Task<Error?>> _save;

    public DriverLicenceEditorViewModel(
        IReadOnlyList<string> current,
        Func<IReadOnlyList<string>, CancellationToken, Task<Error?>> save,
        ILocalizationService localization)
        : base(localization)
    {
        _save = save;
        Options = [.. DriverLicenceCategories.All.Select(code => new LicenceOption(code, current.Contains(code, StringComparer.OrdinalIgnoreCase)))];
    }

    public override string Title => Localization.Get("CvLicenceEditor_Title");

    public IReadOnlyList<LicenceOption> Options { get; }

    protected override Task<Error?> SaveCoreAsync(CancellationToken cancellationToken) =>
        _save([.. Options.Where(o => o.IsSelected).Select(o => o.Code)], cancellationToken);
}
