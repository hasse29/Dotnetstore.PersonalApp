using ClosedXML.Excel;
using Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Tests;

public sealed class ExportKilometerSummaryTests : IDisposable
{
    private readonly TimeReportsFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();

    public void Dispose() => _fixture.Dispose();

    private async Task AddAsync(Guid userId, Guid clientId, DateOnly date, decimal km)
    {
        var command = TimeReportsFixture.ValidCommand(userId, clientId) with { Date = date, DrivingKilometers = km };
        (await _fixture.Create.HandleAsync(command, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task SummarizesKilometersPerClientAndMonthWithTotals()
    {
        var acme = _fixture.Clients.Add("Acme AB");
        var beta = _fixture.Clients.Add("Beta AB");
        await AddAsync(_user, acme.Id, new DateOnly(2026, 1, 5), 10m);
        await AddAsync(_user, acme.Id, new DateOnly(2026, 1, 20), 5.5m);
        await AddAsync(_user, acme.Id, new DateOnly(2026, 3, 1), 20m);
        await AddAsync(_user, beta.Id, new DateOnly(2026, 3, 2), 7m);
        await AddAsync(_user, beta.Id, new DateOnly(2025, 12, 31), 99m);
        await AddAsync(Guid.NewGuid(), beta.Id, new DateOnly(2026, 3, 3), 99m);

        var result = await _fixture.ExportKilometers.HandleAsync(new ExportKilometerSummary.Query(_user, 2026), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        using var workbook = new XLWorkbook(new MemoryStream(result.Value.Content));
        var sheet = workbook.Worksheets.ShouldHaveSingleItem();
        sheet.Name.ShouldBe("2026");
        sheet.Cell(4, 1).GetString().ShouldBe("Acme AB");
        (sheet.Cell(4, 2).GetValue<decimal>()).ShouldBe(15.5m);
        (sheet.Cell(4, 4).GetValue<decimal>()).ShouldBe(20m);
        sheet.Cell(5, 1).GetString().ShouldBe("Beta AB");
        (sheet.Cell(5, 4).GetValue<decimal>()).ShouldBe(7m);

        // Formulas are not calculated in the file, so evaluate them the way Excel would.
        ((decimal)sheet.Cell(4, 14).Value.GetNumber()).ShouldBe(35.5m);
        ((decimal)sheet.Cell(6, 14).Value.GetNumber()).ShouldBe(42.5m);
        ((decimal)sheet.Cell(6, 4).Value.GetNumber()).ShouldBe(27m);
    }

    [Fact]
    public async Task ReportsWhenThereIsNothingToExport()
    {
        var result = await _fixture.ExportKilometers.HandleAsync(new ExportKilometerSummary.Query(_user, 2026), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("time_report_export_empty");
    }
}