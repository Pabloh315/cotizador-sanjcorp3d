using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Identity;
using SanjCorp3D.Api.Models;

namespace SanjCorp3D.Api.Controllers;

[ApiController, Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Sales},{AppRoles.Inventory},{AppRoles.Maker}"), Route("api/retail")]
public sealed class RetailController(AppDbContext db) : ControllerBase
{
    public sealed record ProductRequest(string Category, string Brand, string Name, string? Sku, string? MaterialType, string? ColorName, string? ColorHex, string? Specifications, decimal UnitWeightGrams, decimal ProfitPercent, bool Active = true);
    public sealed record AcquisitionRequest(int Quantity, decimal UnitCost, string? Supplier);
    public sealed record SaleRequest(int Quantity, decimal ProfitPercent, string? Customer);
    public sealed record ColorOption(string Name, string Hex, string MaterialType, string Brand);

    [HttpGet("products")]
    public async Task<IActionResult> Products([FromQuery] string group, [FromQuery] bool includeArchived = false, CancellationToken ct = default)
    {
        var query = db.RetailProducts.AsNoTracking();
        query = group.Equals("Printer", StringComparison.OrdinalIgnoreCase) ? query.Where(x => x.Category == "Printer") : query.Where(x => x.Category == "Filament" || x.Category == "Resin");
        if (!includeArchived) query = query.Where(x => x.Active);
        return Ok(await query.OrderBy(x => x.Brand).ThenBy(x => x.Name).ThenBy(x => x.MaterialType).ThenBy(x => x.ColorName).Select(x => new
        {
            x.Id, x.Category, x.Brand, x.Name, x.Sku, x.MaterialType, x.ColorName, x.ColorHex, x.Specifications, x.UnitWeightGrams,
            x.UnitCost, x.ProfitPercent, SuggestedPrice = decimal.Round(x.UnitCost * (1m + x.ProfitPercent / 100m), 2), x.StockQuantity, x.Active, x.CreatedAtUtc
        }).ToListAsync(ct));
    }

    [HttpPost("products")]
    public async Task<IActionResult> Create(ProductRequest request, CancellationToken ct)
    {
        var item = Map(request); db.RetailProducts.Add(item); await db.SaveChangesAsync(ct); return Ok(Response(item));
    }

    [HttpPut("products/{id:long}")]
    public async Task<IActionResult> Update(long id, ProductRequest request, CancellationToken ct)
    {
        var item = await db.RetailProducts.FirstOrDefaultAsync(x => x.Id == id, ct); if (item is null) return NotFound();
        Apply(item, request); await db.SaveChangesAsync(ct); return Ok(Response(item));
    }

    [HttpPost("products/{id:long}/acquire")]
    public async Task<IActionResult> Acquire(long id, AcquisitionRequest request, CancellationToken ct)
    {
        if (request.Quantity <= 0 || request.UnitCost < 0) return BadRequest(new { message = "Cantidad y costo de adquisición no son válidos." });
        var item = await db.RetailProducts.FirstOrDefaultAsync(x => x.Id == id, ct); if (item is null) return NotFound();
        var totalUnits = item.StockQuantity + request.Quantity;
        item.UnitCost = totalUnits == 0 ? request.UnitCost : decimal.Round((item.StockQuantity * item.UnitCost + request.Quantity * request.UnitCost) / totalUnits, 4, MidpointRounding.AwayFromZero);
        item.StockQuantity = totalUnits;
        db.RetailAcquisitions.Add(new RetailAcquisition { ProductId = item.Id, Quantity = request.Quantity, UnitCost = request.UnitCost, Supplier = request.Supplier?.Trim() ?? string.Empty });
        await db.SaveChangesAsync(ct); return Ok(Response(item));
    }

