namespace CotizadorSanjCorp3D.Domain;

public sealed record Printer(long Id, string Name, decimal BuildX, decimal BuildY, decimal BuildZ,
    decimal Nozzle, decimal Speed, decimal PowerWatts, decimal HourlyCost, bool IsDefault, bool Active = true);

public sealed record Filament(long Id, string Name, string Category, string Material, string Color, decimal PricePerUnit,
    decimal Density, bool IsDefault, bool Active = true, int StockQuantity = 0)
{
    public string DisplayName => $"{Name} · {Material} · {Color} · {StockQuantity} disp.";
    public bool IsResin => Category.Equals("Resina", StringComparison.OrdinalIgnoreCase);
    public string PriceUnit => IsResin ? "litro" : "kg";
}

public sealed record ExtraMaterial(long Id, string Name, string Category, string Unit, decimal UnitPrice, bool Active = true);

public sealed record FilamentUsage(long FilamentId, string FilamentName, string Category, string Material,
    string Color, decimal Grams, decimal PricePerUnit, decimal Density)
{
    public decimal Cost => decimal.Round(
        Category.Equals("Resina", StringComparison.OrdinalIgnoreCase)
            ? Grams / Density / 1000m * PricePerUnit
            : Grams / 1000m * PricePerUnit,
        4, MidpointRounding.AwayFromZero);
}

public sealed record MaterialUsage(long MaterialId, string MaterialName, decimal Quantity, decimal UnitPrice)
{
    public decimal Cost => decimal.Round(Quantity * UnitPrice, 4, MidpointRounding.AwayFromZero);
}

public sealed record AppSettings(string BusinessName, string CurrencyName, string CurrencySymbol,
    decimal ElectricityPerKwh, decimal MaintenancePerPrint,
    decimal DefaultProfitMultiplier, decimal TaxPercent,
    decimal RoundTo, int DecimalPlaces)
{
    public static AppSettings Defaults => new("SANJ CORP 3D", "Bolivianos", "Bs", 1.10m, 2.00m,
        1.30m, 0.00m, 0.10m, 2);
}

public sealed record QuoteInput(string Customer, string ProjectName, Printer Printer, decimal PrintHours,
    int Quantity, decimal AdditionalManualCost, decimal ProfitMultiplier, string Notes, IReadOnlyList<FilamentUsage> Filaments,
    IReadOnlyList<MaterialUsage> Materials);

public sealed record QuoteCalculation(decimal TotalWeight, decimal MaterialCost, decimal ElectricityCost,
    decimal MachineCost, decimal MaintenanceCost, decimal LaborCost, decimal AdditionalCost,
    decimal FunctionalSurcharge, decimal Subtotal, decimal ProfitAmount, decimal TaxAmount,
    decimal RecommendedPrice);

public sealed record SavedQuote(long Id, string OrderCode, DateTime CreatedAt, QuoteInput Input,
    QuoteCalculation Calculation);

public sealed record QuoteSummary(long Id, string OrderCode, DateTime CreatedAt, string Customer,
    string ProjectName, string PrinterName, decimal TotalWeight, decimal CostTotal,
    decimal RecommendedPrice, DateTime? SoldAt)
{
    public bool IsSold => SoldAt.HasValue;
}

public sealed record QuoteDetails(long Id, string OrderCode, DateTime CreatedAt, string Customer,
    string ProjectName, string PrinterName, decimal PrintHours, int Quantity,
    decimal AdditionalManualCost, decimal ProfitMultiplier, string Notes,
    IReadOnlyList<FilamentUsage> Filaments, IReadOnlyList<MaterialUsage> Materials,
    QuoteCalculation Calculation, DateTime? SoldAt);

public sealed record SaleSummary(long Id, long QuoteId, string OrderCode, DateTime SoldAt,
    string Customer, string ProjectName, decimal CostTotal, decimal SaleAmount)
{
    public decimal Profit => SaleAmount - CostTotal;
}

public sealed record ReportSummary(int QuoteCount, decimal TotalCost, decimal ProjectedRevenue,
    decimal ProjectedProfit, int SaleCount, decimal SalesRevenue, decimal SalesProfit,
    IReadOnlyList<SaleSummary> Sales, IReadOnlyDictionary<string, decimal> FilamentDistribution,
    IReadOnlyDictionary<string, decimal> CostDistribution);
