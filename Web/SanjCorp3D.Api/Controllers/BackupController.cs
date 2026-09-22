using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Contracts;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Identity;
using SanjCorp3D.Api.Models;
using SanjCorp3D.Api.Services;

namespace SanjCorp3D.Api.Controllers;

[ApiController, Authorize(Roles = AppRoles.SuperAdmin), Route("api/backup")]
public sealed class BackupController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var printers = await db.Printers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var consumables = await db.Consumables.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var materials = await db.Materials.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var quotes = await db.Quotes.AsNoTracking().AsSplitQuery()
            .Include(x => x.Consumables).Include(x => x.Materials).Include(x => x.Sale)
            .OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var saleIds = quotes.Where(x => x.Sale is not null).Select(x => x.Sale!.Id).ToArray();
        var saleConsumption = await db.SaleConsumables.AsNoTracking()
            .Where(x => saleIds.Contains(x.SaleId)).ToListAsync(cancellationToken);
        var consumptionBySale = saleConsumption.GroupBy(x => x.SaleId)
            .ToDictionary(x => x.Key, x => x.Select(v => new BackupSaleConsumable(v.ConsumableId, v.Grams)).ToList());
        var settings = await db.BusinessSettings.AsNoTracking().ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);

        var backup = new BackupDocument(
            1,
            DateTime.UtcNow,
            printers.Select(x => new BackupPrinter(x.Name, x.BuildX, x.BuildY, x.BuildZ, x.Nozzle, x.Speed, x.PowerWatts, x.HourlyCost, x.IsDefault, x.Active, x.Status, x.ColorCount)).ToList(),
            consumables.Select(x => new BackupConsumable(x.Name, x.Category, x.Material, x.Color, x.PricePerUnit, x.Density, x.IsDefault, x.Active, x.StockQuantity, x.StockGrams, x.LowStockGrams, x.Id)).ToList(),
            materials.Select(x => new BackupMaterial(x.Name, x.Category, x.Unit, x.UnitPrice, x.Active)).ToList(),
            quotes.Select(x => new BackupQuote(
                x.OrderCode, x.CreatedAtUtc, x.Customer, x.ProjectName, x.PrinterName, x.PrintHours, x.Quantity,
                x.AdditionalManualCost, x.ProfitMultiplier, x.Notes, x.TotalWeight, x.MaterialCost, x.ElectricityCost,
                x.MachineCost, x.MaintenanceCost, x.LaborCost, x.AdditionalCost, x.FunctionalSurcharge, x.Subtotal,
                x.ProfitAmount, x.TaxAmount, x.RecommendedPrice,
                x.Consumables.Select(v => new BackupQuoteConsumable(v.LegacyConsumableId, v.Name, v.Category, v.Material, v.Color, v.Grams, v.PricePerUnit, v.Density, v.LineCost)).ToList(),
                x.Materials.Select(v => new BackupQuoteMaterial(v.LegacyMaterialId, v.Name, v.Quantity, v.UnitPrice, v.LineCost)).ToList(),
                x.Sale is null ? null : new BackupSale(x.Sale.SoldAtUtc, x.Sale.SaleAmount,
                    consumptionBySale.TryGetValue(x.Sale.Id, out var lines) ? lines : [])
                , x.CustomerPhone
            )).ToList(),
            settings);

        return File(
            JsonSerializer.SerializeToUtf8Bytes(backup, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }),
            "application/json",
            $"sanjcorp3d-{DateTime.UtcNow:yyyyMMdd-HHmm}.json");
    }

    [HttpPost("restore")]
    public async Task<IActionResult> Restore(RestoreBackupRequest request, CancellationToken cancellationToken)
    {
        if (request.Confirmation != "RESTAURAR") return BadRequest(new { message = "Escribe RESTAURAR para confirmar." });
        if (request.Backup.Version != 1) return BadRequest(new { message = "La versión del respaldo no es compatible." });
        if (request.Backup.Printers is null || request.Backup.Consumables is null || request.Backup.Materials is null || request.Backup.Quotes is null || request.Backup.Settings is null)
            return BadRequest(new { message = "El archivo de respaldo está incompleto." });

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await db.SaleConsumables.ExecuteDeleteAsync(cancellationToken);
            await db.ConsumableStockLots.ExecuteDeleteAsync(cancellationToken);
            await db.ChatMessages.ExecuteDeleteAsync(cancellationToken);
            await db.Sales.ExecuteDeleteAsync(cancellationToken);
            await db.QuoteConsumables.ExecuteDeleteAsync(cancellationToken);
            await db.QuoteMaterials.ExecuteDeleteAsync(cancellationToken);
            await db.Quotes.ExecuteDeleteAsync(cancellationToken);
            await db.Consumables.ExecuteDeleteAsync(cancellationToken);
            await db.Materials.ExecuteDeleteAsync(cancellationToken);
            await db.BusinessSettings.ExecuteDeleteAsync(cancellationToken);

            var consumableEntities = request.Backup.Consumables.Select(x => new Consumable
            {
                Id = x.Id > 0 ? x.Id : 0,
                Name = x.Name,
                Category = x.Category,
                Material = x.Material,
                Color = x.Color,
                PricePerUnit = x.PricePerUnit,
                Density = x.Density,
                IsDefault = x.IsDefault,
                Active = x.Active,
                StockQuantity = x.StockQuantity,
                StockGrams = x.StockGrams > 0 || x.StockQuantity == 0 ? x.StockGrams : x.StockQuantity * 1000m,
                LowStockGrams = x.LowStockGrams
            }).ToList();
            foreach (var item in consumableEntities) SyncLegacyQuantity(item);

            var favoriteNames = request.Backup.Printers.Where(x => x.IsDefault)
                .Select(x => x.Name.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var printer in await db.Printers.ToListAsync(cancellationToken))
                { var backupPrinter = request.Backup.Printers.FirstOrDefault(x => string.Equals(x.Name.Trim(), printer.Name, StringComparison.OrdinalIgnoreCase)); printer.IsDefault = favoriteNames.Contains(printer.Name); if (backupPrinter is not null) printer.Status = string.IsNullOrWhiteSpace(backupPrinter.Status) ? "Disponible" : backupPrinter.Status; }
            db.Consumables.AddRange(consumableEntities);
            db.ConsumableStockLots.AddRange(consumableEntities.Where(x => x.StockGrams > 0).Select(x => new ConsumableStockLot { Consumable = x, OriginalGrams = x.StockGrams, RemainingGrams = x.StockGrams, PricePerKilogram = x.PricePerUnit }));
            db.Materials.AddRange(request.Backup.Materials.Select(x => new ExtraMaterial { Name = x.Name, Category = x.Category, Unit = x.Unit, UnitPrice = x.UnitPrice, Active = x.Active }));
            db.BusinessSettings.AddRange(request.Backup.Settings.Select(x => new BusinessSetting { Key = x.Key, Value = x.Value }));

            var knownConsumableIds = consumableEntities.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();
            foreach (var source in request.Backup.Quotes)
            {
                var quote = new Quote
                {
                    OrderCode = source.OrderCode, CreatedAtUtc = source.CreatedAtUtc, Customer = source.Customer, CustomerPhone = source.CustomerPhone ?? string.Empty,
                    ProjectName = source.ProjectName, PrinterName = source.PrinterName, PrintHours = source.PrintHours,
                    Quantity = source.Quantity, AdditionalManualCost = source.AdditionalManualCost, ProfitMultiplier = source.ProfitMultiplier,
                    Notes = source.Notes, TotalWeight = source.TotalWeight, MaterialCost = source.MaterialCost,
                    ElectricityCost = source.ElectricityCost, MachineCost = source.MachineCost, MaintenanceCost = source.MaintenanceCost,
                    LaborCost = source.LaborCost, AdditionalCost = source.AdditionalCost, FunctionalSurcharge = source.FunctionalSurcharge,
                    Subtotal = source.Subtotal, ProfitAmount = source.ProfitAmount, TaxAmount = source.TaxAmount,
                    RecommendedPrice = source.RecommendedPrice
                };
                quote.Consumables.AddRange(source.Consumables.Select(x => new QuoteConsumable
                {
                    LegacyConsumableId = x.LegacyConsumableId, Name = x.Name, Category = x.Category, Material = x.Material,
                    Color = x.Color, Grams = x.Grams, PricePerUnit = x.PricePerUnit, Density = x.Density, LineCost = x.LineCost
                }));
                quote.Materials.AddRange(source.Materials.Select(x => new QuoteMaterial
                {
                    LegacyMaterialId = x.LegacyMaterialId, Name = x.Name, Quantity = x.Quantity, UnitPrice = x.UnitPrice, LineCost = x.LineCost
                }));
                if (source.Sale is not null)
                {
                    quote.Sale = new Sale { SoldAtUtc = source.Sale.SoldAtUtc, SaleAmount = source.Sale.SaleAmount };
                    foreach (var line in source.Sale.Consumables ?? [])
                        if (knownConsumableIds.Contains(line.ConsumableId))
                            quote.Sale.Consumables.Add(new SaleConsumable { ConsumableId = line.ConsumableId, Grams = line.Grams });
                }
                db.Quotes.Add(quote);
            }

            await db.SaveChangesAsync(cancellationToken);
            await DatabaseSequenceService.AlignBusinessSequencesAsync(db, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(new { message = "Respaldo restaurado correctamente.", quotes = request.Backup.Quotes.Count });
        });
    }

    private static void SyncLegacyQuantity(Consumable item)
    {
        item.StockQuantity = item.StockGrams >= int.MaxValue * 1000m
            ? int.MaxValue
            : (int)decimal.Floor(item.StockGrams / 1000m);
    }
}


