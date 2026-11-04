using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.Production.Features;

/// <summary>Helpers shared by the validators and handlers of the Production module.</summary>
internal static class ProductionRules
{
    /// <summary>Optional text fields: trimmed, and empty becomes null.</summary>
    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }
}