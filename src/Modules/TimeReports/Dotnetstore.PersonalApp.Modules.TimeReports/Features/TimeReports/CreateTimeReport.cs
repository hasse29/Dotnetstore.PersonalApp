using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;
using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;

internal static class CreateTimeReport
{
    public sealed record Command(
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

    public sealed class Handler(
        IDbContextFactory<TimeReportsDbContext> dbFactory,
        IValidator<Command> validator,
        IClientLookup clients,
        TimeProvider clock)
    {
        public Task<Result<TimeReportDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<TimeReportDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var client = (await clients.ListAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(c => c.Id == command.ClientId);
            if (client is null)
            {
                return Error.NotFound("time_report_client_not_found", "Client not found.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var typeName = await db.WorkTimeTypes.AsNoTracking()
                .Where(t => t.Id == command.WorkTimeTypeId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (typeName is null)
            {
                return Error.NotFound("time_report_work_time_type_not_found", "Work time type not found.");
            }

            var report = new TimeReport
            {
                Id = Guid.CreateVersion7(),
                UserId = command.UserId,
                ClientId = command.ClientId,
                WorkTimeTypeId = command.WorkTimeTypeId,
                Date = command.Date,
                StartTime = command.StartTime,
                EndTime = command.EndTime,
                TotalHours = TimeReportRules.RoundHours(command.TotalHours),
                Location = TimeReportRules.NormalizeOptional(command.Location),
                DrivingKilometers = TimeReportRules.RoundKilometers(command.DrivingKilometers),
                CreatedAt = clock.GetUtcNow(),
            };

            db.TimeReports.Add(report);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return TimeReportDto.From(report, client.Name, typeName);
        }
    }
}