using Dotnetstore.PersonalApp.Modules.Grades.Domain;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;

internal static class CreateTest
{
    public sealed record Command(Guid UserId, string Name, DateOnly Date, double MaxScore);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NameRules();
            RuleFor(c => c.MaxScore).MaxScoreRules();
        }
    }

    public sealed class Handler(IDbContextFactory<GradesDbContext> dbFactory, IValidator<Command> validator, TimeProvider clock)
    {
        public Task<Result<TestDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<TestDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var test = new Test
            {
                Id = Guid.CreateVersion7(),
                UserId = command.UserId,
                Name = command.Name.Trim(),
                Date = command.Date,
                MaxScore = command.MaxScore,
                CreatedAt = clock.GetUtcNow(),
            };

            db.Tests.Add(test);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return TestDto.From(test, 0);
        }
    }
}
