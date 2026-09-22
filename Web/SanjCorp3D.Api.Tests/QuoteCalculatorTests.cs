using SanjCorp3D.Api.Contracts;
using SanjCorp3D.Api.Models;
using SanjCorp3D.Api.Services;

namespace SanjCorp3D.Api.Tests;

public sealed class QuoteCalculatorTests
{
    [Fact]
    public void Calculates_filament_electricity_maintenance_materials_profit_tax_and_rounding()
    {
        var printer = Printer(powerWatts: 500m);
        var consumable = Consumable("Filamento", 200m, 1.24m);
        var material = new ExtraMaterial { Id = 1, Name = "Imán", Category = "Herraje", Unit = "unidad", UnitPrice = 3m };
        var request = new QuoteRequest("Cliente", "Proyecto", 1, 2m, 3, 5m, 1.5m, "",
            [new ConsumableUsageRequest(1, 100m)], [new MaterialUsageRequest(1, 2m)]);
        var settings = new BusinessSettingsDto("SANJ", "Bolivianos", "Bs", 1.2m, 2m, 1.3m, 10m, .1m, 2);

        var result = QuoteCalculator.Calculate(request, printer,
            [new QuoteCalculator.ConsumableLine(consumable, 100m)],
            [new QuoteCalculator.MaterialLine(material, 2m)], settings);

        Assert.Equal(300m, result.TotalWeight);
        Assert.Equal(60m, result.MaterialCost);
        Assert.Equal(3.6m, result.ElectricityCost);
        Assert.Equal(6m, result.MaintenanceCost);
        Assert.Equal(11m, result.AdditionalCost);
        Assert.Equal(80.6m, result.Subtotal);
        Assert.Equal(40.3m, result.ProfitAmount);
        Assert.Equal(12.09m, result.TaxAmount);
        Assert.Equal(133m, result.RecommendedPrice);
    }

    [Fact]
    public void Converts_resin_grams_to_liters_using_density()
    {
        var request = new QuoteRequest("Cliente", "Figura", 1, 1m, 2, 0m, 1m, "",
            [new ConsumableUsageRequest(1, 120m)], []);
        var resin = Consumable("Resina", 300m, 1.2m);
        var settings = new BusinessSettingsDto("SANJ", "Bolivianos", "Bs", 0m, 0m, 1m, 0m, .01m, 2);

        var result = QuoteCalculator.Calculate(request, Printer(0m),
            [new QuoteCalculator.ConsumableLine(resin, 120m)], [], settings);

        Assert.Equal(240m, result.TotalWeight);
        Assert.Equal(60m, result.MaterialCost);
        Assert.Equal(60m, result.RecommendedPrice);
    }

    [Fact]
    public void Matches_spreadsheet_formula_when_time_is_entered_as_hours_and_minutes()
    {
        // The spreadsheet example uses 1,400 minutes (23 h 20 min), 400 W,
        // 1.10 Bs/kWh, 900 g at 155 Bs/kg, no maintenance and +50%.
        var request = new QuoteRequest("Cliente", "Pieza", 1, 23m + 20m / 60m, 1, 0m, 1.5m, "",
            [new ConsumableUsageRequest(1, 900m)], []);
        var settings = new BusinessSettingsDto("SANJ", "Bolivianos", "Bs", 1.10m, 0m, 1.3m, 0m, 0m, 2);

        var result = QuoteCalculator.Calculate(request, Printer(400m),
            [new QuoteCalculator.ConsumableLine(Consumable("Filamento", 155m, 1.24m), 900m)], [], settings);

        Assert.Equal(139.50m, result.MaterialCost);
        Assert.Equal(10.27m, result.ElectricityCost);
        Assert.Equal(149.77m, result.Subtotal);
        Assert.Equal(74.88m, result.ProfitAmount);
        Assert.Equal(224.65m, result.RecommendedPrice);
    }

    [Fact]
    public void Rejects_quote_without_positive_consumable_weight()
    {
        var request = new QuoteRequest("Cliente", "Proyecto", 1, 1m, 1, 0m, 1.3m, "", [], []);
        Assert.Throws<ArgumentException>(() => QuoteCalculator.Calculate(request, Printer(300m), [], [], BusinessSettingsService.Defaults));
    }

    private static Printer Printer(decimal powerWatts) => new()
    {
        Id = 1, Name = "Prueba", BuildX = 200, BuildY = 200, BuildZ = 200,
        Nozzle = .4m, Speed = 60, PowerWatts = powerWatts
    };

    private static Consumable Consumable(string category, decimal price, decimal density) => new()
    {
        Id = 1, Name = "Prueba", Category = category, Material = "Material", Color = "Negro",
        PricePerUnit = price, Density = density, StockQuantity = 1
    };
}
