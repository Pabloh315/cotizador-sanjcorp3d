using CotizadorSanjCorp3D.Domain;
using CotizadorSanjCorp3D.Services;

namespace CotizadorSanjCorp3D.UI.Pages;

internal sealed class ReportsPage : PageBase
{
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1), Width = 140 };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Width = 140 };
    private readonly Label _count = Metric("0"); private readonly Label _revenue = Metric("0"); private readonly Label _profit = Metric("0");
    private readonly Label _saleCount = Metric("0"); private readonly Label _salesRevenue = Metric("0"); private readonly Label _salesProfit = Metric("0");
    private readonly DataGridView _sales = Theme.Grid(); private readonly DataGridView _filaments = Theme.Grid(); private readonly DataGridView _costs = Theme.Grid(); private ReportSummary? _report;
    public ReportsPage(AppState state) : base(state)
    {
        var heading = new Panel { Dock = DockStyle.Top, Height = 86 }; var title = Theme.Label("REPORTES", 17, FontStyle.Bold, Theme.Blue); title.Location = new Point(0, 2); var sub = Theme.Label("Cotizaciones y ventas confirmadas por rango de fechas.", 9.5f, FontStyle.Regular, Theme.Muted); sub.Location = new Point(0, 42); heading.Controls.Add(title); heading.Controls.Add(sub);
        var filters = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 112, Padding = new Padding(0, 4, 0, 6), WrapContents = true }; filters.Controls.Add(Theme.Label("Desde", 9, FontStyle.Regular, Theme.Muted)); filters.Controls.Add(_from); filters.Controls.Add(Theme.Label("Hasta", 9, FontStyle.Regular, Theme.Muted)); filters.Controls.Add(_to); filters.Controls.Add(Theme.Button("Actualizar", (_, _) => RefreshData(), true)); filters.Controls.Add(Theme.Button("CSV cotizaciones", (_, _) => Export())); filters.Controls.Add(Theme.Button("CSV ventas", (_, _) => ExportSales(), false));
        var metrics = new TableLayoutPanel { Dock = DockStyle.Top, Height = 205, ColumnCount = 3, RowCount = 2 }; for (int i = 0; i < 3; i++) metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f)); metrics.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); metrics.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); metrics.Controls.Add(MetricCard("COTIZACIONES", _count, Theme.Blue), 0, 0); metrics.Controls.Add(MetricCard("INGRESO PROYECTADO", _revenue, Theme.Green), 1, 0); metrics.Controls.Add(MetricCard("GANANCIA PROYECTADA", _profit, Theme.Orange), 2, 0); metrics.Controls.Add(MetricCard("VENTAS CONFIRMADAS", _saleCount, Theme.Purple), 0, 1); metrics.Controls.Add(MetricCard("INGRESOS POR VENTAS", _salesRevenue, Theme.Green), 1, 1); metrics.Controls.Add(MetricCard("GANANCIA POR VENTAS", _salesProfit, Theme.Orange), 2, 1);
        var grids = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 }; grids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); grids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); grids.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); grids.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); var salesCard = GridCard("VENTAS GENERADAS", _sales); grids.Controls.Add(salesCard, 0, 0); grids.SetColumnSpan(salesCard, 2); grids.Controls.Add(GridCard("Distribución por consumible o resina (g)", _filaments), 0, 1); grids.Controls.Add(GridCard("Costos por categoría", _costs), 1, 1);
        Controls.Add(grids); Controls.Add(metrics); Controls.Add(filters); Controls.Add(heading);
    }
    public override void RefreshData()
    {
        try
        {
            _report = State.Database.GetReport(_from.Value.Date, _to.Value.Date); string s = State.Settings.CurrencySymbol;
            _count.Text = _report.QuoteCount.ToString(); _revenue.Text = $"{_report.ProjectedRevenue:N2} {s}"; _profit.Text = $"{_report.ProjectedProfit:N2} {s}";
            _saleCount.Text = _report.SaleCount.ToString(); _salesRevenue.Text = $"{_report.SalesRevenue:N2} {s}"; _salesProfit.Text = $"{_report.SalesProfit:N2} {s}";
            _sales.DataSource = _report.Sales.Select(x => new { Código = x.OrderCode, Fecha = x.SoldAt.ToString("dd/MM/yyyy HH:mm"), Cliente = x.Customer, Proyecto = x.ProjectName, Venta = $"{x.SaleAmount:N2} {s}", Ganancia = $"{x.Profit:N2} {s}" }).ToList();
            if (_sales.Columns.Contains("Código")) { _sales.Columns["Código"].MinimumWidth = 190; _sales.Columns["Código"].FillWeight = 130; }
            _filaments.DataSource = _report.FilamentDistribution.Select(x => new { Filamento = x.Key, Gramos = x.Value }).ToList();
            _costs.DataSource = _report.CostDistribution.Select(x => new { Categoría = x.Key, Costo = $"{x.Value:N2} {s}" }).ToList();
        }
        catch (Exception ex) { ShowError(ex); }
    }
    private void Export() { var items = State.Database.GetQuotes("", _from.Value.Date, _to.Value.Date); if (items.Count == 0) { MessageBox.Show("No hay cotizaciones en el rango seleccionado."); return; } using var dialog = new SaveFileDialog { Filter = "Archivo CSV (*.csv)|*.csv", FileName = $"reporte-{_from.Value:yyyyMMdd}-{_to.Value:yyyyMMdd}.csv" }; if (dialog.ShowDialog(this) == DialogResult.OK) CsvExport.Quotes(dialog.FileName, items, State.Settings.CurrencySymbol); }
    private void ExportSales() { var items = State.Database.GetSales(_from.Value.Date, _to.Value.Date); if (items.Count == 0) { MessageBox.Show("No hay ventas confirmadas en el rango seleccionado."); return; } using var dialog = new SaveFileDialog { Filter = "Archivo CSV (*.csv)|*.csv", FileName = $"ventas-{_from.Value:yyyyMMdd}-{_to.Value:yyyyMMdd}.csv" }; if (dialog.ShowDialog(this) == DialogResult.OK) CsvExport.Sales(dialog.FileName, items, State.Settings.CurrencySymbol); }
    private static Label Metric(string value) => Theme.Label(value, 18, FontStyle.Bold);
    private static Control MetricCard(string caption, Label metric, Color color) { var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(5), Padding = new Padding(14) }; var cap = Theme.Label(caption, 8.5f, FontStyle.Bold, Theme.Muted); cap.Dock = DockStyle.Top; metric.ForeColor = color; metric.Dock = DockStyle.Fill; metric.TextAlign = ContentAlignment.MiddleLeft; card.Controls.Add(metric); card.Controls.Add(cap); return card; }
    private static Control GridCard(string title, DataGridView grid) { var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(5) }; var label = Theme.Label(title, 11, FontStyle.Bold); label.Dock = DockStyle.Top; label.Height = 38; card.Controls.Add(grid); card.Controls.Add(label); return card; }
}
