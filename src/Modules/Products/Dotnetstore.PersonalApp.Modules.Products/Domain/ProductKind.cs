namespace Dotnetstore.PersonalApp.Modules.Products.Domain;

/// <summary>What sort of product it is. Stored by name, so members may be added but not renamed.</summary>
internal enum ProductKind
{
    /// <summary>Physical goods: network cables, tools, computer hardware.</summary>
    Hardware,

    /// <summary>Software the owner has made.</summary>
    Software,

    /// <summary>Consulting hours.</summary>
    Consulting,
}
