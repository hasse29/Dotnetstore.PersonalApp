namespace Dotnetstore.PersonalApp.Shared.Files;

/// <summary>The files that could be read, and the names of those that were too large or could not be opened.</summary>
public sealed record FilePickResult(IReadOnlyList<PickedFile> Files, IReadOnlyList<string> Skipped);

/// <summary>Lets a view model ask the user for any files (e.g. mail attachments) without touching UI types; the Host shows the open dialog.</summary>
public interface IFilePickService
{
    /// <param name="title">Dialog title.</param>
    /// <param name="maxBytes">Files larger than this are not read and are reported as skipped.</param>
    /// <returns>The chosen files that could be read; empty when the user cancelled.</returns>
    Task<FilePickResult> PickFilesAsync(string title, long maxBytes, CancellationToken cancellationToken);
}
