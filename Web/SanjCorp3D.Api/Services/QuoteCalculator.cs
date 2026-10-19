using SanjCorp3D.Api.Contracts;
using SanjCorp3D.Api.Models;

namespace SanjCorp3D.Api.Services;

public static class QuoteCalculator
{
    public sealed record ConsumableLine(Consumable Item, decimal Grams, decimal? CalculatedUnitCost = null)
    {
        public decimal UnitCost => CalculatedUnitCost ?? (Item.Category.Equals("Resina", StringComparison.OrdinalIgnoreCase)
            ? Grams / Item.Density / 1000m * Item.PricePerUnit
            : Grams / 1000m * Item.PricePerUnit);
    }

    public sealed record MaterialLine(ExtraMaterial Item, decimal Quantity) { public decimal Cost => Quantity * Item.UnitPrice; }

    public static QuoteCalculationDto Calculate(QuoteRequest request, Printer printer, IReadOnlyList<ConsumableLine> consumables, IReadOnlyList<MaterialLine> materials, BusinessSettingsDto settings)
    {
        if (string.IsNullOrWhiteSpace(request.Customer) || string.IsNullOrWhiteSpace(request.ProjectName)) throw new ArgumentException("Cliente y proyecto son obligatorios.");
        if (request.PrintHours <= 0 || request.Quantity <= 0) throw new ArgumentException("Tiempo y cantidad deben ser mayores que cero.");
        if (request.ProfitMultiplier < 1m) throw new ArgumentException("El multiplicador debe ser igual o mayor a 1.");
        if (request.AdditionalManualCost < 0) throw new ArgumentException("El costo adicional no puede ser negativo.");
        if (consumables.Count == 0 || consumables.Sum(x => x.Grams) <= 0) throw new ArgumentException("Agrega al menos un consumible con peso mayor que cero.");
        if (consumables.Any(x => x.Grams <= 0)) throw new ArgumentException("Los pesos deben ser mayores que cero.");
        if (materials.Any(x => x.Quantity <= 0)) throw new ArgumentException("Las cantidades de materiales deben ser mayores que cero.");

        decimal pieces = request.Quantity;
        decimal weight = consumables.Sum(x => x.Grams) * pieces;
        decimal material = consumables.Sum(x => x.UnitCost) * pieces;

        // The source spreadsheet works with whole minutes:
        // kWh = (power in W / 1000) * (print minutes / 60).
        // The API keeps the public value in hours for compatibility, so convert
        // it back to minutes before applying the energy formula.
        decimal printMinutes = decimal.Round(request.PrintHours * 60m, 0, MidpointRounding.AwayFromZero);
        decimal energyKwh = printer.PowerWatts / 1000m * (printMinutes / 60m);
        decimal electricity = energyKwh * settings.ElectricityPerKwh * pieces;
        decimal directMaterials = materials.Sum(x => x.Cost) + request.AdditionalManualCost;
        // Production is priced from its direct replacement cost. Administrative
        // expenses, salaries and failures are covered by the x3 multiplier.
        decimal maintenance = Math.Clamp(settings.MaintenancePerPrint, 1m, 3m);
        decimal preparation = 0m;
        decimal labor = 0m;
        decimal waste = 0m;
        decimal overhead = 0m;
        decimal packaging = Math.Max(0, request.PackagingCost ?? settings.PackagingCost) * pieces;
        decimal transport = Math.Max(0, request.TransportCost ?? settings.TransportCost);
        decimal additional = directMaterials + packaging + transport;

        decimal productionCost = material + electricity + directMaterials + maintenance;
        decimal subtotal = productionCost + packaging + transport;
        decimal multipliedTotal = productionCost * request.ProfitMultiplier;
        decimal recommended = decimal.Ceiling(multipliedTotal + packaging + transport);
        decimal profit = recommended - subtotal;
        decimal tax = 0m;
        decimal Round(decimal value) => decimal.Round(value, settings.DecimalPlaces, MidpointRounding.AwayFromZero);
        return new(Round(weight), Round(material), Round(electricity), Round(maintenance), Round(preparation), Round(labor), Round(waste), Round(overhead), Round(packaging), Round(transport), Round(additional), Round(subtotal), Round(profit), Round(tax), Round(recommended));
    }
}
