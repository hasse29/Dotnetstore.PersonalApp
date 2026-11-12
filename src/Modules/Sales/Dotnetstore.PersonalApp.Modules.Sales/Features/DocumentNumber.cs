using System.Globalization;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features;

/// <summary>
/// Numbers for offers, orders and invoices: a prefix, the date and a four-digit counter of the documents made that day, for example
/// OFF202611120001. The counter is one more than the highest used so far, so a deleted draft never makes a number repeat.
/// </summary>
internal static class DocumentNumber
{
    public const string Offer = "OFF";
    public const string Order = "ORD";
    public const string Invoice = "INV";

    /// <summary>The part every number of this kind made on <paramref name="date"/> starts with, for example OFF20261112.</summary>
    public static string Prefix(string kind, DateOnly date) =>
        string.Create(CultureInfo.InvariantCulture, $"{kind}{date:yyyyMMdd}");

    /// <summary>The number after the highest of <paramref name="used"/>; <paramref name="used"/> are the numbers that start with <paramref name="prefix"/>.</summary>
    public static string Next(string prefix, IEnumerable<string> used)
    {
        var highest = used
            .Select(n => int.TryParse(n.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : 0)
            .DefaultIfEmpty(0)
            .Max();

        return string.Create(CultureInfo.InvariantCulture, $"{prefix}{highest + 1:0000}");
    }

    /// <summary>The pick list number of an order: PL instead of ORD, for example PL202611120001.</summary>
    public static string PickList(string orderNumber) => "PL" + orderNumber[Order.Length..];
}
