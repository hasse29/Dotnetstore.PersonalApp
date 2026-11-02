using ClosedXML.Excel;
using Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Tests;

public sealed class ExportTimeReportsTests : IDisposable
{
    private readonly TimeReportsFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();

    public void Dispose() => _fixture.Dispose();

    private async Task AddAsync(Guid userId, Guid clientId, DateOnly date)
    {
        var command = TimeReportsFixture.ValidCommand(userId, clientId) with { Date = date };
        (await _fixture.Create.HandleAsync(command, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ExportsOneSheetPerMonthNamedAfterYearAndMonthForTheChosenClientAndYear()
    {
        var acme = _fixture.Clients.Add("Acme AB");
        var other = _fixture.Clients.Add("Other AB");
        await AddAsync(_user, acme.Id, new DateOnly(2026, 11, 3));
        await AddAsync(_user, acme.Id, new DateOnly(2026, 10, 30));
        await AddAsync(_user, acme.Id, new DateOnly(2026, 10, 2));
        await AddAsync(_user, acme.Id, new DateOnly(2025, 12, 31));
        await AddAsync(_user, other.Id, new DateOnly(2026, 10, 5));
        await AddAsync(Guid.NewGuid(), acme.Id, new DateOnly(2026, 10, 6));

        var result = await _fixture.Export.HandleAsync(new ExportTimeReports.Query(_user, acme.Id, 2026), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ReportCount.ShouldBe(3);
        result.Value.FileName.ShouldBe("TimeReports Acme AB 2026.xlsx");
        using var workbook = new XLWorkbook(new MemoryStream(result.Value.Content));
        workbook.Worksheets.Select(s => s.Name).ShouldBe(["2026-10", "2026-11"]);
        var october = workbook.Worksheet("2026-10");
        october.Cell(4, 1).GetDateTime().ShouldBe(new DateTime(2026, 10, 2));
        (october.Cell(4, 2).GetValue<decimal>()).ShouldBe(8m);
        october.Cell(4, 3).GetString().ShouldBe("Stockholm");
        october.Cell(6, 2).FormulaA1.ShouldBe("SUM(B4:B5)");
    }

    [Fact]
    public async Task ReportsWhenThereIsNothingToExport()
    {
        var acme = _fixture.Clients.Add("Acme AB");

        var result = await _fixture.Export.HandleAsync(new ExportTimeReports.Query(_user, acme.Id, 2026), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("time_report_export_empty");
    }

    [Fact]
    public async Task RejectsAnUnknownClientAndAnInvalidYear()
    {
        var acme = _fixture.Clients.Add("Acme AB");
        var ct = TestContext.Current.CancellationToken;

        (await _fixture.Export.HandleAsync(new ExportTimeReports.Query(_user, Guid.NewGuid(), 2026), ct)).Error.Code.ShouldBe("time_report_client_not_found");
        (await _fixture.Export.HandleAsync(new ExportTimeReports.Query(_user, acme.Id, 1900), ct)).Error.Code.ShouldBe("time_report_year_invalid");
    }
}