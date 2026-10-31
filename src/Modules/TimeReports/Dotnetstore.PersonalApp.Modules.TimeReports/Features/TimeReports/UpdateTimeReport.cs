using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;

/// <summary>Changes one of the user's own reports; someone else's report is reported as not found.</summary>
internal static class UpdateTimeReport
{
    public sealed record Command(
        Guid Id,
        Guid UserId,
        Guid ClientId,
        Guid WorkTimeTypeId,
        DateOnly Date,
        TimeOnly StartTime,
        TimeOnly EndTime,
        decimal TotalHours,
        string? Location,
        decimal DrivingKilometers);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.UserId).UserIdRules();
            RuleFor(c => c.ClientId).ClientIdRules();
            RuleFor(c => c.WorkTimeTypeId).WorkTimeTypeIdRules();
            RuleFor(c => c.Date).DateRules();
            RuleFor(c => c.EndTime).GreaterThan(c => c.StartTime)
                .WithErrorCode("time_report_end_before_start").WithMessage("End time must be after start time.");
            RuleFor(c => c.TotalHours).TotalHoursRules();
            RuleFor(c => c.Location).LocationRules();
            RuleFor(c => c.DrivingKilometers).DrivingKilometersRules();
        }
    }

    public sealed class Handler(IDbContextFactory<TimeReportsDbContext> dbFactory, IValidator<Command> validator, IClientLookup clients)
    {
        public async Task<Result<TimeReportDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var report = await db.TimeReports
                .FirstOrDefaultAsync(r => r.Id == command.Id && r.UserId == command.UserId, cancellationToken)
                .ConfigureAwait(false);
            if (report is null)
            {
                return Error.NotFound("time_report_not_found", "Time report not found.");
            }

            var client = (await clients.ListAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(c => c.Id == command.ClientId);
            if (client is null)
            {
                return Error.NotFound("time_report_client_not_found", "Client not found.");
            }

            var typeName = await db.WorkTimeTypes.AsNoTracking()
                .Where(t => t.Id == command.WorkTimeTypeId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (typeName is null)
            {
                return Error.NotFound("time_report_work_time_type_not_found", "Work time type not found.");
            }

            report.ClientId = command.ClientId;
            report.WorkTimeTypeId = command.WorkTimeTypeId;
            report.Date = command.Date;
            report.StartTime = command.StartTime;
            report.EndTime = command.EndTime;
            report.TotalHours = TimeReportRules.RoundHours(command.TotalHours);
            report.Location = TimeReportRules.NormalizeOptional(command.Location);
            report.DrivingKilometers = TimeReportRules.RoundKilometers(command.DrivingKilometers);

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return TimeReportDto.From(report, client.Name, typeName);
        }
    }
}