namespace Dotnetstore.PersonalApp.Shared.Lookups;

/// <summary>A row of another module as a drop-down entry: just enough to pick it and show its name.</summary>
public sealed record ReferenceItem(Guid Id, string Name);

/// <summary>Implemented by a module that owns a list other modules point at (products, ship methods, ...), so they can refer to it without referencing the module.</summary>
public interface IReferenceLookup
{
    /// <summary>Every row ordered by name.</summary>
    Task<IReadOnlyList<ReferenceItem>> ListAsync(CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}
