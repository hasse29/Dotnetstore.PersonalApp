namespace Dotnetstore.PersonalApp.Modules.TimeReports.Domain;

/// <summary>What an expense was for. Stored by name, so members may be added but not renamed.</summary>
internal enum ExpenseCategory
{
    Hotel,
    Travel,
    Meals,
    Parking,
    Fuel,
    Materials,
    Other,
}
