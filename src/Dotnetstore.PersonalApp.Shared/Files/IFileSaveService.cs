namespace Dotnetstore.PersonalApp.Shared.Files;

/// <summary>Lets a view model hand a finished file to the user without touching UI types; the Host shows the save dialog.</summary>
public interface IFileSaveService
{
    /// <param name="title">Dialog title.</param>
    /// <param name="suggestedFileName">File name including its extension, e.g. <c>report.xlsx</c>.</param>
    /// <param name="content">The file's bytes.</param>
    /// <returns>The saved file's path or name, or null when the user cancelled.</returns>
    Task<string?> SaveAsync(string title, string suggestedFileName, byte[] content, CancellationToken cancellationToken);
}