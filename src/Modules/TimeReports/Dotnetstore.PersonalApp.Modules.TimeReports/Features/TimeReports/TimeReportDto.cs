using System.Globalization;
using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;

/// <param name="ClientName">Null when the client no longer exists.</param>
internal sealed record TimeReportDto(
    Guid Id,
    Guid ClientId,
    string? ClientName,
    Guid WorkTimeTypeId,
    string? WorkTimeTypeName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal TotalHours,
    string? Location,
    decimal DrivingKilometers)
{
    public string TimeRange => string.Create(CultureInfo.InvariantCulture, $"{StartTime:HH:mm}–{EndTime:HH:mm}");

    public static TimeReportDto From(TimeReport report, string? clientName, string? workTimeTypeName) => new(
        report.Id,
        report.ClientId,
        clientName,
        report.WorkTimeTypeId,
        workTimeTypeName,
        report.Date,
        report.StartTime,
        report.EndTime,
        report.TotalHours,
        report.Location,
        report.DrivingKilometers);
}