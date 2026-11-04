namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>exchange rate.</summary>
internal sealed class CurrencyRate
{
    public Guid Id { get; set; }

    public DateOnly RateDate { get; set; }

    public Guid FromCurrencyId { get; set; }

    public Currency FromCurrency { get; set; } = null!;

    public Guid ToCurrencyId { get; set; }

    public Currency ToCurrency { get; set; } = null!;

    public decimal AverageRate { get; set; }

    public decimal EndOfDayRate { get; set; }
}