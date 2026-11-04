using Dotnetstore.PersonalApp.Shared.Products;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features;

/// <summary>
/// The data of other modules that this module points at (products, users, ...), read through their lookups.
/// A lookup that is not registered, because its module is absent, reads as empty and accepts every id.
/// </summary>
internal sealed class ExternalReferences(IProductLookup? product = null)
{
    public static string? NameOf(IReadOnlyDictionary<Guid, string> names, Guid? id) =>
        id is { } value ? names.GetValueOrDefault(value, string.Empty) : null;

    public async Task<IReadOnlyList<OptionDto>> ProductOptionsAsync(CancellationToken cancellationToken) =>
        product is null ? [] : [.. (await product.ListAsync(cancellationToken).ConfigureAwait(false)).Select(i => new OptionDto(i.Id, i.Name))];

    public async Task<IReadOnlyDictionary<Guid, string>> ProductNamesAsync(CancellationToken cancellationToken) =>
        (await ProductOptionsAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(o => o.Id, o => o.Name);

    public async Task<bool> ProductExistsAsync(Guid id, CancellationToken cancellationToken) =>
        product is null || await product.ExistsAsync(id, cancellationToken).ConfigureAwait(false);
}