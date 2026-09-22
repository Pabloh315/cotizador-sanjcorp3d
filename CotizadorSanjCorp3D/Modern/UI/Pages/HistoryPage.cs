using CotizadorSanjCorp3D.Domain;
using CotizadorSanjCorp3D.Services;

namespace CotizadorSanjCorp3D.UI.Pages;

internal sealed class HistoryPage : PageBase
{
    private readonly DataGridView _grid = Theme.Grid(); private readonly TextBox _search = Theme.TextBox();
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1), Width = 140 };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Width = 140 };
    private IReadOnlyList<QuoteSummary> _items = Array.Empty<QuoteSummary>();
    public HistoryPage(AppState state) : base(state)
    {
        var heading = new Panel { Dock = DockStyle.Top, Height = 86 }; var title = Theme.Label("HISTORIAL DE COTIZACIONES", 17, FontStyle.Bold, Theme.Blue); title.Location = new Point(0, 2); var subtitle = Theme.Label("Busca, exporta o elimina cotizaciones guardadas localmente.", 9.5f, FontStyle.Regular, Theme.Muted); subtitle.Location = new Point(0, 42); heading.Controls.Add(title); heading.Controls.Add(subtitle);
        var filters = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 112, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 6), WrapContents = true }; _search.Width = 250; _search.PlaceholderText = "Código, cliente o proyecto"; filters.Controls.Add(_search); filters.Controls.Add(Theme.Label("Desde", 9, FontStyle.Regular, Theme.Muted)); filters.Controls.Add(_from); filters.Controls.Add(Theme.Label("Hasta", 9, FontStyle.Regular, Theme.Muted)); filters.Controls.Add(_to); filters.Controls.Add(Theme.Button("Buscar", (_, _) => RefreshData(), true)); filters.Controls.Add(Theme.Button("Ver resumen", (_, _) => ShowSelected())); filters.Controls.Add(Theme.Button("Exportar CSV", (_, _) => Export())); filters.Controls.Add(Theme.Button("Eliminar", (_, _) => Delete()));
        _search.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; RefreshData(); } }; _grid.CellDoubleClick += (_, _) => ShowSelected();
        var card = new CardPanel { Dock = DockStyle.Fill }; card.Controls.Add(_grid); Controls.Add(card); Controls.Add(filters); Controls.Add(heading);
    }
    public override void RefreshData()
    {
        try
        {
            _items = State.Database.GetQuotes(_search.Text, _from.Value.Date, _to.Value.Date); _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            _grid.DataSource = _items.Select(x => new { x.Id, Código = x.OrderCode, Estado = x.IsSold ? "VENDIDA" : "COTIZACIÓN", Fecha = x.CreatedAt.ToString("dd/MM/yyyy HH:mm"), Cliente = x.Customer, Proyecto = x.ProjectName, Impresora = x.PrinterName, Peso = $"{x.TotalWeight:0.##} g", Costo = $"{x.CostTotal:0.00} {State.Settings.CurrencySymbol}", Precio = $"{x.RecommendedPrice:0.00} {State.Settings.CurrencySymbol}" }).ToList();
            _grid.Columns["Id"].Visible = false; _grid.Columns["Código"].MinimumWidth = 185; _grid.Columns["Estado"].MinimumWidth = 105; _grid.Columns["Fecha"].MinimumWidth = 145;
        }
        catch (Exception ex) { ShowError(ex); }
    }
    private QuoteSummary? Selected() => _grid.SelectedRows.Count == 0 ? null : _items.FirstOrDefault(x => x.Id == Convert.ToInt64(_grid.SelectedRows[0].Cells["Id"].Value));
    private void ShowSelected() { var item = Selected(); if (item is null) return; string sale = item.IsSold ? $"VENDIDA el {item.SoldAt:g}" : "Cotización pendiente"; MessageBox.Show($"Código: {item.OrderCode}\nEstado: {sale}\nFecha: {item.CreatedAt:g}\nCliente: {item.Customer}\nPieza/proyecto: {item.ProjectName}\nImpresora: {item.PrinterName}\nPeso total: {item.TotalWeight:0.##} g\nCosto: {item.CostTotal:0.00} {State.Settings.CurrencySymbol}\nPrecio recomendado: {item.RecommendedPrice:0.00} {State.Settings.CurrencySymbol}\nGanancia proyectada: {item.RecommendedPrice - item.CostTotal:0.00} {State.Settings.CurrencySymbol}", "Resumen de cotización", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    private void Export() { if (_items.Count == 0) { MessageBox.Show("No hay resultados para exportar."); return; } using var dialog = new SaveFileDialog { Filter = "Archivo CSV (*.csv)|*.csv", FileName = $"cotizaciones-{DateTime.Now:yyyyMMdd}.csv" }; if (dialog.ShowDialog(this) != DialogResult.OK) return; try { CsvExport.Quotes(dialog.FileName, _items, State.Settings.CurrencySymbol); MessageBox.Show("Archivo CSV exportado correctamente."); } catch (Exception ex) { ShowError(ex); } }
    private void Delete() { var item = Selected(); if (item is null) return; string saleWarning = item.IsSold ? "\n\nTambién se eliminará su registro del reporte de ventas." : ""; if (MessageBox.Show($"¿Eliminar definitivamente la cotización {item.OrderCode}?{saleWarning}", "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return; try { State.Database.DeleteQuote(item.Id); State.NotifyChanged(); } catch (Exception ex) { ShowError(ex); } }
}
