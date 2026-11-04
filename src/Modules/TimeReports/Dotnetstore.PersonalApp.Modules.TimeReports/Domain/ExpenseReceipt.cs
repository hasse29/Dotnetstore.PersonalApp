namespace Dotnetstore.PersonalApp.Modules.TimeReports.Domain;

/// <summary>A copy of the receipt for one expense. Kept in its own table so listing expenses never loads the file.</summary>
internal sealed class ExpenseReceipt
{
    public Guid ExpenseId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public byte[] Content { get; set; } = [];
}
