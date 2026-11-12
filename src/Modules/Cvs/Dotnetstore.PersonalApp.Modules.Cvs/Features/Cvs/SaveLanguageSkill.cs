using Dotnetstore.PersonalApp.Modules.Cvs.Domain;
using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>Adds a language to the user's own CV, or changes one when <see cref="Command.Id"/> is given. A language appears once per CV.</summary>
internal static class SaveLanguageSkill
{
    public sealed record Command(Guid CvId, Guid UserId, Guid? Id, string Language, LanguageLevel Level);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.CvId).CvIdRules();
            RuleFor(c => c.Language).RequiredText("cv_language_required", "cv_language_too_long", CvSectionRules.MaxLanguageLength);
            RuleFor(c => c.Level).IsInEnum().WithErrorCode("cv_language_level_invalid").WithMessage("Language level is not valid.");
        }
    }

    public sealed class Handler(IDbContextFactory<CvsDbContext> dbFactory, IValidator<Command> validator)
    {
        public Task<Result<LanguageSkillDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<LanguageSkillDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (!await db.OwnsCvAsync(command.CvId, command.UserId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("cv_not_found", "CV not found.");
            }

            var language = command.Language.Trim();
            if (await db.LanguageSkills.AnyAsync(
                    l => l.CvId == command.CvId && l.Language == language && l.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("cv_language_conflict", $"'{language}' is already on this CV.");
            }

            LanguageSkill skill;
            if (command.Id is { } id)
            {
                var existing = await db.LanguageSkills.FirstOrDefaultAsync(l => l.Id == id && l.CvId == command.CvId, cancellationToken).ConfigureAwait(false);
                if (existing is null)
                {
                    return Error.NotFound("cv_item_not_found", "The entry was not found.");
                }

                skill = existing;
            }
            else
            {
                skill = new LanguageSkill { Id = Guid.CreateVersion7(), CvId = command.CvId };
                db.LanguageSkills.Add(skill);
            }

            skill.Language = language;
            skill.Level = command.Level;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return LanguageSkillDto.From(skill);
        }
    }
}
