using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Products;
using Dotnetstore.PersonalApp.Shared.Purchasing;
using Dotnetstore.PersonalApp.Shared.Users;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features;

/// <summary>
/// The data of other modules that this module points at (products, users, ...), read through their lookups.
/// A lookup that is not registered, because its module is absent, reads as empty and accepts every id.
/// </summary>
internal sealed class ExternalReferences(IUserLookup? user = null, IProductLookup? product = null, IClientLookup? client = null, IShipMethodLookup? shipMethod = null)
{
    public static string? NameOf(IReadOnlyDictionary<Guid, string> names, Guid? id) =>
        id is { } value ? names.GetValueOrDefault(value, string.Empty) : null;

    public async Task<IReadOnlyList<OptionDto>> UserOptionsAsync(CancellationToken cancellationToken) =>
        user is null ? [] : [.. (await user.ListAsync(cancellationToken).ConfigureAwait(false)).Select(i => new OptionDto(i.Id, i.FullName))];

    public async Task<IReadOnlyDictionary<Guid, string>> UserNamesAsync(CancellationToken cancellationToken) =>
        (await UserOptionsAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(o => o.Id, o => o.Name);

    public async Task<bool> UserExistsAsync(Guid id, CancellationToken cancellationToken) =>
        user is null || await user.GetAsync(id, cancellationToken).ConfigureAwait(false) is not null;

    public async Task<IReadOnlyList<OptionDto>> ProductOptionsAsync(CancellationToken cancellationToken) =>
        product is null ? [] : [.. (await product.ListAsync(cancellationToken).ConfigureAwait(false)).Select(i => new OptionDto(i.Id, i.Name))];

    public async Task<IReadOnlyDictionary<Guid, string>> ProductNamesAsync(CancellationToken cancellationToken) =>
        (await ProductOptionsAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(o => o.Id, o => o.Name);

    public async Task<bool> ProductExistsAsync(Guid id, CancellationToken cancellationToken) =>
        product is null || await product.ExistsAsync(id, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<OptionDto>> ClientOptionsAsync(CancellationToken cancellationToken) =>
        client is null ? [] : [.. (await client.ListAsync(cancellationToken).ConfigureAwait(false)).Select(i => new OptionDto(i.Id, i.Name))];

    public async Task<IReadOnlyDictionary<Guid, string>> ClientNamesAsync(CancellationToken cancellationToken) =>
        (await ClientOptionsAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(o => o.Id, o => o.Name);

    public async Task<bool> ClientExistsAsync(Guid id, CancellationToken cancellationToken) =>
        client is null || await client.ExistsAsync(id, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<OptionDto>> ShipMethodOptionsAsync(CancellationToken cancellationToken) =>
        shipMethod is null ? [] : [.. (await shipMethod.ListAsync(cancellationToken).ConfigureAwait(false)).Select(i => new OptionDto(i.Id, i.Name))];

    public async Task<IReadOnlyDictionary<Guid, string>> ShipMethodNamesAsync(CancellationToken cancellationToken) =>
        (await ShipMethodOptionsAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(o => o.Id, o => o.Name);

    public async Task<bool> ShipMethodExistsAsync(Guid id, CancellationToken cancellationToken) =>
        shipMethod is null || await shipMethod.ExistsAsync(id, cancellationToken).ConfigureAwait(false);
}