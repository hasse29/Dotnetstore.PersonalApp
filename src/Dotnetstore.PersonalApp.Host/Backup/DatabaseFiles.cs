namespace Dotnetstore.PersonalApp.Host.Backup;

/// <summary>The file name of every module database in the data directory. Backup and restore only ever touch these.</summary>
internal static class DatabaseFiles
{
    public const string Clients = "clients.db";
    public const string TimeReports = "timereports.db";
    public const string Products = "products.db";

    // Not sales.db: that name belonged to the old Sales module and is deleted at startup (see Retired).
    public const string Sales = "salesdocuments.db";

    public const string Addresses = "addresses.db";
    public const string EmailAddresses = "emailaddresses.db";
    public const string PhoneNumbers = "phonenumbers.db";
    public const string Cvs = "cvs.db";
    public const string Students = "students.db";
    public const string Subjects = "subjects.db";
    public const string Schedule = "schedule.db";
    public const string Grades = "grades.db";
    public const string Email = "email.db";
    public const string Users = "users.db";

    public static IReadOnlyList<string> All { get; } =
    [
        Clients, TimeReports, Products, Sales, Addresses, EmailAddresses, PhoneNumbers, Cvs, Students, Subjects,
        Schedule, Grades, Email, Users,
    ];

    /// <summary>Databases of modules that were removed from the app. They are no longer used, deleted at startup, and skipped when a backup that still has them is restored.</summary>
    public static IReadOnlyList<string> Retired { get; } = ["production.db", "purchasing.db", "sales.db", "inventory.db"];
}
