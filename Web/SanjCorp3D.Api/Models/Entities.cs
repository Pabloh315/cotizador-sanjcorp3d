namespace SanjCorp3D.Api.Models;

public interface IActiveEntity { bool Active { get; set; } }
public interface ITenantEntity { Guid TenantId { get; set; } }

public sealed class Tenant
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Kind { get; set; }
    public string? LogoUrl { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Printer : IActiveEntity, ITenantEntity
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CatalogId { get; set; }
    public required string Name { get; set; }
    public decimal BuildX { get; set; }
    public decimal BuildY { get; set; }
    public decimal BuildZ { get; set; }
    public decimal Nozzle { get; set; }
    public decimal Speed { get; set; }
    public decimal PowerWatts { get; set; }
    public decimal HourlyCost { get; set; }
    public int ColorCount { get; set; } = 1;
    public bool IsDefault { get; set; }
    public string Status { get; set; } = "Disponible";
    public bool Active { get; set; } = true;
}

public sealed class Consumable : IActiveEntity, ITenantEntity
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public required string Material { get; set; }
    public required string Color { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal Density { get; set; }
    public bool IsDefault { get; set; }
    public bool Active { get; set; } = true;
    // Kept for compatibility with older clients/backups. New code uses StockGrams.
    public int StockQuantity { get; set; }
    public decimal StockGrams { get; set; }
    public decimal LowStockGrams { get; set; } = 1000m;
    [System.Text.Json.Serialization.JsonIgnore]
    public List<ConsumableStockLot> StockLots { get; set; } = [];
}

public sealed class ConsumableStockLot : ITenantEntity
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public long ConsumableId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Consumable Consumable { get; set; } = null!;
    public decimal OriginalGrams { get; set; }
    public decimal RemainingGrams { get; set; }
    public decimal PricePerKilogram { get; set; }
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class InventoryLoss : ITenantEntity
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public long ConsumableId { get; set; }
    public Consumable Consumable { get; set; } = null!;
    public decimal Grams { get; set; }
    public required string Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ProductCatalog : ITenantEntity
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public decimal FilamentGrams { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal AdditionalMaterialCost { get; set; }
    public decimal ProductionMinutes { get; set; }
    public decimal MaintenancePercent { get; set; } = 6m;
    public decimal PreparationPercent { get; set; } = 10m;
    public decimal LaborPercent { get; set; } = 20m;
    public decimal WastePercent { get; set; } = 7m;
    public decimal OverheadPercent { get; set; } = 5m;
    public decimal PackagingCost { get; set; }
    public decimal ProfitMultiplier { get; set; } = 3m;
    public bool Active { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}


public sealed class ConsumableMaterialType : IActiveEntity, ITenantEntity
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class ExtraMaterialCategory : IActiveEntity, ITenantEntity
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public bool Active { get; set; } = true;
}
public sealed class ExtraMaterial : IActiveEntity, ITenantEntity
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public required string Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class Quote : ITenantEntity
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public required string OrderCode { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public required string Customer { get; set; }
    public required string CustomerPhone { get; set; }
    public required string ProjectName { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public long? PrinterId { get; set; }
    public required string PrinterName { get; set; }
    public decimal PrintHours { get; set; }
    public int Quantity { get; set; }
    public decimal AdditionalManualCost { get; set; }
    public decimal ProfitMultiplier { get; set; }
    public string Notes { get; set; } = string.Empty;
    public decimal TotalWeight { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal ElectricityCost { get; set; }
    public decimal MachineCost { get; set; }
    public decimal MaintenanceCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal AdditionalCost { get; set; }
    public decimal FunctionalSurcharge { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal RecommendedPrice { get; set; }
    public List<QuoteConsumable> Consumables { get; set; } = [];
    public List<QuoteMaterial> Materials { get; set; } = [];
    public Sale? Sale { get; set; }
}

public sealed class QuoteConsumable : ITenantEntity
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public long QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;
    public long LegacyConsumableId { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public required string Material { get; set; }
    public required string Color { get; set; }
    public decimal Grams { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal Density { get; set; }
    public decimal LineCost { get; set; }
}

public sealed class QuoteMaterial : ITenantEntity
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public long QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;
    public long LegacyMaterialId { get; set; }
    public required string Name { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineCost { get; set; }
}

public sealed class Sale : ITenantEntity
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public long QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;
    public DateTime SoldAtUtc { get; set; }
    public decimal SaleAmount { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public List<SaleConsumable> Consumables { get; set; } = [];
}

public sealed class SaleConsumable : ITenantEntity
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public long SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public long ConsumableId { get; set; }
    public Consumable Consumable { get; set; } = null!;
    public decimal Grams { get; set; }
    public decimal PricePerKilogram { get; set; }
}


public sealed class PrintOrder : ITenantEntity
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public long QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;
    public long PrinterId { get; set; }
    public Printer Printer { get; set; } = null!;
    public string Status { get; set; } = "Pendiente";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? EstimatedFinishedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public DateTime? CoolingUntilUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
public sealed class BusinessSetting : ITenantEntity
{
    public Guid TenantId { get; set; }
    public required string Key { get; set; }
    public required string Value { get; set; }
}

public sealed class ChatMessage : ITenantEntity
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SenderUserId { get; set; }
    public required string SenderName { get; set; }
    public required string Body { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
