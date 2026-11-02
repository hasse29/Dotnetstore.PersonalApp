using Dotnetstore.PersonalApp.Modules.Schedule.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Subjects;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;

internal static class UpdateScheduleEntry
{
    /// <summary>The owner is part of the command so that one teacher cannot change another one's lessons.</summary>
    public sealed record Command(
        Guid Id,
        Guid UserId,
        DayOfWeek Day,
        TimeOnly StartTime,
        TimeOnly EndTime,
        Guid SubjectId,
        Guid ClientId,
        string? Room,
        string? Notes);

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
        }
    }

    public sealed class Handler(
        IDbContextFactory<ScheduleDbContext> dbFactory,
        IValidator<Command> validator,
        ISubjectLookup subjects,
        IClientLookup clients)
    {
        public async Task<Result<ScheduleEntryDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entry = await db.ScheduleEntries
                .FirstOrDefaultAsync(e => e.Id == command.Id && e.UserId == command.UserId, cancellationToken)
                .ConfigureAwait(false);
            if (entry is null)
            {
                return Error.NotFound("schedule_entry_not_found", "Lesson not found.");
            }

            var problem = await ScheduleRules.CheckAsync(
                db, subjects, clients, command.UserId, command.Day, command.StartTime, command.EndTime,
                command.SubjectId, command.ClientId, ignoreEntryId: entry.Id, cancellationToken).ConfigureAwait(false);
            if (problem is { } error)
            {
                return error;
            }

            entry.Day = command.Day;
            entry.StartTime = command.StartTime;
            entry.EndTime = command.EndTime;
            entry.SubjectId = command.SubjectId;
            entry.ClientId = command.ClientId;
            entry.Room = ScheduleRules.NormalizeOptional(command.Room);
            entry.Notes = ScheduleRules.NormalizeOptional(command.Notes);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ScheduleEntryDto.From(entry);
        }
    }
}
