using Dotnetstore.PersonalApp.Modules.Products.Domain;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;

internal sealed record StorageLocationDto(Guid Id, string Name, string? Description, bool IsActive)
{
    public static StorageLocationDto From(StorageLocation location) => new(location.Id, location.Name, location.Description, location.IsActive);
}