    [HttpPost("products/{id:long}/sell")]
    public async Task<IActionResult> Sell(long id, SaleRequest request, CancellationToken ct)
    {
        if (request.Quantity <= 0 || request.ProfitPercent < 0) return BadRequest(new { message = "Cantidad y porcentaje de ganancia no son válidos." });
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            db.ChangeTracker.Clear(); await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var item = await db.RetailProducts.FirstOrDefaultAsync(x => x.Id == id && x.Active, ct); if (item is null) return NotFound();
            if (item.StockQuantity < request.Quantity) return Conflict(new { message = $"Stock insuficiente. Disponible: {item.StockQuantity}." });
            var unitPrice = decimal.Round(item.UnitCost * (1m + request.ProfitPercent / 100m), 2, MidpointRounding.AwayFromZero);
            var totalCost = decimal.Round(item.UnitCost * request.Quantity, 2); var totalPrice = decimal.Round(unitPrice * request.Quantity, 2);
            var sale = new RetailSale { ProductId = item.Id, Category = item.Category, ProductName = $"{item.Brand} {item.Name}".Trim(), Customer = request.Customer?.Trim() ?? string.Empty, Quantity = request.Quantity, UnitCost = item.UnitCost, ProfitPercent = request.ProfitPercent, UnitPrice = unitPrice, TotalCost = totalCost, TotalPrice = totalPrice, Profit = totalPrice - totalCost };
            item.StockQuantity -= request.Quantity; item.ProfitPercent = request.ProfitPercent; db.RetailSales.Add(sale);
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return Ok(new { sale.Id, sale.ProductId, sale.ProductName, sale.Quantity, sale.UnitPrice, sale.TotalPrice, sale.Profit, sale.SoldAtUtc, stockQuantity = item.StockQuantity });
        });
    }

    [HttpGet("sales")]
    public async Task<IActionResult> Sales([FromQuery] string group, CancellationToken ct)
    {
        var query = db.RetailSales.AsNoTracking(); query = group.Equals("Printer", StringComparison.OrdinalIgnoreCase) ? query.Where(x => x.Category == "Printer") : query.Where(x => x.Category == "Filament" || x.Category == "Resin");
        return Ok(await query.OrderByDescending(x => x.SoldAtUtc).Take(100).ToListAsync(ct));
    }

    [HttpGet("palette")]
    public async Task<IActionResult> Palette([FromQuery] string? materialType = null, [FromQuery] string? brand = null, CancellationToken ct = default)
    {
        var colors = new[] { ("Negro", "#151515"), ("Blanco", "#F4F4F0"), ("Gris", "#7D8285"), ("Rojo", "#D72638"), ("Azul", "#1769AA"), ("Azul cielo", "#55BDEB"), ("Verde", "#2E8B57"), ("Verde lima", "#8BC34A"), ("Amarillo", "#F4D03F"), ("Naranja", "#F57C00"), ("Rosa", "#EE7FA8"), ("Morado", "#7652A5"), ("Marrón", "#795548"), ("Beige", "#D9C6A5"), ("Dorado", "#C9A227"), ("Plateado", "#B7BDC3"), ("Transparente", "#DDEBF2") };
        var result = colors.Select(x => new ColorOption(x.Item1, x.Item2, materialType ?? "Todos", string.IsNullOrWhiteSpace(brand) ? "Comercial / personalizable" : brand.Trim())).ToList();
        var stored = db.RetailProducts.AsNoTracking().Where(x => x.Category != "Printer" && x.ColorName != "");
        if (!string.IsNullOrWhiteSpace(materialType)) stored = stored.Where(x => x.MaterialType.ToLower() == materialType.Trim().ToLower());
        if (!string.IsNullOrWhiteSpace(brand)) stored = stored.Where(x => x.Brand.ToLower() == brand.Trim().ToLower());
        foreach (var color in await stored.Select(x => new ColorOption(x.ColorName, x.ColorHex, x.MaterialType, x.Brand)).Distinct().ToListAsync(ct))
            if (!result.Any(x => x.Name.Equals(color.Name, StringComparison.OrdinalIgnoreCase) && x.Hex.Equals(color.Hex, StringComparison.OrdinalIgnoreCase))) result.Insert(0, color);
        return Ok(result);
    }

    private static RetailProduct Map(ProductRequest request) { var item = new RetailProduct(); Apply(item, request); return item; }
    private static void Apply(RetailProduct item, ProductRequest request)
    {
        if (request.Category != "Printer" && request.Category != "Filament" && request.Category != "Resin" || string.IsNullOrWhiteSpace(request.Brand) || string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Categoría, marca y nombre son obligatorios.");
        if (request.ProfitPercent < 0 || request.UnitWeightGrams < 0) throw new ArgumentException("Peso y porcentaje no pueden ser negativos.");
        item.Category = request.Category; item.Brand = request.Brand.Trim(); item.Name = request.Name.Trim(); item.Sku = request.Sku?.Trim() ?? string.Empty; item.MaterialType = request.MaterialType?.Trim() ?? string.Empty; item.ColorName = request.ColorName?.Trim() ?? string.Empty; item.ColorHex = string.IsNullOrWhiteSpace(request.ColorHex) ? "#808080" : request.ColorHex.Trim(); item.Specifications = request.Specifications?.Trim() ?? string.Empty; item.UnitWeightGrams = request.UnitWeightGrams; item.ProfitPercent = request.ProfitPercent; item.Active = request.Active;
    }
    private static object Response(RetailProduct x) => new { x.Id, x.Category, x.Brand, x.Name, x.Sku, x.MaterialType, x.ColorName, x.ColorHex, x.Specifications, x.UnitWeightGrams, x.UnitCost, x.ProfitPercent, SuggestedPrice = decimal.Round(x.UnitCost * (1m + x.ProfitPercent / 100m), 2), x.StockQuantity, x.Active, x.CreatedAtUtc };
}
