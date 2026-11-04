using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockLevels;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockMovements;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;

/// <summary>A storage location with what is stored in it and what has moved in and out.</summary>
internal sealed class StorageLocationDetailViewModel(
    StorageLocationDto location,
    StockLevelsViewModel contents,
    StockMovementsViewModel movements) : DetailViewModel
{
    public StorageLocationDto Location { get; } = location;

    public StockLevelsViewModel Contents { get; } = contents.ForParent(location.Id);

    public StockMovementsViewModel Movements { get; } = movements.ForParent(location.Id);
}
