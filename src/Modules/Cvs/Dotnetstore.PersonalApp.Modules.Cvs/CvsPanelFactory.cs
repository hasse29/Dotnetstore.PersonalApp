using Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Cvs;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Cvs;

internal sealed class CvsPanelFactory(
    ICurrentUser currentUser,
    ListCvs.Handler list,
    CreateCv.Handler create,
    UpdateCv.Handler update,
    DeleteCv.Handler delete,
    CvDetailServices detailServices,
    ILocalizationService localization) : IUserCvsPanelFactory
{
    /// <summary>The signed-in user's CVs, used by the module's own page. With nobody signed in the list is simply empty.</summary>
    public CvsViewModel CreateForCurrentUser() => Build(currentUser.User?.Id ?? Guid.Empty, isPageLayout: true);

    public ViewModelBase Create(Guid userId) => Build(userId, isPageLayout: false);

    private CvsViewModel Build(Guid userId, bool isPageLayout) =>
        new(userId, isPageLayout, list, create, update, delete, detailServices, localization);
}
