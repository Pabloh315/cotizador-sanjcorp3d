using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Models;
using SanjCorp3D.Api.Services;

var arguments = args.ToList();
string source = Value("--source") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SanjCorp3D", "cotizador.db");
string? destination = Value("--connection") ?? Environment.GetEnvironmentVariable("SANJCORP_POSTGRES");
bool apply = arguments.Contains("--apply", StringComparer.OrdinalIgnoreCase);
if (!File.Exists(source)) return Fail($"No se encontró SQLite: {source}");

var sourceConnection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = source, Mode = SqliteOpenMode.ReadOnly }.ToString());
await sourceConnection.OpenAsync();
await using (var integrity = sourceConnection.CreateCommand())
{
    integrity.CommandText = "PRAGMA integrity_check";
    if (!string.Equals(Convert.ToString(await integrity.ExecuteScalarAsync()), "ok", StringComparison.OrdinalIgnoreCase)) return Fail("La base SQLite no superó PRAGMA integrity_check.");
}

string[] tables = ["settings", "printers", "filaments", "materials", "quotes", "quote_filaments", "quote_materials", "sales"];
var sourceCounts = new Dictionary<string, long>();
foreach (string table in tables) sourceCounts[table] = await CountSqlite(table);
Console.WriteLine("Base SQLite válida (solo lectura):");
foreach (var item in sourceCounts) Console.WriteLine($"  {item.Key,-20} {item.Value,6}");
if (!apply) { Console.WriteLine("\nValidación terminada. Usa --apply y SANJCORP_POSTGRES para migrar."); return 0; }
if (string.IsNullOrWhiteSpace(destination)) return Fail("Falta SANJCORP_POSTGRES o --connection.");

var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(destination).Options;
var tenantContext = new TenantContext();
tenantContext.Use(TenantContext.TechnologyTenantId);
await using var db = new AppDbContext(options, tenantContext);
await db.Database.MigrateAsync();
if (await db.Printers.AnyAsync() || await db.Consumables.AnyAsync() || await db.Materials.AnyAsync() || await db.Quotes.AnyAsync()) return Fail("El destino ya contiene datos de negocio. Se canceló para evitar duplicados.");

await using var transaction = await db.Database.BeginTransactionAsync();
try
{
    await ImportSettings(); await ImportPrinters(); await ImportConsumables(); await ImportMaterials();
    await ImportQuotes(); await ImportQuoteConsumables(); await ImportQuoteMaterials(); await ImportSales();
    await db.SaveChangesAsync(); await DatabaseSequenceService.AlignBusinessSequencesAsync(db); await transaction.CommitAsync();
}
catch { await transaction.RollbackAsync(); throw; }

var targetCounts = new Dictionary<string, long>
{
    ["settings"] = await db.BusinessSettings.LongCountAsync(), ["printers"] = await db.Printers.LongCountAsync(),
    ["filaments"] = await db.Consumables.LongCountAsync(), ["materials"] = await db.Materials.LongCountAsync(),
    ["quotes"] = await db.Quotes.LongCountAsync(), ["quote_filaments"] = await db.QuoteConsumables.LongCountAsync(),
    ["quote_materials"] = await db.QuoteMaterials.LongCountAsync(), ["sales"] = await db.Sales.LongCountAsync()
};
bool verified = sourceCounts.All(item => targetCounts[item.Key] == item.Value);
Console.WriteLine("\nVerificación del destino:");
foreach (var item in targetCounts) Console.WriteLine($"  {item.Key,-20} {item.Value,6} / {sourceCounts[item.Key],6}");
Console.WriteLine(verified ? "\nMIGRACIÓN VERIFICADA." : "\nERROR: las cantidades no coinciden.");
return verified ? 0 : 2;

string? Value(string name) { int index = arguments.FindIndex(value => value.Equals(name, StringComparison.OrdinalIgnoreCase)); return index >= 0 && index + 1 < arguments.Count ? arguments[index + 1] : null; }
static int Fail(string message) { Console.Error.WriteLine($"ERROR: {message}"); return 1; }
async Task<long> CountSqlite(string table) { await using var command = sourceConnection.CreateCommand(); command.CommandText = $"SELECT COUNT(*) FROM {table}"; return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture); }
static decimal Dec(SqliteDataReader reader, int index) => Convert.ToDecimal(reader.GetValue(index), CultureInfo.InvariantCulture);
static DateTime Utc(SqliteDataReader reader, int index) { var value = DateTime.Parse(reader.GetString(index), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind); return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(); }
async Task<SqliteDataReader> Read(string sql) { var command = sourceConnection.CreateCommand(); command.CommandText = sql; return await command.ExecuteReaderAsync(); }

