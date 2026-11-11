namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

/// <summary>The amounts of one line that decide what it costs, whether it comes from the database or from the editor.</summary>
internal readonly record struct LineAmounts(decimal Quantity, decimal UnitPrice, decimal VatRate, decimal DiscountPercent);

/// <summary>What an offer adds up to. VAT is rounded per line, as it is on an invoice.</summary>
internal readonly record struct OfferTotal(decimal Net, decimal Vat)
{
    public decimal Gross => Net + Vat;
}

internal static class OfferTotals
{
    public static decimal LineNet(LineAmounts line) =>
        Math.Round(line.Quantity * line.UnitPrice * (1 - (line.DiscountPercent / 100m)), 2, MidpointRounding.AwayFromZero);

    public static decimal LineVat(LineAmounts line) =>
        Math.Round(LineNet(line) * line.VatRate / 100m, 2, MidpointRounding.AwayFromZero);

    public static OfferTotal Compute(IEnumerable<LineAmounts> lines)
    {
        decimal net = 0;
        decimal vat = 0;
        foreach (var line in lines)
        {
            net += LineNet(line);
            vat += LineVat(line);
        }

        return new OfferTotal(net, vat);
    }
}
