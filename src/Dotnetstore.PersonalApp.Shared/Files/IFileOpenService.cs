namespace Dotnetstore.PersonalApp.Shared.Files;

/// <summary>A file the user picked.</summary>
public sealed record PickedFile(string Name, ReadOnlyMemory<byte> Content);

/// <summary>Lets a view model ask the user for a file without touching UI types; the Host shows the open dialog.</summary>
public interface IFileOpenService
{
    /// <param name="title">Dialog title.</param>
    /// <param name="maxBytes">Files larger than this are not read.</param>
    /// <returns>The chosen image, or null when the user cancelled or the file was too large to read.</returns>
    Task<PickedFile?> PickImageAsync(string title, long maxBytes, CancellationToken cancellationToken);
}
