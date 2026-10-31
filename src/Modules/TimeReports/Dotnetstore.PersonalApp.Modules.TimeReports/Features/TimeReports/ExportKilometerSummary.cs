using System.Globalization;
using ClosedXML.Excel;
using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;

/// <summary>
/// Builds a one-sheet Excel workbook of one user's driving for a calendar year, for tax purposes: a row per client, a column
/// per month, row and column totals, and a grand total for the whole year in the bottom-right cell.
/// </summary>
internal static class ExportKilometerSummary
{
    public sealed record Query(Guid UserId, int Year);

    public sealed record Response(string FileName, byte[] Content, int ReportCount);

    public sealed class Handler(IDbContextFactory<TimeReportsDbContext> dbFactory, IClientLookup clients, ILocalizationService localization)
    {
        private const int FirstMonthColumn = 2;
        private const int TotalColumn = FirstMonthColumn + 12;
        private const int FirstDataRow = 4;

        public async Task<Result<Response>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            if (query.Year is < ExportTimeReports.MinYear or > ExportTimeReports.MaxYear)
            {
                return Error.Validation("time_report_year_invalid", "The year is not valid.");
            }

            var from = new DateOnly(query.Year, 1, 1);
            var to = new DateOnly(query.Year, 12, 31);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var drives = await db.TimeReports.AsNoTracking()
                .Where(r => r.UserId == query.UserId && r.Date >= from && r.Date <= to)
                .Select(r => new { r.ClientId, r.Date.Month, r.DrivingKilometers })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (drives.Count == 0)
            {
                return Error.NotFound("time_report_export_empty", "There are no time reports to export.");
            }

            var names = (await clients.ListAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(c => c.Id, c => c.Name);
            var unknown = localization.Get("TimeReportExport_UnknownClient");

            var rows = drives
                .GroupBy(d => d.ClientId)
                .Select(g => (Name: names.GetValueOrDefault(g.Key, unknown), Months: Enumerable.Range(1, 12).Select(m => g.Where(d => d.Month == m).Sum(d => d.DrivingKilometers)).ToArray()))
                .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            using var workbook = new XLWorkbook();
            var sheet = workbook.AddWorksheet(query.Year.ToString(CultureInfo.InvariantCulture));
            Write(sheet, query.Year, rows);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new Response(string.Create(CultureInfo.InvariantCulture, $"Kilometers {query.Year}.xlsx"), stream.ToArray(), drives.Count);
        }

        private void Write(IXLWorksheet sheet, int year, List<(string Name, decimal[] Months)> rows)
        {
            sheet.Cell(1, 1).Value = string.Format(CultureInfo.CurrentCulture, localization.Get("TimeReportExport_KilometersTitle"), year);
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;

            var months = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames;
            sheet.Cell(3, 1).Value = localization.Get("TimeReportExport_Client");
            for (var m = 0; m < 12; m++)
            {
                sheet.Cell(3, FirstMonthColumn + m).Value = months[m];
            }

            sheet.Cell(3, TotalColumn).Value = localization.Get("TimeReportExport_Total");
            var header = sheet.Range(3, 1, 3, TotalColumn);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;

            var row = FirstDataRow;
            foreach (var (name, perMonth) in rows)
            {
                sheet.Cell(row, 1).Value = name;
                for (var m = 0; m < 12; m++)
                {
                    sheet.Cell(row, FirstMonthColumn + m).Value = perMonth[m];
                }

                sheet.Cell(row, TotalColumn).FormulaA1 = $"SUM({sheet.Cell(row, FirstMonthColumn).Address}:{sheet.Cell(row, TotalColumn - 1).Address})";
                row++;
            }

            // Column totals, with the grand total for the year in the last cell.
            sheet.Cell(row, 1).Value = localization.Get("TimeReportExport_Total");
            for (var col = FirstMonthColumn; col <= TotalColumn; col++)
            {
                sheet.Cell(row, col).FormulaA1 = $"SUM({sheet.Cell(FirstDataRow, col).Address}:{sheet.Cell(row - 1, col).Address})";
            }

            sheet.Range(FirstDataRow, FirstMonthColumn, row, TotalColumn).Style.NumberFormat.Format = "0.0";
            sheet.Range(row, 1, row, TotalColumn).Style.Font.Bold = true;
            sheet.Range(FirstDataRow, TotalColumn, row, TotalColumn).Style.Font.Bold = true;
            sheet.Columns().AdjustToContents();
        }
    }
}