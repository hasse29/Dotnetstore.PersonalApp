using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.Expenses;

/// <summary>A receipt the user attached: the file's name (its extension decides the type) and bytes.</summary>
internal sealed record ReceiptFile(string FileName, byte[] Content);

/// <summary>Shared validation rules. Each rule carries an error code (<c>expense_*</c>) that the UI translates via <c>Error_{code}</c>.</summary>
internal static class ExpenseRules
{
    public const int MaxDescriptionLength = 200;
    public const decimal MaxAmount = 10_000_000m;
    public const long MaxReceiptBytes = 10 * 1024 * 1024;

    private static readonly string[] ReceiptExtensions = [".pdf", ".png", ".jpg", ".jpeg"];

    public static IRuleBuilderOptions<T, Guid> UserIdRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("expense_user_required").WithMessage("An expense must belong to a user.");

    public static IRuleBuilderOptions<T, DateOnly> DateRules<T>(this IRuleBuilder<T, DateOnly> rule) =>
        rule.NotEqual(default(DateOnly)).WithErrorCode("expense_date_required").WithMessage("Date is required.");

    public static IRuleBuilderOptions<T, ExpenseCategory> CategoryRules<T>(this IRuleBuilder<T, ExpenseCategory> rule) =>
        rule.IsInEnum().WithErrorCode("expense_category_invalid").WithMessage("Category is not valid.");

    public static IRuleBuilderOptions<T, string> DescriptionRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("expense_description_required").WithMessage("Description is required.")
            .MaximumLength(MaxDescriptionLength).WithErrorCode("expense_description_too_long").WithMessage("Description is too long.");

    public static IRuleBuilderOptions<T, decimal> AmountRules<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThan(0m).WithErrorCode("expense_amount_invalid").WithMessage("Amount must be more than 0.")
            .LessThanOrEqualTo(MaxAmount).WithErrorCode("expense_amount_invalid").WithMessage("Amount must be more than 0.");

    /// <summary>The VAT is part of the amount, so it can be neither negative nor more than the amount.</summary>
    public static IRuleBuilderOptions<T, decimal> VatRules<T>(this IRuleBuilder<T, decimal> rule, Func<T, decimal> amount) =>
        rule.Must((c, vat) => vat >= 0m && vat <= amount(c)).WithErrorCode("expense_vat_invalid").WithMessage("VAT must be between 0 and the amount.");

    public static IRuleBuilderOptions<T, ReceiptFile?> ReceiptRules<T>(this IRuleBuilder<T, ReceiptFile?> rule) =>
        rule.Must(r => r is null || r.Content.Length > 0).WithErrorCode("expense_receipt_empty").WithMessage("The receipt file is empty.")
            .Must(r => r is null || r.Content.Length <= MaxReceiptBytes).WithErrorCode("expense_receipt_too_large").WithMessage("The receipt file is too large.")
            .Must(r => r is null || HasAllowedType(r.FileName)).WithErrorCode("expense_receipt_type_invalid").WithMessage("The receipt must be a PDF, PNG or JPEG file.");

    public static bool HasAllowedType(string fileName) =>
        ReceiptExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);

    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>Keeps only the file name, so a full path never ends up in the database.</summary>
    public static string ReceiptName(string fileName) => Path.GetFileName(fileName.Trim());

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }
}
