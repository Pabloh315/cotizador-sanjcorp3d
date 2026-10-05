using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Contracts;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Identity;
using SanjCorp3D.Api.Models;
using SanjCorp3D.Api.Services;

namespace SanjCorp3D.Api.Controllers;

[ApiController, Authorize, Route("api/products")]
public sealed class ProductsController(AppDbContext db, BusinessSettingsService settings) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeArchived = false, CancellationToken ct = default)
    {
        var query = db.ProductCatalogs.AsNoTracking();
        if (!includeArchived) query = query.Where(x => x.Active);
        return Ok(await query.OrderBy(x => x.Name).Select(x => ToDto(x)).ToListAsync(ct));
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Maker},{AppRoles.SuperAdmin}"), HttpPost]
    public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken ct)
    {
        var validation = ValidateProduct(request, out var name);
        if (validation is not null) return validation;
        var item = new ProductCatalog { Name = name, Description = request.Description?.Trim() ?? string.Empty, MaterialType = request.MaterialType?.Trim() ?? string.Empty, FilamentGrams = request.FilamentGrams, MaterialCost = request.MaterialCost, ProductionMinutes = request.ProductionMinutes, ProfitMultiplier = request.ProfitMultiplier <= 0 ? 3m : request.ProfitMultiplier, Active = true };
        db.ProductCatalogs.Add(item);
        try { await db.SaveChangesAsync(ct); return Ok(ToDto(item)); }
        catch (DbUpdateException) { return Conflict(new { message = "Ese producto ya est registrado." }); }
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Maker},{AppRoles.SuperAdmin}"), HttpPatch("{id:long}")]
    public async Task<IActionResult> Update(long id, CreateProductRequest request, CancellationToken ct)
    {
        var item = await db.ProductCatalogs.FindAsync([id], ct);
        if (item is null) return NotFound();
        var validation = ValidateProduct(request, out var name);
        if (validation is not null) return validation;
        item.Name = name;
        item.Description = request.Description?.Trim() ?? string.Empty;
        item.MaterialType = request.MaterialType?.Trim() ?? string.Empty;
        item.FilamentGrams = request.FilamentGrams;
        item.MaterialCost = request.MaterialCost;
        item.ProductionMinutes = request.ProductionMinutes;
        item.ProfitMultiplier = request.ProfitMultiplier <= 0 ? 3m : request.ProfitMultiplier;
        item.Active = true;
        try { await db.SaveChangesAsync(ct); return Ok(ToDto(item)); }
        catch (DbUpdateException) { return Conflict(new { message = "Ese producto ya est registrado." }); }
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Maker},{AppRoles.SuperAdmin}"), HttpDelete("{id:long}")]
    public async Task<IActionResult> Archive(long id, CancellationToken ct)
    {
        var item = await db.ProductCatalogs.FindAsync([id], ct);
        if (item is null) return NotFound();
        item.Active = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Sales},{AppRoles.Maker},{AppRoles.SuperAdmin}"), HttpPost("store/calculate")]
    public async Task<IActionResult> CalculateStoreQuote(StoreQuoteRequest request, CancellationToken ct)
    {
        var (calculation, _, _) = await CalculateStore(request, ct);
        return Ok(calculation);
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Sales},{AppRoles.Maker},{AppRoles.SuperAdmin}"), HttpPost("store/quote")]
    public async Task<IActionResult> CreateStoreQuote(StoreQuoteRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerPhone)) return BadRequest(new { message = "El celular del cliente es obligatorio para guardar la cotizacion." });
        var (calculation, products, printers) = await CalculateStore(request, ct);
        var next = (await db.Quotes.MaxAsync(x => (long?)x.Id, ct) ?? 0) + 1;
        var created = DateTime.UtcNow;
        var productSummary = string.Join("; ", products.Select(x => $"{x.Product.Name} ({x.MaterialLabel}) x{x.Quantity}"));
        var printerSummary = string.Join(", ", printers.Select(x => x.Name));
        var noteParts = new List<string>
        {
            "Cotizacion de productos de tienda.",
            $"Materiales: {string.Join(", ", products.Select(x => x.MaterialLabel).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())}",
            $"Impresoras asignadas: {printerSummary}",
            $"PrinterIds: {string.Join(",", printers.Select(x => x.Id))}",
            $"Productos: {productSummary}"
        };
        if (!string.IsNullOrWhiteSpace(request.Notes)) noteParts.Add(request.Notes.Trim());
        var quote = new Quote
        {
            OrderCode = $"PT{created:yyyyMMdd}{next:0000}",
            CreatedAtUtc = created,
            Customer = request.Customer.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            ProjectName = string.IsNullOrWhiteSpace(request.ProjectName) ? "Productos de tienda" : request.ProjectName.Trim(),
            ProductName = productSummary,
            PrinterId = printers[0].Id,
            PrinterName = printerSummary,
            PrintHours = calculation.PrintHours,
            Quantity = (int)calculation.TotalQuantity,
            AdditionalManualCost = 0,
            ProfitMultiplier = products.Count == 0 ? 1 : decimal.Round(products.Sum(x => x.Product.ProfitMultiplier * x.Quantity) / Math.Max(1, products.Sum(x => x.Quantity)), 4, MidpointRounding.AwayFromZero),
            Notes = string.Join("\n", noteParts),
            TotalWeight = 0,
            MaterialCost = calculation.MaterialCost,
            ElectricityCost = calculation.ElectricityCost,
            MachineCost = 0,
            MaintenanceCost = calculation.MaintenanceCost,
            LaborCost = 0,
            AdditionalCost = 0,
            FunctionalSurcharge = 0,
            Subtotal = calculation.Subtotal,
            ProfitAmount = calculation.ProfitAmount,
            TaxAmount = calculation.TaxAmount,
            RecommendedPrice = calculation.RecommendedPrice
        };
        var quoteQuantity = Math.Max(1, quote.Quantity);
        foreach (var line in products.Where(x => x.Consumable is not null))
        {
            var consumable = line.Consumable!;
            var totalGrams = line.Product.FilamentGrams * line.Quantity;
            quote.Consumables.Add(new QuoteConsumable
            {
                LegacyConsumableId = consumable.Id,
                Name = consumable.Name,
                Category = consumable.Category,
                Material = consumable.Material,
                Color = consumable.Color,
                Grams = decimal.Round(totalGrams / quoteQuantity, 4, MidpointRounding.AwayFromZero),
                PricePerUnit = consumable.PricePerUnit,
                Density = consumable.Density,
                LineCost = decimal.Round(totalGrams / 1000m * consumable.PricePerUnit, 4, MidpointRounding.AwayFromZero)
            });
        }
        db.Quotes.Add(quote);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction("Get", "Quotes", new { id = quote.Id }, new { quote.Id, quote.OrderCode, quote.CreatedAtUtc, quote.Customer, quote.ProjectName, quote.ProductName, quote.PrinterName, quote.TotalWeight, CostTotal = quote.Subtotal, quote.RecommendedPrice, SoldAtUtc = (DateTime?)null });
    }

    private static decimal Percent(decimal amount, decimal percent) => amount * Math.Max(0, percent) / 100m;

    private async Task<(StoreQuoteCalculationDto Calculation, List<(ProductCatalog Product, int Quantity, string MaterialType, string MaterialLabel, decimal PricePerKg, Consumable? Consumable)> Products, List<Printer> Printers)> CalculateStore(StoreQuoteRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Customer)) throw new ArgumentException("El cliente es obligatorio.");
        if (request.Lines.Count == 0) throw new ArgumentException("Agrega al menos un producto de tienda.");
        if (request.Lines.Any(x => x.Quantity <= 0)) throw new ArgumentException("Las cantidades deben ser mayores que cero.");
        var printerIds = request.PrinterIds.Distinct().Take(3).ToArray();
        if (printerIds.Length is < 1 or > 3) throw new ArgumentException("Selecciona entre una y tres impresoras.");
        var printers = await db.Printers.Where(x => printerIds.Contains(x.Id) && x.Active).ToListAsync(ct);
        if (printers.Count != printerIds.Length) throw new ArgumentException("Una de las impresoras seleccionadas no existe o est archivada.");
        var busyPrinterNames = await db.PrintOrders.AsNoTracking()
            .Include(x => x.Printer)
            .Where(x => printerIds.Contains(x.PrinterId) && x.Status != "Terminado")
            .Select(x => x.Printer.Name)
            .Distinct()
            .ToListAsync(ct);
        if (busyPrinterNames.Count > 0) throw new ArgumentException($"Estas impresoras ya tienen un pedido asignado: {string.Join(", ", busyPrinterNames)}. Deben terminar y enfriarse antes de recibir otro pedido.");
        var productIds = request.Lines.Select(x => x.ProductId).Distinct().ToArray();
        var catalog = await db.ProductCatalogs.Where(x => productIds.Contains(x.Id) && x.Active).ToDictionaryAsync(x => x.Id, ct);
        var consumableIds = request.Lines.Where(x => x.ConsumableId.HasValue).Select(x => x.ConsumableId!.Value).Distinct().ToArray();
        var selectedConsumables = consumableIds.Length == 0 ? new Dictionary<long, Consumable>() : await db.Consumables.AsNoTracking().Where(x => consumableIds.Contains(x.Id) && x.Active).ToDictionaryAsync(x => x.Id, ct);
        var products = new List<(ProductCatalog Product, int Quantity, string MaterialType, string MaterialLabel, decimal PricePerKg, Consumable? Consumable)>();
        foreach (var line in request.Lines)
        {
            if (!catalog.TryGetValue(line.ProductId, out var product)) throw new ArgumentException("Uno de los productos seleccionados no existe o est archivado.");
            if (product.ProductionMinutes <= 0) throw new ArgumentException($"Configura el tiempo de produccin de {product.Name} antes de cotizarlo.");
            var materialType = string.IsNullOrWhiteSpace(line.MaterialType) ? product.MaterialType : line.MaterialType.Trim();
            var materialLabel = materialType;
            var pricePerKg = 0m;
            Consumable? selectedConsumable = null;
            if (line.ConsumableId.HasValue)
            {
                if (!selectedConsumables.TryGetValue(line.ConsumableId.Value, out var consumable)) throw new ArgumentException("El material/color seleccionado no existe o esta archivado.");
                materialType = consumable.Material;
                materialLabel = $"{consumable.Name} - {consumable.Material} - {consumable.Color}";
                pricePerKg = consumable.PricePerUnit;
                selectedConsumable = consumable;
            }
            products.Add((product, line.Quantity, materialType, materialLabel, pricePerKg, selectedConsumable));
        }
        var config = await settings.GetAsync(ct);
        var quantity = products.Sum(x => x.Quantity);
        var totalMinutes = products.Sum(x => x.Product.ProductionMinutes * x.Quantity);
        var printHours = totalMinutes / 60m;
        var averagePower = printers.Average(x => x.PowerWatts);
        var materialPrices = await db.Consumables.AsNoTracking()
            .Where(x => x.Active)
            .GroupBy(x => x.Material)
            .Select(x => new { Material = x.Key, Price = x.Average(item => item.PricePerUnit) })
            .ToDictionaryAsync(x => x.Material, x => x.Price, StringComparer.OrdinalIgnoreCase, ct);
        decimal MaterialUnitCost(ProductCatalog product, string materialType, decimal selectedPricePerKg)
        {
            var pricePerKg = selectedPricePerKg > 0 ? selectedPricePerKg : materialPrices.TryGetValue(materialType, out var price) ? price : 0m;
            var calculated = product.FilamentGrams / 1000m * pricePerKg;
            return calculated > 0 ? calculated : product.MaterialCost;
        }
        var material = products.Sum(x => MaterialUnitCost(x.Product, x.MaterialType, x.PricePerKg) * x.Quantity);
        var electricity = averagePower / 1000m * printHours * config.ElectricityPerKwh;
        var directBase = material + electricity;
        var maintenance = config.MaintenancePerPrint * quantity + Percent(directBase, config.MaintenancePercent);
        var preparation = Percent(directBase, config.PreparationPercent);
        var labor = Percent(directBase, config.LaborPercent);
        var waste = Percent(directBase, config.WastePercent);
        var overhead = Percent(directBase, config.OverheadPercent);
        var packaging = config.PackagingCost * quantity;
        var transport = config.TransportCost;
        var subtotal = material + electricity + maintenance + preparation + labor + waste + overhead + packaging + transport;
        var multiplied = products.Sum(x =>
        {
            var lineHours = x.Product.ProductionMinutes * x.Quantity / 60m;
            var lineMaterial = MaterialUnitCost(x.Product, x.MaterialType, x.PricePerKg) * x.Quantity;
            var lineElectricity = averagePower / 1000m * lineHours * config.ElectricityPerKwh;
            var lineBase = lineMaterial + lineElectricity;
            var lineCosts = lineBase + config.MaintenancePerPrint * x.Quantity + Percent(lineBase, config.MaintenancePercent) + Percent(lineBase, config.PreparationPercent) + Percent(lineBase, config.LaborPercent) + Percent(lineBase, config.WastePercent) + Percent(lineBase, config.OverheadPercent) + config.PackagingCost * x.Quantity;
            return lineCosts * x.Product.ProfitMultiplier;
        }) + transport;
        var profit = multiplied - subtotal;
        var tax = multiplied * config.TaxPercent / 100m;
        var recommended = config.RoundTo <= 0 ? multiplied + tax : decimal.Round((multiplied + tax) / config.RoundTo, 0, MidpointRounding.AwayFromZero) * config.RoundTo;
        decimal Round(decimal value) => decimal.Round(value, config.DecimalPlaces, MidpointRounding.AwayFromZero);
        return (new StoreQuoteCalculationDto(quantity, Round(printHours), Round(material), Round(electricity), Round(maintenance), Round(preparation), Round(labor), Round(waste), Round(overhead), Round(packaging), Round(transport), Round(subtotal), Round(profit), Round(tax), Round(recommended)), products, printers.OrderBy(x => Array.IndexOf(printerIds, x.Id)).ToList());
    }

    private BadRequestObjectResult? ValidateProduct(CreateProductRequest request, out string name)
    {
        name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "El nombre del producto es obligatorio." });
        if (request.MaterialCost < 0) return BadRequest(new { message = "El costo de produccin no puede ser negativo." });
        if (request.FilamentGrams < 0) return BadRequest(new { message = "Los gramos de filamento no pueden ser negativos." });
        if (request.ProductionMinutes < 0) return BadRequest(new { message = "El tiempo de produccin no puede ser negativo." });
        if (request.ProfitMultiplier < 1) return BadRequest(new { message = "El multiplicador debe ser igual o mayor que 1." });
        return null;
    }

    private static ProductCatalogDto ToDto(ProductCatalog item) => new(item.Id, item.Name, item.Description, item.MaterialType, item.FilamentGrams, item.MaterialCost, item.ProductionMinutes, item.ProfitMultiplier, item.Active);
}
