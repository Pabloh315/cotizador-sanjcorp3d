namespace SanjCorp3D.Api.Contracts;

public sealed record BusinessSettingsDto(
    string BusinessName, string CurrencyName, string CurrencySymbol,
    decimal ElectricityPerKwh, decimal MaintenancePerPrint,
    decimal MaintenancePercent, decimal PreparationPercent, decimal LaborPercent,
    decimal WastePercent, decimal OverheadPercent, decimal PackagingCost, decimal TransportCost,
    decimal DefaultProfitMultiplier, decimal TaxPercent,
    decimal RoundTo, int DecimalPlaces);

public sealed record ConsumableUsageRequest(long ConsumableId, decimal Grams);
public sealed record MaterialUsageRequest(long MaterialId, decimal Quantity);

public sealed record InventoryAlertDto(
    long Id, string Name, string Category, string Material, string Color,
    decimal StockGrams, decimal LowStockGrams, string Severity, string Message);

public sealed record QuoteRequest(
    string Customer, string ProjectName, long PrinterId, decimal PrintHours,
    int Quantity, decimal AdditionalManualCost, decimal ProfitMultiplier,
    string Notes, IReadOnlyList<ConsumableUsageRequest> Consumables,
    IReadOnlyList<MaterialUsageRequest> Materials, string? CustomerPhone = null, string? ProductName = null,
    decimal? MaintenancePercent = null, decimal? PreparationPercent = null, decimal? LaborPercent = null,
    decimal? WastePercent = null, decimal? OverheadPercent = null, decimal? PackagingCost = null, decimal? TransportCost = null);

public sealed record QuoteCalculationDto(
    decimal TotalWeight, decimal MaterialCost, decimal ElectricityCost,
    decimal MaintenanceCost, decimal PreparationCost, decimal LaborCost,
    decimal WasteCost, decimal OverheadCost, decimal PackagingCost, decimal TransportCost,
    decimal AdditionalCost, decimal Subtotal,
    decimal ProfitAmount, decimal TaxAmount, decimal RecommendedPrice);

public sealed record CreateUserRequest(string Username, string DisplayName, string? Email, string Password, string Role, string? ProfilePhotoUrl = null);
public sealed record UpdateUserRequest(string DisplayName, string? Email, string Role, bool Active, string? ProfilePhotoUrl = null);
public sealed record TenantDto(Guid Id, string Name, string Slug, string Kind, string? LogoUrl, bool Active, DateTime CreatedAtUtc, int UserCount);
public sealed record CreateMakerTenantRequest(string Name, string Slug, string? LogoUrl, string Username, string DisplayName, string? Email, string Password);
public sealed record UpdateTenantRequest(string Name, string? LogoUrl, bool Active);
public sealed record CreateMakerUserRequest(string Username, string DisplayName, string? Email, string Password);
public sealed record ProductCatalogDto(long Id, string Name, string Description, string MaterialType, decimal FilamentGrams, decimal MaterialCost, decimal ProductionMinutes, decimal MaintenancePercent, decimal PreparationPercent, decimal LaborPercent, decimal WastePercent, decimal OverheadPercent, decimal PackagingCost, decimal ProfitMultiplier, bool Active);
public sealed record CreateProductRequest(string Name, string? Description = null, string? MaterialType = null, decimal FilamentGrams = 0, decimal MaterialCost = 0, decimal ProductionMinutes = 0, decimal MaintenancePercent = 6m, decimal PreparationPercent = 10m, decimal LaborPercent = 20m, decimal WastePercent = 7m, decimal OverheadPercent = 5m, decimal PackagingCost = 0, decimal ProfitMultiplier = 1.4m);
public sealed record StoreQuoteLineRequest(long ProductId, int Quantity, string? MaterialType = null, long? ConsumableId = null);
public sealed record StoreQuoteRequest(string Customer, string? CustomerPhone, string? ProjectName, IReadOnlyList<long> PrinterIds, string? Notes, IReadOnlyList<StoreQuoteLineRequest> Lines, decimal? TransportCost = null);
public sealed record StoreQuoteCalculationDto(decimal TotalQuantity, decimal PrintHours, decimal MaterialCost, decimal ElectricityCost, decimal MaintenanceCost, decimal PreparationCost, decimal LaborCost, decimal WasteCost, decimal OverheadCost, decimal PackagingCost, decimal TransportCost, decimal Subtotal, decimal ProfitAmount, decimal TaxAmount, decimal RecommendedPrice);
public sealed record InventoryLossRequest(decimal Grams, string Reason);

public sealed record ConsumableMaterialTypeDto(long Id, string Name, bool Active);
public sealed record CreateConsumableMaterialTypeRequest(string Name);

public sealed record ExtraMaterialCategoryDto(long Id, string Name, bool Active);
public sealed record CreateExtraMaterialCategoryRequest(string Name);

public sealed record PrintOrderDto(
    long Id, long QuoteId, string OrderCode, string Customer, string ProjectName, string ProductName,
    long PrinterId, string PrinterName, string PrinterStatus, string Status,
    DateTime CreatedAtUtc, DateTime? StartedAtUtc, DateTime? EstimatedFinishedAtUtc,
    DateTime? FinishedAtUtc, DateTime? CoolingUntilUtc, DateTime? CompletedAtUtc,
    decimal PrintHours, int Quantity, decimal RecommendedPrice);
