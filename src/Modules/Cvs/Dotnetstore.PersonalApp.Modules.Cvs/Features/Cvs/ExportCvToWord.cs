using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>Builds a Word document (.docx) of one of the user's own CVs, in the current UI language.</summary>
internal static class ExportCvToWord
{
    public sealed record Query(Guid Id, Guid UserId);

    public sealed record Response(string FileName, byte[] Content);

    public sealed class Handler(GetCvDetail.Handler getDetail, ILocalizationService localization)
    {
        public async Task<Result<Response>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var detail = await getDetail.HandleAsync(new GetCvDetail.Query(query.Id, query.UserId), cancellationToken).ConfigureAwait(false);
            if (!detail.IsSuccess)
            {
                return detail.Error;
            }

            var content = CvWordDocument.Build(detail.Value, localization);
            return new Response(FileNameFor(detail.Value), content);
        }

        private static string FileNameFor(CvDetailDto detail)
        {
            var name = detail.User is { } user ? $"CV - {user.FullName} - {detail.Cv.Title}" : $"CV - {detail.Cv.Title}";
            var invalid = Path.GetInvalidFileNameChars();
            return string.Concat(name.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c)) + ".docx";
        }
    }
}
