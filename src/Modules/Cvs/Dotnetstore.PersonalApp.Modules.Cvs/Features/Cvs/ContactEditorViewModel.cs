using CommunityToolkit.Mvvm.ComponentModel;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

internal sealed partial class ContactOption : ObservableObject
{
    public ContactOption(Guid id, string value, string caption, bool isSelected)
    {
        Id = id;
        Value = value;
        Caption = caption;
        IsSelected = isSelected;
    }

    public Guid Id { get; }

    public string Value { get; }

    /// <summary>"Email · Work": what kind of entry it is and which type it has.</summary>
    public string Caption { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>Ticks which of the user's addresses, phone numbers and email addresses the CV shows.</summary>
internal sealed class ContactEditorViewModel : CvSectionEditorViewModel
{
    private readonly Func<IReadOnlyList<Guid>, CancellationToken, Task<Error?>> _save;

    public ContactEditorViewModel(
        IReadOnlyList<ContactEntry> available,
        IReadOnlyList<Guid> selected,
        Func<IReadOnlyList<Guid>, CancellationToken, Task<Error?>> save,
        ILocalizationService localization)
        : base(localization)
    {
        _save = save;
        Options = [.. available.Select(e => new ContactOption(e.Id, e.Value, $"{KindText(localization, e.Kind)} · {e.TypeName}", selected.Contains(e.Id)))];
    }

    public override string Title => Localization.Get("CvContactEditor_Title");

    public IReadOnlyList<ContactOption> Options { get; }

    public bool HasNoOptions => Options.Count == 0;

    public static string KindText(ILocalizationService localization, ContactKind kind) => localization.Get($"CvContactKind_{kind}");

    protected override Task<Error?> SaveCoreAsync(CancellationToken cancellationToken) =>
        _save([.. Options.Where(o => o.IsSelected).Select(o => o.Id)], cancellationToken);
}
