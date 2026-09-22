using CotizadorSanjCorp3D.Domain;

namespace CotizadorSanjCorp3D.Services;

public static class CalculationService
{
    public static QuoteCalculation Calculate(QuoteInput input, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(settings);
        if (input.PrintHours <= 0) throw new ArgumentOutOfRangeException(nameof(input), "El tiempo debe ser mayor que cero.");
        if (input.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(input), "La cantidad debe ser mayor que cero.");
        if (input.ProfitMultiplier < 1m) throw new ArgumentOutOfRangeException(nameof(input), "El multiplicador debe ser igual o mayor que 1.");
        if (input.Filaments.Count == 0 || input.Filaments.Sum(item => item.Grams) <= 0)
            throw new ArgumentException("Registre al menos un filamento con peso mayor que cero.", nameof(input));

        decimal pieces = input.Quantity;
        decimal weight = input.Filaments.Sum(item => item.Grams) * pieces;
        decimal material = input.Filaments.Sum(item => item.Cost) * pieces;
        decimal electricity = input.Printer.PowerWatts / 1000m * input.PrintHours * settings.ElectricityPerKwh * pieces;
        decimal machine = 0m;
        decimal maintenance = settings.MaintenancePerPrint * pieces;
        decimal labor = 0m;
        decimal additional = input.Materials.Sum(item => item.Cost) + input.AdditionalManualCost;
        decimal baseCost = material + electricity + machine + maintenance + labor + additional;
        decimal functional = 0m;
        decimal subtotal = baseCost;
        decimal profit = subtotal * (input.ProfitMultiplier - 1m);
        decimal tax = (subtotal + profit) * settings.TaxPercent / 100m;
        decimal recommended = RoundToIncrement(subtotal + profit + tax, settings.RoundTo);

        return new(Round(weight, settings), Round(material, settings), Round(electricity, settings),
            Round(machine, settings), Round(maintenance, settings), Round(labor, settings),
            Round(additional, settings), Round(functional, settings), Round(subtotal, settings),
            Round(profit, settings), Round(tax, settings), Round(recommended, settings));
    }

    private static decimal Round(decimal value, AppSettings settings) =>
        decimal.Round(value, Math.Clamp(settings.DecimalPlaces, 0, 4), MidpointRounding.AwayFromZero);

    private static decimal RoundToIncrement(decimal value, decimal increment) =>
        increment <= 0 ? value : decimal.Round(value / increment, 0, MidpointRounding.AwayFromZero) * increment;
}
