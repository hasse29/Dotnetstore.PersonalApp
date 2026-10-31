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
        Assert.True((await _fixture.Create.HandleAsync(command, TestContext.Current.CancellationToken)).IsSuccess);
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

        Assert.True(result.IsSuccess);
        using var workbook = new XLWorkbook(new MemoryStream(result.Value.Content));
        var sheet = Assert.Single(workbook.Worksheets);
        Assert.Equal("2026", sheet.Name);
        Assert.Equal("Acme AB", sheet.Cell(4, 1).GetString());
        Assert.Equal(15.5m, sheet.Cell(4, 2).GetValue<decimal>());
        Assert.Equal(20m, sheet.Cell(4, 4).GetValue<decimal>());
        Assert.Equal("Beta AB", sheet.Cell(5, 1).GetString());
        Assert.Equal(7m, sheet.Cell(5, 4).GetValue<decimal>());

        // Formulas are not calculated in the file, so evaluate them the way Excel would.
        Assert.Equal(35.5m, (decimal)sheet.Cell(4, 14).Value.GetNumber());
        Assert.Equal(42.5m, (decimal)sheet.Cell(6, 14).Value.GetNumber());
        Assert.Equal(27m, (decimal)sheet.Cell(6, 4).Value.GetNumber());
    }

    [Fact]
    public async Task ReportsWhenThereIsNothingToExport()
    {
        var result = await _fixture.ExportKilometers.HandleAsync(new ExportKilometerSummary.Query(_user, 2026), TestContext.Current.CancellationToken);

        Assert.Equal("time_report_export_empty", result.Error.Code);
    }
}