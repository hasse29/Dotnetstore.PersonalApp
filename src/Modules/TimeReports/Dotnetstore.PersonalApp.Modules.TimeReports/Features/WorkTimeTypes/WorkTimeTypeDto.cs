using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.WorkTimeTypes;

internal sealed record WorkTimeTypeDto(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt)
{
    public static WorkTimeTypeDto From(WorkTimeType type) => new(type.Id, type.Name, type.Description, type.CreatedAt);
}
