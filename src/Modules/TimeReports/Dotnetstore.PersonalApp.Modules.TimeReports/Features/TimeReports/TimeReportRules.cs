using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;

/// <summary>
/// Shared validation rules. Each rule carries an error code (e.g. <c>time_report_end_before_start</c>) that the UI translates
/// via the <c>Error_{code}</c> resource; the English message is only a fallback.
/// </summary>
internal static class TimeReportRules
{
    public const int MaxLocationLength = 200;
    public const decimal MaxHoursPerReport = 24m;
    public const decimal MaxKilometersPerReport = 10_000m;

    public static IRuleBuilderOptions<T, Guid> UserIdRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("time_report_user_required").WithMessage("A time report must belong to a user.");

    public static IRuleBuilderOptions<T, Guid> ClientIdRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("time_report_client_required").WithMessage("Client is required.");

    public static IRuleBuilderOptions<T, Guid> WorkTimeTypeIdRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("time_report_work_time_type_required").WithMessage("Work time type is required.");

    public static IRuleBuilderOptions<T, DateOnly> DateRules<T>(this IRuleBuilder<T, DateOnly> rule) =>
        rule.NotEqual(default(DateOnly)).WithErrorCode("time_report_date_required").WithMessage("Date is required.");

    public static IRuleBuilderOptions<T, decimal> TotalHoursRules<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThan(0m).WithErrorCode("time_report_total_hours_invalid").WithMessage("Total hours must be between 0 and 24.")
            .LessThanOrEqualTo(MaxHoursPerReport).WithErrorCode("time_report_total_hours_invalid").WithMessage("Total hours must be between 0 and 24.");

    public static IRuleBuilderOptions<T, string?> LocationRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxLocationLength).WithErrorCode("time_report_location_too_long").WithMessage("Location is too long.");

    public static IRuleBuilderOptions<T, decimal> DrivingKilometersRules<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.InclusiveBetween(0m, MaxKilometersPerReport).WithErrorCode("time_report_driving_invalid")
            .WithMessage("Driving must be between 0 and 10 000 km.");

    /// <summary>Optional text fields: trimmed, and empty becomes null.</summary>
    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static decimal RoundHours(decimal hours) => Math.Round(hours, 2, MidpointRounding.AwayFromZero);

    public static decimal RoundKilometers(decimal kilometers) => Math.Round(kilometers, 1, MidpointRounding.AwayFromZero);

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }
}