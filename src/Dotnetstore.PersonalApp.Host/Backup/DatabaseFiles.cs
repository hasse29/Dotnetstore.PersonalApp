namespace Dotnetstore.PersonalApp.Host.Backup;

/// <summary>The file name of every module database in the data directory. Backup and restore only ever touch these.</summary>
internal static class DatabaseFiles
{
    public const string Clients = "clients.db";
    public const string TimeReports = "timereports.db";
    public const string Addresses = "addresses.db";
    public const string EmailAddresses = "emailaddresses.db";
    public const string PhoneNumbers = "phonenumbers.db";
    public const string Cvs = "cvs.db";
    public const string Students = "students.db";
    public const string Subjects = "subjects.db";
    public const string Schedule = "schedule.db";
    public const string Grades = "grades.db";
    public const string Email = "email.db";
    public const string Production = "production.db";
    public const string Sales = "sales.db";
    public const string Purchasing = "purchasing.db";
    public const string Inventory = "inventory.db";
    public const string Users = "users.db";

    public static IReadOnlyList<string> All { get; } =
    [
        Clients, TimeReports, Addresses, EmailAddresses, PhoneNumbers, Cvs, Students, Subjects,
        Schedule, Grades, Email, Production, Sales, Purchasing, Inventory, Users,
    ];
}
