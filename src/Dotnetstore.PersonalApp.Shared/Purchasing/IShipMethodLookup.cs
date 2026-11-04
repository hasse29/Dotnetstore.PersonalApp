using Dotnetstore.PersonalApp.Shared.Lookups;

namespace Dotnetstore.PersonalApp.Shared.Purchasing;

/// <summary>Implemented by the Purchasing module; Sales orders are shipped with the same ship methods as purchase orders are.</summary>
public interface IShipMethodLookup : IReferenceLookup;
