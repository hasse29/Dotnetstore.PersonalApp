using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Host.ViewModels;

/// <summary>The signed-in experience: module navigation on the left, the selected module page on the right.</summary>
internal sealed partial class ShellViewModel : ViewModelBase
{
    private readonly Action _signOut;

    public ShellViewModel(IEnumerable<IModulePage> pages, AuthenticatedUser user, Action signOut)
    {
        _signOut = signOut;
        User = user;
        Pages = [.. pages];
        SelectedPage = Pages.Count > 0 ? Pages[0] : null;
    }

    public AuthenticatedUser User { get; }

    public string Initials => string.Concat(
        User.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0])));

    public IReadOnlyList<IModulePage> Pages { get; }

    [ObservableProperty]
    public partial IModulePage? SelectedPage { get; set; }

    [ObservableProperty]
    public partial ViewModelBase? CurrentPage { get; set; }

    partial void OnSelectedPageChanged(IModulePage? value) => CurrentPage = value?.CreateViewModel();

    [RelayCommand]
    private void SignOut() => _signOut();
}
