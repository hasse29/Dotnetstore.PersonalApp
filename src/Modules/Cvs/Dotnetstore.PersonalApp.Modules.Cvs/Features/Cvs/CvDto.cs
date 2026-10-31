using Dotnetstore.PersonalApp.Modules.Cvs.Domain;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

internal sealed record CvDto(Guid Id, Guid UserId, string Title, string? TargetJob, string? Summary, DateTimeOffset UpdatedAt, string? LinkedIn)
{
    public static CvDto From(Cv cv) => new(cv.Id, cv.UserId, cv.Title, cv.TargetJob, cv.Summary, cv.UpdatedAt, cv.LinkedIn);
}
