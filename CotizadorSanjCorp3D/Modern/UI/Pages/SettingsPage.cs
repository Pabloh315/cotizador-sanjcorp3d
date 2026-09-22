using CotizadorSanjCorp3D.Domain;
using CotizadorSanjCorp3D.Services;

namespace CotizadorSanjCorp3D.UI.Pages;

internal sealed class SettingsPage : PageBase
{
    private readonly TextBox _business = Theme.TextBox(); private readonly TextBox _currencyName = Theme.TextBox(); private readonly TextBox _symbol = Theme.TextBox();
    private readonly NumericUpDown _electricity = Theme.Number(0, 10000, 4, 0.01m);
    private readonly NumericUpDown _maintenance = Theme.Number(0, 10000, 2, 0.1m); private readonly NumericUpDown _multiplier = Theme.Number(1.30m, 100, 2, 0.05m);
    private readonly NumericUpDown _tax = Theme.Number(0, 1000, 2, 1);
    private readonly NumericUpDown _round = Theme.Number(0, 1000, 2, 0.1m); private readonly NumericUpDown _decimals = Theme.Number(2, 4, 0, 1);
    private readonly Label _path;
    public SettingsPage(AppState state) : base(state)
    {
        _path = Theme.Label(State.Database.PathOnDisk, 9, FontStyle.Regular, Theme.Muted);
        _multiplier.Minimum = 1m;
        var heading = new Panel { Dock = DockStyle.Top, Height = 86 }; var title = Theme.Label("CONFIGURACIÓN", 17, FontStyle.Bold, Theme.Blue); title.Location = new Point(0, 2); var sub = Theme.Label("Tarifas y preferencias utilizadas por todas las cotizaciones.", 9.5f, FontStyle.Regular, Theme.Muted); sub.Location = new Point(0, 42); heading.Controls.Add(title); heading.Controls.Add(sub);
        var tabs = new TabControl { Dock = DockStyle.Fill, Font = Theme.Font(), Padding = new Point(14, 8) }; tabs.TabPages.Add(CreateGeneral()); tabs.TabPages.Add(CreateCosts()); tabs.TabPages.Add(CreateBackup());
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 68, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 6) }; actions.Controls.Add(Theme.Button("Guardar cambios", (_, _) => Save(), true)); actions.Controls.Add(Theme.Button("Restaurar valores predeterminados", (_, _) => LoadValues(AppSettings.Defaults)));
        Controls.Add(tabs); Controls.Add(actions); Controls.Add(heading); LoadValues(State.Settings);
    }
    public override void RefreshData() => LoadValues(State.Settings);
    private TabPage CreateGeneral() { var page = Page("General"); var form = Theme.FormGrid(); Theme.AddField(form, "Nombre del negocio", _business); Theme.AddField(form, "Nombre de la moneda", _currencyName); Theme.AddField(form, "Símbolo monetario", _symbol); Theme.AddField(form, "Decimales mostrados", _decimals); Theme.AddField(form, "Redondear precio final a", _round); page.Controls.Add(form); return page; }
    private TabPage CreateCosts() { var page = Page("Costos"); var form = Theme.FormGrid(); Theme.AddField(form, "Electricidad por kWh", _electricity); Theme.AddField(form, "Mantenimiento por impresión", _maintenance); Theme.AddField(form, "Multiplicador de ganancia predeterminado", _multiplier); Theme.AddField(form, "Impuestos (%)", _tax); page.Controls.Add(form); return page; }
    private TabPage CreateBackup() { var page = Page("Respaldo"); var stack = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(12), AutoScroll = true }; stack.Controls.Add(Theme.Label("DATOS LOCALES", 12, FontStyle.Bold, Theme.Green)); stack.Controls.Add(Theme.Label("La base de datos se guarda únicamente en este equipo. No se incluye en el proyecto ni se envía a internet.", 9.5f, FontStyle.Regular, Theme.Muted)); stack.Controls.Add(Theme.Label("Ruta:", 9, FontStyle.Bold)); stack.Controls.Add(_path); stack.Controls.Add(Theme.Button("Crear respaldo SQLite", (_, _) => Backup(), true, Theme.Green)); stack.Controls.Add(Theme.Button("Restaurar desde respaldo", (_, _) => Restore())); stack.Controls.Add(Theme.Label("Al restaurar se reemplazarán los catálogos, ajustes y cotizaciones actuales. Se solicitará confirmación.", 8.5f, FontStyle.Regular, Theme.Orange)); page.Controls.Add(stack); return page; }
    private static TabPage Page(string title) => new(title) { BackColor = Theme.Card, ForeColor = Theme.Text, Padding = new Padding(20) };
    private void LoadValues(AppSettings s) { _business.Text = s.BusinessName; _currencyName.Text = s.CurrencyName; _symbol.Text = s.CurrencySymbol; Set(_electricity, s.ElectricityPerKwh); Set(_maintenance, s.MaintenancePerPrint); Set(_multiplier, s.DefaultProfitMultiplier); Set(_tax, s.TaxPercent); Set(_round, s.RoundTo); Set(_decimals, s.DecimalPlaces); }
    private void Save() { try { var settings = new AppSettings(_business.Text.Trim(), _currencyName.Text.Trim(), _symbol.Text.Trim(), _electricity.Value, _maintenance.Value, _multiplier.Value, _tax.Value, _round.Value, (int)_decimals.Value); State.Database.SaveSettings(settings); State.ReloadSettings(); MessageBox.Show("Configuración guardada correctamente.", "Guardado", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch (Exception ex) { ShowError(ex); } }
    private void Backup() { using var dialog = new SaveFileDialog { Filter = "Respaldo SQLite (*.db)|*.db", InitialDirectory = AppPaths.BackupDirectory, FileName = $"sanjcorp3d-{DateTime.Now:yyyyMMdd-HHmm}.db" }; if (dialog.ShowDialog(this) != DialogResult.OK) return; try { State.Database.BackupTo(dialog.FileName); MessageBox.Show("Respaldo creado correctamente."); } catch (Exception ex) { ShowError(ex); } }
    private void Restore() { using var dialog = new OpenFileDialog { Filter = "Respaldo SQLite (*.db)|*.db|Todos los archivos (*.*)|*.*", InitialDirectory = AppPaths.BackupDirectory }; if (dialog.ShowDialog(this) != DialogResult.OK) return; if (MessageBox.Show("¿Restaurar este respaldo? Los datos locales actuales serán reemplazados.", "Confirmar restauración", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return; try { State.Database.RestoreFrom(dialog.FileName); State.ReloadSettings(); MessageBox.Show("Respaldo restaurado correctamente."); } catch (Exception ex) { ShowError(ex); } }
    private static void Set(NumericUpDown control, decimal value) => control.Value = Math.Min(control.Maximum, Math.Max(control.Minimum, value));
}
