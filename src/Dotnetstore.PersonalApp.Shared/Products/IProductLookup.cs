using Dotnetstore.PersonalApp.Shared.Lookups;

namespace Dotnetstore.PersonalApp.Shared.Products;

/// <summary>Implemented by the Production module so that Purchasing and Sales can refer to products.</summary>
public interface IProductLookup : IReferenceLookup;

/// <summary>Implemented by the Production module so that other modules can refer to units of measure.</summary>
public interface IUnitMeasureLookup : IReferenceLookup;
