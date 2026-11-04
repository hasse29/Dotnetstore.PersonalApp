using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Products;
using Dotnetstore.PersonalApp.Shared.Users;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features;

/// <summary>
/// The data of other modules that this module points at (products, users, ...), read through their lookups.
/// A lookup that is not registered, because its module is absent, reads as empty and accepts every id.
/// </summary>
internal sealed class ExternalReferences(IClientLookup? client = null, IProductLookup? product = null, IUnitMeasureLookup? unitMeasure = null, IUserLookup? user = null)
{
    public static string? NameOf(IReadOnlyDictionary<Guid, string> names, Guid? id) =>
        id is { } value ? names.GetValueOrDefault(value, string.Empty) : null;

    public async Task<IReadOnlyList<OptionDto>> ClientOptionsAsync(CancellationToken cancellationToken) =>
        client is null ? [] : [.. (await client.ListAsync(cancellationToken).ConfigureAwait(false)).Select(i => new OptionDto(i.Id, i.Name))];

    public async Task<IReadOnlyDictionary<Guid, string>> ClientNamesAsync(CancellationToken cancellationToken) =>
        (await ClientOptionsAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(o => o.Id, o => o.Name);

    public async Task<bool> ClientExistsAsync(Guid id, CancellationToken cancellationToken) =>
        client is null || await client.ExistsAsync(id, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<OptionDto>> ProductOptionsAsync(CancellationToken cancellationToken) =>
        product is null ? [] : [.. (await product.ListAsync(cancellationToken).ConfigureAwait(false)).Select(i => new OptionDto(i.Id, i.Name))];

    public async Task<IReadOnlyDictionary<Guid, string>> ProductNamesAsync(CancellationToken cancellationToken) =>
        (await ProductOptionsAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(o => o.Id, o => o.Name);

    public async Task<bool> ProductExistsAsync(Guid id, CancellationToken cancellationToken) =>
        product is null || await product.ExistsAsync(id, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<OptionDto>> UnitMeasureOptionsAsync(CancellationToken cancellationToken) =>
        unitMeasure is null ? [] : [.. (await unitMeasure.ListAsync(cancellationToken).ConfigureAwait(false)).Select(i => new OptionDto(i.Id, i.Name))];

    public async Task<IReadOnlyDictionary<Guid, string>> UnitMeasureNamesAsync(CancellationToken cancellationToken) =>
        (await UnitMeasureOptionsAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(o => o.Id, o => o.Name);

    public async Task<bool> UnitMeasureExistsAsync(Guid id, CancellationToken cancellationToken) =>
        unitMeasure is null || await unitMeasure.ExistsAsync(id, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<OptionDto>> UserOptionsAsync(CancellationToken cancellationToken) =>
        user is null ? [] : [.. (await user.ListAsync(cancellationToken).ConfigureAwait(false)).Select(i => new OptionDto(i.Id, i.FullName))];

    public async Task<IReadOnlyDictionary<Guid, string>> UserNamesAsync(CancellationToken cancellationToken) =>
        (await UserOptionsAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(o => o.Id, o => o.Name);

    public async Task<bool> UserExistsAsync(Guid id, CancellationToken cancellationToken) =>
        user is null || await user.GetAsync(id, cancellationToken).ConfigureAwait(false) is not null;
}