using Dotnetstore.PersonalApp.Modules.Schedule.Domain;
using Dotnetstore.PersonalApp.Modules.Schedule.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Students;
using Dotnetstore.PersonalApp.Shared.Subjects;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;

internal static class CreateScheduleEntry
{
    public sealed record Command(
        Guid UserId,
        DayOfWeek Day,
        TimeOnly StartTime,
        TimeOnly EndTime,
        Guid SubjectId,
        Guid ClientId,
        string? Room,
        string? Notes,
        IReadOnlyList<Guid> StudentIds);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Day).DayRules();
            RuleFor(c => c.EndTime).EndRules(c => c.StartTime);
            RuleFor(c => c.SubjectId).SubjectRules();
            RuleFor(c => c.ClientId).SchoolRules();
            RuleFor(c => c.Room).RoomRules();
            RuleFor(c => c.Notes).NotesRules();
            RuleFor(c => c.StudentIds).StudentRules();
        }
    }

    public sealed class Handler(
        IDbContextFactory<ScheduleDbContext> dbFactory,
        IValidator<Command> validator,
        ISubjectLookup subjects,
        IClientLookup clients,
        IStudentLookup students,
        TimeProvider clock)
    {
        public Task<Result<ScheduleEntryDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<ScheduleEntryDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var problem = await ScheduleRules.CheckAsync(
                db, subjects, clients, students, command.UserId, command.Day, command.StartTime, command.EndTime,
                command.SubjectId, command.ClientId, command.StudentIds, ignoreEntryId: null, cancellationToken).ConfigureAwait(false);
            if (problem is { } error)
            {
                return error;
            }

            var entry = new ScheduleEntry
            {
                Id = Guid.CreateVersion7(),
                UserId = command.UserId,
                SubjectId = command.SubjectId,
                ClientId = command.ClientId,
                Day = command.Day,
                StartTime = command.StartTime,
                EndTime = command.EndTime,
                Room = ScheduleRules.NormalizeOptional(command.Room),
                Notes = ScheduleRules.NormalizeOptional(command.Notes),
                CreatedAt = clock.GetUtcNow(),
            };
            entry.Students = [.. command.StudentIds.Distinct().Select(id => new ScheduleEntryStudent { EntryId = entry.Id, StudentId = id })];

            db.ScheduleEntries.Add(entry);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ScheduleEntryDto.From(entry);
        }
    }
}
