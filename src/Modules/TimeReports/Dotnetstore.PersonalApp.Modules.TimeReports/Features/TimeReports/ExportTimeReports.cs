using System.Globalization;
using ClosedXML.Excel;
using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;

/// <summary>
/// Builds an Excel workbook with one user's reports (date, hours worked and location) for one client and calendar year, one sheet per month that has reports,
/// each named <c>yyyy-MM</c>. Column headings are in the current UI language.
/// </summary>
internal static class ExportTimeReports
{
    public const int MinYear = 2000;
    public const int MaxYear = 2100;

    public sealed record Query(Guid UserId, Guid ClientId, int Year);

    public sealed record Response(string FileName, byte[] Content, int ReportCount);

    public sealed class Handler(IDbContextFactory<TimeReportsDbContext> dbFactory, IClientLookup clients, ILocalizationService localization)
    {
        public async Task<Result<Response>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            if (query.Year is < MinYear or > MaxYear)
            {
                return Error.Validation("time_report_year_invalid", "The year is not valid.");
            }

            var client = (await clients.ListAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(c => c.Id == query.ClientId);
            if (client is null)
            {
                return Error.NotFound("time_report_client_not_found", "Client not found.");
            }

            var from = new DateOnly(query.Year, 1, 1);
            var to = new DateOnly(query.Year, 12, 31);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var reports = await db.TimeReports.AsNoTracking()
                .Where(r => r.UserId == query.UserId && r.ClientId == query.ClientId && r.Date >= from && r.Date <= to)
                .OrderBy(r => r.Date)
                .ThenBy(r => r.StartTime)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (reports.Count == 0)
            {
                return Error.NotFound("time_report_export_empty", "There are no time reports to export.");
            }

            using var workbook = new XLWorkbook();
            foreach (var month in reports.GroupBy(r => r.Date.Month))
            {
                var sheetName = string.Create(CultureInfo.InvariantCulture, $"{query.Year}-{month.Key:00}");
                WriteSheet(workbook.AddWorksheet(sheetName), client.Name, sheetName, month);
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new Response(FileName(client.Name, query.Year), stream.ToArray(), reports.Count);
        }

        private void WriteSheet(
            IXLWorksheet sheet,
            string clientName,
            string title,
            IEnumerable<Domain.TimeReport> reports)
        {
            sheet.Cell(1, 1).Value = $"{clientName} - {title}";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;

            string[] headings = ["TimeReportExport_Date", "TimeReportExport_Hours", "TimeReportExport_Location"];

            for (var i = 0; i < headings.Length; i++)
            {
                var cell = sheet.Cell(3, i + 1);
                cell.Value = localization.Get(headings[i]);
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            var row = 4;
            foreach (var report in reports)
            {
                sheet.Cell(row, 1).Value = report.Date.ToDateTime(TimeOnly.MinValue);
                sheet.Cell(row, 1).Style.DateFormat.Format = "yyyy-mm-dd";
                sheet.Cell(row, 2).Value = report.TotalHours;
                sheet.Cell(row, 2).Style.NumberFormat.Format = "0.00";
                sheet.Cell(row, 3).Value = report.Location ?? string.Empty;
                row++;
            }

            sheet.Cell(row, 1).Value = localization.Get("TimeReportExport_Total");
            sheet.Cell(row, 2).FormulaA1 = $"SUM(B4:B{row - 1})";
            sheet.Cell(row, 2).Style.NumberFormat.Format = "0.00";
            sheet.Row(row).Style.Font.Bold = true;

            sheet.Columns().AdjustToContents();
        }

        private static string FileName(string clientName, int year)
        {
            var safeName = string.Concat(clientName.Select(c => Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c));
            return string.Create(CultureInfo.InvariantCulture, $"TimeReports {safeName} {year}.xlsx");
        }
    }
}