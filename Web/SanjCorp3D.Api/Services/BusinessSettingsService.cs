using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Contracts;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Models;

namespace SanjCorp3D.Api.Services;

public sealed class BusinessSettingsService(AppDbContext db)
{
    // Keep the spreadsheet's neutral defaults. Maintenance and rounding are
    // optional inputs, so they must not change a quote unless the user sets
    // them explicitly in Configuración.
    public static BusinessSettingsDto Defaults => new("SANJ CORP 3D", "Bolivianos", "Bs", 1.10m, 0m, 3m, 0m, 0m, 2);

    public async Task<BusinessSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var values = await db.BusinessSettings.AsNoTracking().ToDictionaryAsync(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var defaults = Defaults;
        decimal legacyMargin = Number(values, "default_margin", 30m);
        return new(
            Text(values, "business_name", defaults.BusinessName), Text(values, "currency_name", defaults.CurrencyName),
            Text(values, "currency_symbol", defaults.CurrencySymbol), Number(values, "electricity_per_kwh", defaults.ElectricityPerKwh),
            Number(values, "maintenance_per_print", defaults.MaintenancePerPrint), Number(values, "profit_multiplier", 1m + legacyMargin / 100m),
            Number(values, "tax_percent", defaults.TaxPercent), Number(values, "round_to", defaults.RoundTo),
            Math.Clamp((int)Number(values, "decimal_places", defaults.DecimalPlaces), 0, 4));
    }

    public async Task SaveAsync(BusinessSettingsDto settings, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.BusinessName) || string.IsNullOrWhiteSpace(settings.CurrencySymbol)) throw new ArgumentException("El negocio y el símbolo monetario son obligatorios.");
        if (settings.ElectricityPerKwh < 0 || settings.MaintenancePerPrint < 0 || settings.TaxPercent < 0 || settings.RoundTo < 0) throw new ArgumentException("Los costos no pueden ser negativos.");
        if (settings.DefaultProfitMultiplier < 1) throw new ArgumentException("El multiplicador debe ser igual o mayor que 1.");
        if (settings.DecimalPlaces is < 0 or > 4) throw new ArgumentException("Los decimales deben estar entre 0 y 4.");
        var values = new Dictionary<string, string>
        {
            ["business_name"] = settings.BusinessName.Trim(), ["currency_name"] = settings.CurrencyName.Trim(),
            ["currency_symbol"] = settings.CurrencySymbol.Trim(), ["electricity_per_kwh"] = Invariant(settings.ElectricityPerKwh),
            ["maintenance_per_print"] = Invariant(settings.MaintenancePerPrint), ["profit_multiplier"] = Invariant(settings.DefaultProfitMultiplier),
            ["tax_percent"] = Invariant(settings.TaxPercent), ["round_to"] = Invariant(settings.RoundTo),
            ["decimal_places"] = settings.DecimalPlaces.ToString(CultureInfo.InvariantCulture)
        };
        foreach (var item in values)
        {
            var tenantId = db.CurrentTenantId ?? throw new InvalidOperationException("No se seleccionó un espacio de trabajo.");
            var current = await db.BusinessSettings.FindAsync([tenantId, item.Key], cancellationToken);
            if (current is null) db.BusinessSettings.Add(new BusinessSetting { Key = item.Key, Value = item.Value });
            else current.Value = item.Value;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string Text(IReadOnlyDictionary<string, string> values, string key, string fallback) => values.TryGetValue(key, out var value) ? value : fallback;
    private static decimal Number(IReadOnlyDictionary<string, string> values, string key, decimal fallback) => values.TryGetValue(key, out var value) && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static string Invariant(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}


