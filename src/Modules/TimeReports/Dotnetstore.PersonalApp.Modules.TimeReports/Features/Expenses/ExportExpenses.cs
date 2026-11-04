using System.Globalization;
using ClosedXML.Excel;
using Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;
using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.Expenses;

/// <summary>
/// Builds an Excel workbook with one user's expenses for a calendar year, for tax purposes: a sheet listing every expense
/// (date, category, description, client, amount, VAT, receipt file name) with totals, and a sheet with the totals per category.
/// Headings are in the current UI language.
/// </summary>
internal static class ExportExpenses
{
    public sealed record Query(Guid UserId, int Year);

    public sealed record Response(string FileName, byte[] Content, int ExpenseCount);

    public sealed class Handler(IDbContextFactory<TimeReportsDbContext> dbFactory, IClientLookup clients, ILocalizationService localization)
    {
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

            var rows = await db.Expenses.AsNoTracking()
                .Where(e => e.UserId == query.UserId && e.Date >= from && e.Date <= to)
                .OrderBy(e => e.Date)
                .ThenBy(e => e.Id)
                .Select(e => new Row(e.Date, e.Category, e.Description, e.ClientId, e.Amount, e.VatAmount, e.Receipt != null ? e.Receipt.FileName : null))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (rows.Count == 0)
            {
                return Error.NotFound("expense_export_empty", "There are no expenses to export.");
            }

            var names = (await clients.ListAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(c => c.Id, c => c.Name);

            using var workbook = new XLWorkbook();
            WriteExpenses(workbook.AddWorksheet(localization.Get("ExpenseExport_SheetExpenses")), query.Year, rows, names);
            WriteSummary(workbook.AddWorksheet(localization.Get("ExpenseExport_SheetSummary")), query.Year, rows);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new Response(string.Create(CultureInfo.InvariantCulture, $"Expenses {query.Year}.xlsx"), stream.ToArray(), rows.Count);
        }

        private void WriteExpenses(IXLWorksheet sheet, int year, List<Row> rows, Dictionary<Guid, string> names)
        {
            WriteTitle(sheet, "ExpenseExport_Title", year);

            string[] headings =
            [
                "ExpenseExport_Date", "ExpenseExport_Category", "ExpenseExport_Description", "ExpenseExport_Client",
                "ExpenseExport_Amount", "ExpenseExport_Vat", "ExpenseExport_Receipt",
            ];
            WriteHeadings(sheet, headings);

            var row = FirstDataRow;
            foreach (var expense in rows)
            {
                sheet.Cell(row, 1).Value = expense.Date.ToDateTime(TimeOnly.MinValue);
                sheet.Cell(row, 1).Style.DateFormat.Format = "yyyy-mm-dd";
                sheet.Cell(row, 2).Value = CategoryName(expense.Category);
                sheet.Cell(row, 3).Value = expense.Description;
                sheet.Cell(row, 4).Value = expense.ClientId is { } id ? names.GetValueOrDefault(id, localization.Get("ExpenseExport_UnknownClient")) : string.Empty;
                sheet.Cell(row, 5).Value = expense.Amount;
                sheet.Cell(row, 6).Value = expense.VatAmount;
                sheet.Cell(row, 7).Value = expense.ReceiptFileName ?? string.Empty;
                row++;
            }

            sheet.Cell(row, 1).Value = localization.Get("ExpenseExport_Total");
            for (var col = 5; col <= 6; col++)
            {
                sheet.Cell(row, col).FormulaA1 = $"SUM({sheet.Cell(FirstDataRow, col).Address}:{sheet.Cell(row - 1, col).Address})";
            }

            sheet.Range(FirstDataRow, 5, row, 6).Style.NumberFormat.Format = "#,##0.00";
            sheet.Row(row).Style.Font.Bold = true;
            sheet.Columns().AdjustToContents();
        }

        private void WriteSummary(IXLWorksheet sheet, int year, List<Row> rows)
        {
            WriteTitle(sheet, "ExpenseExport_SummaryTitle", year);
            WriteHeadings(sheet, ["ExpenseExport_Category", "ExpenseExport_Amount", "ExpenseExport_Vat"]);

            var row = FirstDataRow;
            foreach (var group in rows.GroupBy(r => r.Category).OrderBy(g => g.Key))
            {
                sheet.Cell(row, 1).Value = CategoryName(group.Key);
                sheet.Cell(row, 2).Value = group.Sum(r => r.Amount);
                sheet.Cell(row, 3).Value = group.Sum(r => r.VatAmount);
                row++;
            }

            sheet.Cell(row, 1).Value = localization.Get("ExpenseExport_Total");
            for (var col = 2; col <= 3; col++)
            {
                sheet.Cell(row, col).FormulaA1 = $"SUM({sheet.Cell(FirstDataRow, col).Address}:{sheet.Cell(row - 1, col).Address})";
            }

            sheet.Range(FirstDataRow, 2, row, 3).Style.NumberFormat.Format = "#,##0.00";
            sheet.Row(row).Style.Font.Bold = true;
            sheet.Columns().AdjustToContents();
        }

        private void WriteTitle(IXLWorksheet sheet, string key, int year)
        {
            sheet.Cell(1, 1).Value = string.Format(CultureInfo.CurrentCulture, localization.Get(key), year);
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;
        }

        private void WriteHeadings(IXLWorksheet sheet, string[] keys)
        {
            for (var i = 0; i < keys.Length; i++)
            {
                var cell = sheet.Cell(3, i + 1);
                cell.Value = localization.Get(keys[i]);
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }
        }

        private string CategoryName(Domain.ExpenseCategory category) => localization.Get($"ExpenseCategory_{category}");
    }

    private sealed record Row(
        DateOnly Date,
        Domain.ExpenseCategory Category,
        string Description,
        Guid? ClientId,
        decimal Amount,
        decimal VatAmount,
        string? ReceiptFileName);
}