async Task ImportSettings() { await using var r = await Read("SELECT key,value FROM settings"); while (await r.ReadAsync()) db.BusinessSettings.Add(new BusinessSetting { Key=r.GetString(0), Value=r.GetString(1) }); }
async Task ImportPrinters() { await using var r = await Read("SELECT id,name,build_x,build_y,build_z,nozzle,speed,power_watts,hourly_cost,is_default,active FROM printers"); while (await r.ReadAsync()) db.Printers.Add(new Printer { Id=r.GetInt64(0), Name=r.GetString(1), BuildX=Dec(r,2), BuildY=Dec(r,3), BuildZ=Dec(r,4), Nozzle=Dec(r,5), Speed=Dec(r,6), PowerWatts=Dec(r,7), HourlyCost=Dec(r,8), IsDefault=r.GetBoolean(9), Active=r.GetBoolean(10) }); }
async Task ImportConsumables() { await using var r = await Read("SELECT id,name,category,material,color,price_per_kg,density,is_default,active,stock_quantity FROM filaments"); while (await r.ReadAsync()) { var stockQuantity = r.GetInt32(9); db.Consumables.Add(new Consumable { Id=r.GetInt64(0), Name=r.GetString(1), Category=r.GetString(2), Material=r.GetString(3), Color=r.GetString(4), PricePerUnit=Dec(r,5), Density=Dec(r,6), IsDefault=r.GetBoolean(7), Active=r.GetBoolean(8), StockQuantity=stockQuantity, StockGrams=stockQuantity * 1000m }); } }
async Task ImportMaterials() { await using var r = await Read("SELECT id,name,category,unit,unit_price,active FROM materials"); while (await r.ReadAsync()) db.Materials.Add(new ExtraMaterial { Id=r.GetInt64(0), Name=r.GetString(1), Category=r.GetString(2), Unit=r.GetString(3), UnitPrice=Dec(r,4), Active=r.GetBoolean(5) }); }
async Task ImportQuotes() { await using var r = await Read("SELECT id,order_code,created_at,customer,project_name,printer_name,print_hours,quantity,manual_additional,profit_multiplier,notes,total_weight,material_cost,electricity_cost,machine_cost,maintenance_cost,labor_cost,additional_cost,functional_surcharge,subtotal,profit_amount,tax_amount,recommended_price FROM quotes"); while (await r.ReadAsync()) db.Quotes.Add(new Quote { Id=r.GetInt64(0), OrderCode=r.GetString(1), CreatedAtUtc=Utc(r,2), Customer=r.GetString(3), ProjectName=r.GetString(4), PrinterName=r.GetString(5), PrintHours=Dec(r,6), Quantity=r.GetInt32(7), AdditionalManualCost=Dec(r,8), ProfitMultiplier=Dec(r,9), Notes=r.GetString(10), TotalWeight=Dec(r,11), MaterialCost=Dec(r,12), ElectricityCost=Dec(r,13), MachineCost=Dec(r,14), MaintenanceCost=Dec(r,15), LaborCost=Dec(r,16), AdditionalCost=Dec(r,17), FunctionalSurcharge=Dec(r,18), Subtotal=Dec(r,19), ProfitAmount=Dec(r,20), TaxAmount=Dec(r,21), RecommendedPrice=Dec(r,22) }); }
async Task ImportQuoteConsumables() { await using var r = await Read("SELECT id,quote_id,filament_id,filament_name,category,material,color,grams,price_per_kg,density,line_cost FROM quote_filaments"); while (await r.ReadAsync()) db.QuoteConsumables.Add(new QuoteConsumable { Id=r.GetInt64(0), QuoteId=r.GetInt64(1), LegacyConsumableId=r.GetInt64(2), Name=r.GetString(3), Category=r.GetString(4), Material=r.GetString(5), Color=r.GetString(6), Grams=Dec(r,7), PricePerUnit=Dec(r,8), Density=Dec(r,9), LineCost=Dec(r,10) }); }
async Task ImportQuoteMaterials() { await using var r = await Read("SELECT id,quote_id,material_id,material_name,quantity,unit_price,line_cost FROM quote_materials"); while (await r.ReadAsync()) db.QuoteMaterials.Add(new QuoteMaterial { Id=r.GetInt64(0), QuoteId=r.GetInt64(1), LegacyMaterialId=r.GetInt64(2), Name=r.GetString(3), Quantity=Dec(r,4), UnitPrice=Dec(r,5), LineCost=Dec(r,6) }); }
async Task ImportSales() { await using var r = await Read("SELECT id,quote_id,sold_at,sale_amount FROM sales"); while (await r.ReadAsync()) db.Sales.Add(new Sale { Id=r.GetInt64(0), QuoteId=r.GetInt64(1), SoldAtUtc=Utc(r,2), SaleAmount=Dec(r,3) }); }
