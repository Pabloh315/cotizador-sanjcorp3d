using CotizadorSanjCorp3D.Domain;
using CotizadorSanjCorp3D.Services;

namespace CotizadorSanjCorp3D.UI.Pages;

internal sealed class QuotePage : PageBase
{
    private readonly TextBox _customer = Theme.TextBox(); private readonly TextBox _project = Theme.TextBox();
    private readonly ComboBox _printer = Theme.Combo(); private readonly NumericUpDown _hours = Theme.Number(1, 10000, 0, 1);
    private readonly NumericUpDown _minutes = Theme.Number(0, 59, 0, 1); private readonly NumericUpDown _quantity = Theme.Number(1, 10000, 0, 1);
    private readonly NumericUpDown _manualExtra = Theme.Number(0, 1000000, 2, 0.5m);
    private readonly NumericUpDown _multiplier = Theme.Number(1.30m, 100, 2, 0.05m);
    private readonly TextBox _notes = Theme.TextBox(); private readonly ComboBox _filament = Theme.Combo(); private readonly NumericUpDown _grams = Theme.Number(0, 100000, 2, 1);
    private readonly ComboBox _material = Theme.Combo(); private readonly NumericUpDown _materialQuantity = Theme.Number(1, 100000, 2, 1);
    private readonly DataGridView _filamentGrid = Theme.Grid(); private readonly DataGridView _materialGrid = Theme.Grid();
    private readonly Label _weightLabel = Theme.Label("0 colores · 0 g", 10.5f, FontStyle.Bold, Theme.Blue);
    private readonly Label _quoteStatus = Theme.Label("Nueva cotización", 8.5f, FontStyle.Bold, Theme.Muted);
    private readonly Dictionary<string, Label> _resultLabels = new();
    private readonly List<FilamentUsage> _filamentUsages = new(); private readonly List<MaterialUsage> _materialUsages = new();
    private QuoteCalculation? _calculation;
    private long? _currentQuoteId;
    private bool _loadingQuote;

    public QuotePage(AppState state) : base(state)
    {
        Padding = new Padding(16);
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterWidth = 10, BackColor = Theme.Background };
        bool initialSplitApplied = false;
        split.SizeChanged += (_, _) => { if (!initialSplitApplied && split.Width > 800) { split.SplitterDistance = (int)(split.Width * 0.58); initialSplitApplied = true; } };
        split.Panel1.Controls.Add(CreateInputArea()); split.Panel2.Controls.Add(CreateResultArea()); Controls.Add(split);
        _notes.Multiline = true; _notes.Height = 62;
        _filament.DropDownWidth = 420;
        _hours.MinimumSize = new Size(95, 30); _hours.Width = 95; _minutes.MinimumSize = new Size(95, 30); _minutes.Width = 95;
        _filamentGrid.Height = 145; _filamentGrid.Dock = DockStyle.Top; _materialGrid.Height = 120; _materialGrid.Dock = DockStyle.Top;
        _multiplier.Minimum = 1m;
        _multiplier.Value = state.Settings.DefaultProfitMultiplier;
        RefreshData(); RefreshUsageGrids();
        WatchInputChanges();
    }

    public override void RefreshData()
    {
        bool previousLoading = _loadingQuote; _loadingQuote = true;
        try
        {
            long? printerId = (_printer.SelectedItem as Printer)?.Id; long? filamentId = (_filament.SelectedItem as Filament)?.Id; long? materialId = (_material.SelectedItem as ExtraMaterial)?.Id;
            var printers = State.Database.GetPrinters(); var filaments = State.Database.GetFilaments(); var materials = State.Database.GetMaterials();
            Bind(_printer, printers, nameof(Printer.Name), printerId, printers.FirstOrDefault(x => x.IsDefault));
            Bind(_filament, filaments, nameof(Filament.DisplayName), filamentId, filaments.FirstOrDefault(x => x.IsDefault));
            Bind(_material, materials, nameof(ExtraMaterial.Name), materialId, materials.FirstOrDefault());
        }
        finally { _loadingQuote = previousLoading; }
    }

    private Control CreateInputArea()
    {
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Background };
        var stack = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0), BackColor = Color.Transparent };
        stack.SizeChanged += (_, _) => { foreach (Control control in stack.Controls) control.Width = Math.Max(490, scroll.ClientSize.Width - 28); };
        stack.Controls.Add(Section("1. DATOS DE LA IMPRESIÓN", Theme.Blue, CreateBasicForm(), 410));
        stack.Controls.Add(Section("2. CONSUMIBLES / COLORES", Theme.Green, CreateFilamentForm(), 335));
        stack.Controls.Add(Section("3. MATERIALES Y COSTOS ADICIONALES", Theme.Purple, CreateMaterialForm(), 315));
        scroll.Controls.Add(stack); return scroll;
    }

    private Control CreateBasicForm()
    {
        var form = Theme.FormGrid(2); form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        AddPair(form, "Cliente", _customer, "Pieza o proyecto", _project, 0);
        AddPair(form, "Impresora", _printer, "Cantidad de piezas", _quantity, 1);
        var time = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight }; time.Controls.Add(_hours); time.Controls.Add(Theme.Label("h", 10, FontStyle.Regular, Theme.Muted)); time.Controls.Add(_minutes); time.Controls.Add(Theme.Label("min", 10, FontStyle.Regular, Theme.Muted));
        AddPair(form, "Tiempo de impresión por pieza", time, "Multiplicador de ganancia", _multiplier, 2);
        AddPair(form, "Notas", _notes, "Costo adicional manual", _manualExtra, 3); return form;
    }

    private Control CreateFilamentForm()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var add = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, MinimumSize = new Size(0, 48), FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        _filament.Width = 200; _grams.Width = 90; add.Controls.Add(_filament); add.Controls.Add(_grams); add.Controls.Add(Theme.Label("g por pieza", 9, FontStyle.Regular, Theme.Muted));
        add.Controls.Add(Theme.Button("Agregar", (_, _) => AddFilament(), true, Theme.Green)); add.Controls.Add(Theme.Button("Quitar", (_, _) => RemoveFilament()));
        _weightLabel.Dock = DockStyle.Bottom; _weightLabel.Padding = new Padding(0, 8, 0, 0);
        panel.Controls.Add(_filamentGrid); panel.Controls.Add(add); panel.Controls.Add(_weightLabel); return panel;
    }

    private Control CreateMaterialForm()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var add = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, MinimumSize = new Size(0, 48), FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        _material.Width = 270; _materialQuantity.Width = 120; add.Controls.Add(_material); add.Controls.Add(_materialQuantity);
        add.Controls.Add(Theme.Button("Agregar", (_, _) => AddMaterial(), true, Theme.Purple)); add.Controls.Add(Theme.Button("Quitar", (_, _) => RemoveMaterial()));
        panel.Controls.Add(_materialGrid); panel.Controls.Add(add); return panel;
    }

    private Control CreateResultArea()
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(22) };
        var stack = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Color.Transparent };
        stack.Controls.Add(Theme.Label("RESULTADO", 17, FontStyle.Bold, Theme.Green)); stack.Controls.Add(Theme.Label("Desglose transparente de costos", 8.5f, FontStyle.Regular, Theme.Muted)); stack.Controls.Add(_quoteStatus);
        foreach (var row in new[] { ("Material", "material"), ("Electricidad", "electricity"), ("Mantenimiento por impresión", "maintenance"), ("Adicionales", "additional"), ("COSTO TOTAL", "subtotal"), ("Ganancia por multiplicador", "profit"), ("Impuestos", "tax") }) stack.Controls.Add(ResultRow(row.Item1, row.Item2));
        var recommended = new Panel { Width = 295, Height = 105, BackColor = Color.FromArgb(20, 65, 46), Margin = new Padding(0, 12, 0, 12) };
        var caption = Theme.Label("PRECIO RECOMENDADO", 11, FontStyle.Bold, Theme.Green); caption.Location = new Point(14, 12);
        var value = Theme.Label($"0.00 {State.Settings.CurrencySymbol}", 24, FontStyle.Bold, Theme.Green); value.Name = "recommended"; value.Location = new Point(14, 48); _resultLabels["recommended"] = value;
        recommended.Controls.Add(caption); recommended.Controls.Add(value); stack.Controls.Add(recommended);
        var actions = new FlowLayoutPanel { AutoSize = true, Width = 295, FlowDirection = FlowDirection.LeftToRight, WrapContents = true }; actions.Controls.Add(Theme.Button("Calcular precio", (_, _) => Calculate(), true)); actions.Controls.Add(Theme.Button("Guardar cotización", (_, _) => Save())); actions.Controls.Add(Theme.Button("Buscar cotización", (_, _) => SearchQuote())); actions.Controls.Add(Theme.Button("VENTA", (_, _) => ConfirmSale(), true, Theme.Green)); actions.Controls.Add(Theme.Button("Limpiar", (_, _) => Clear())); stack.Controls.Add(actions);
        var formula = Theme.Label("Fórmula: costos × multiplicador de ganancia + impuestos. Las tarifas se administran en Configuración.", 8.5f, FontStyle.Regular, Theme.Muted); formula.MaximumSize = new Size(285, 0); stack.Controls.Add(formula);
        card.Controls.Add(stack); return card;
    }

    private Control ResultRow(string caption, string key)
    {
        bool longCaption = caption.Length > 23;
        var panel = new Panel { Width = 295, Height = longCaption ? 50 : 38, BackColor = Color.Transparent, Margin = new Padding(0, 3, 0, 0) };
        float captionSize = longCaption ? 8f : key == "subtotal" ? 10.5f : 9.5f;
        var name = Theme.Label(caption, captionSize, key == "subtotal" ? FontStyle.Bold : FontStyle.Regular, key == "subtotal" ? Theme.Blue : Theme.Text); name.Location = new Point(0, 7); name.MaximumSize = new Size(180, 0);
        var value = Theme.Label($"0.00 {State.Settings.CurrencySymbol}", 10, key == "subtotal" ? FontStyle.Bold : FontStyle.Regular, key == "profit" ? Theme.Green : Theme.Text); value.Location = new Point(185, longCaption ? 14 : 9); value.Width = 105; value.TextAlign = ContentAlignment.MiddleRight; _resultLabels[key] = value;
        panel.Controls.Add(name); panel.Controls.Add(value); return panel;
    }

    private CardPanel Section(string title, Color accent, Control content, int height = 360)
    {
        var card = new CardPanel { Height = height }; var label = Theme.Label(title, 11, FontStyle.Bold, accent); label.Dock = DockStyle.Top; label.Height = 34; content.Dock = DockStyle.Fill; card.Controls.Add(content); card.Controls.Add(label); return card;
    }

    private static void AddPair(TableLayoutPanel form, string left, Control leftControl, string right, Control rightControl, int row)
    {
        while (form.RowCount <= row) { form.RowStyles.Add(new RowStyle(SizeType.AutoSize)); form.RowCount++; }
        var lp = Field(left, leftControl); var rp = Field(right, rightControl); form.Controls.Add(lp, 0, row); form.Controls.Add(rp, 1, row);
    }
    private static Control Field(string title, Control control) { var p = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(4) }; if (!string.IsNullOrEmpty(title)) p.Controls.Add(Theme.Label(title, 9, FontStyle.Regular, Theme.Muted)); control.Width = 210; p.Controls.Add(control); return p; }

    private void AddFilament() { if (_filament.SelectedItem is not Filament item || _grams.Value <= 0) { MessageBox.Show("Seleccione un filamento o resina e indique gramos mayores que cero."); return; } if (item.StockQuantity <= 0) { MessageBox.Show($"No hay existencia de {item.Name} · {item.Material} · {item.Color}.\n\nActualiza la cantidad de rollos o botellas en Consumibles → Inventario.", "Sin existencia", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } _filamentUsages.Add(new(item.Id, item.Name, item.Category, item.Material, item.Color, _grams.Value, item.PricePerUnit, item.Density)); _grams.Value = 0; RefreshUsageGrids(); InvalidateCalculation(); }
    private void RemoveFilament() { if (_filamentGrid.SelectedRows.Count == 0) return; int index = _filamentGrid.SelectedRows[0].Index; if (index >= 0 && index < _filamentUsages.Count) _filamentUsages.RemoveAt(index); RefreshUsageGrids(); InvalidateCalculation(); }
    private void AddMaterial() { if (_material.SelectedItem is not ExtraMaterial item || _materialQuantity.Value <= 0) return; _materialUsages.Add(new(item.Id, item.Name, _materialQuantity.Value, item.UnitPrice)); _materialQuantity.Value = 1; RefreshUsageGrids(); InvalidateCalculation(); }
    private void RemoveMaterial() { if (_materialGrid.SelectedRows.Count == 0) return; int index = _materialGrid.SelectedRows[0].Index; if (index >= 0 && index < _materialUsages.Count) _materialUsages.RemoveAt(index); RefreshUsageGrids(); InvalidateCalculation(); }
    private void RefreshUsageGrids() { _filamentGrid.DataSource = _filamentUsages.Select(x => new { Nombre = x.FilamentName, Clase = x.Category == "Resina" ? "RESINA" : "FILAMENTO", Tipo = x.Material, Color = x.Color, Peso_g = x.Grams, Costo = x.Cost }).ToList(); _materialGrid.DataSource = _materialUsages.Select(x => new { Material = x.MaterialName, Cantidad = x.Quantity, Precio = x.UnitPrice, Costo = x.Cost }).ToList(); int colors = _filamentUsages.Select(x => x.Color).Where(color => !string.IsNullOrWhiteSpace(color)).Distinct(StringComparer.OrdinalIgnoreCase).Count(); _weightLabel.Text = $"Colores: {colors}  ·  Peso por pieza: {_filamentUsages.Sum(x => x.Grams):0.##} g  ·  Total: {_filamentUsages.Sum(x => x.Grams) * _quantity.Value:0.##} g"; }

    private QuoteInput ReadInput() { if (_printer.SelectedItem is not Printer printer) throw new InvalidOperationException("Primero registre y seleccione una impresora."); decimal time = _hours.Value + _minutes.Value / 60m; if (string.IsNullOrWhiteSpace(_customer.Text) || string.IsNullOrWhiteSpace(_project.Text)) throw new InvalidOperationException("Cliente y pieza/proyecto son obligatorios."); return new(_customer.Text.Trim(), _project.Text.Trim(), printer, time, (int)_quantity.Value, _manualExtra.Value, _multiplier.Value, _notes.Text.Trim(), _filamentUsages.ToList(), _materialUsages.ToList()); }
    private void Calculate() { try { _calculation = CalculationService.Calculate(ReadInput(), State.Settings); ShowCalculation(_calculation); } catch (Exception ex) { ShowError(ex); } }
    private void Save() { try { var input = ReadInput(); _calculation = CalculationService.Calculate(input, State.Settings); var saved = State.Database.SaveQuote(input, _calculation); ShowCalculation(_calculation); _currentQuoteId = saved.Id; _quoteStatus.Text = $"Código: {saved.OrderCode} · pendiente de venta"; _quoteStatus.ForeColor = Theme.Blue; State.NotifyChanged(); QuoteDialogs.ShowSaved(this, saved.OrderCode); } catch (Exception ex) { ShowError(ex); } }
    private void ShowCalculation(QuoteCalculation c) { string symbol = State.Settings.CurrencySymbol; void Set(string key, decimal value) => _resultLabels[key].Text = $"{value:N2} {symbol}"; Set("material", c.MaterialCost); Set("electricity", c.ElectricityCost); Set("maintenance", c.MaintenanceCost); Set("additional", c.AdditionalCost); Set("subtotal", c.Subtotal); Set("profit", c.ProfitAmount); Set("tax", c.TaxAmount); Set("recommended", c.RecommendedPrice); }
    private void SearchQuote()
    {
        try
        {
            long? id = QuoteDialogs.Search(this, State.Database);
            if (!id.HasValue) return;
            QuoteDetails? quote = State.Database.GetQuote(id.Value);
            if (quote is null) throw new InvalidOperationException("La cotización seleccionada ya no existe.");
            LoadQuote(quote);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void LoadQuote(QuoteDetails quote)
    {
        _loadingQuote = true;
        try
        {
            _customer.Text = quote.Customer; _project.Text = quote.ProjectName;
            Printer? printer = _printer.Items.Cast<Printer>().FirstOrDefault(item => item.Name.Equals(quote.PrinterName, StringComparison.OrdinalIgnoreCase));
            if (printer is null)
            {
                var archived = new Printer(0, quote.PrinterName, 1, 1, 1, 0.4m, 1, 0, 0, false, false);
                var printers = _printer.Items.Cast<Printer>().Append(archived).ToList();
                _printer.DataSource = null; _printer.DisplayMember = nameof(Printer.Name); _printer.ValueMember = nameof(Printer.Id); _printer.DataSource = printers; printer = archived;
            }
            _printer.SelectedItem = printer;
            decimal wholeHours = decimal.Truncate(quote.PrintHours); decimal minutes = decimal.Round((quote.PrintHours - wholeHours) * 60m, 0, MidpointRounding.AwayFromZero);
            if (minutes >= 60) { wholeHours++; minutes = 0; }
            SetValue(_hours, wholeHours); SetValue(_minutes, minutes); SetValue(_quantity, quote.Quantity); SetValue(_manualExtra, quote.AdditionalManualCost); SetValue(_multiplier, quote.ProfitMultiplier);
            _notes.Text = quote.Notes; _filamentUsages.Clear(); _filamentUsages.AddRange(quote.Filaments); _materialUsages.Clear(); _materialUsages.AddRange(quote.Materials);
            RefreshUsageGrids(); ShowCalculation(quote.Calculation); _calculation = quote.Calculation; _currentQuoteId = quote.Id;
            _quoteStatus.Text = quote.SoldAt.HasValue ? $"Código: {quote.OrderCode} · VENDIDA {quote.SoldAt:g}" : $"Código: {quote.OrderCode} · pendiente de venta";
            _quoteStatus.ForeColor = quote.SoldAt.HasValue ? Theme.Green : Theme.Blue;
        }
        finally { _loadingQuote = false; }
    }

    private void ConfirmSale()
    {
        if (!_currentQuoteId.HasValue) { MessageBox.Show("Primero guarda la cotización o búscala por su código, cliente o proyecto.", "Venta", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        QuoteDetails? quote = State.Database.GetQuote(_currentQuoteId.Value);
        if (quote is null) { MessageBox.Show("La cotización ya no existe."); return; }
        if (quote.SoldAt.HasValue) { MessageBox.Show($"Esta venta ya fue confirmada el {quote.SoldAt:g}.", "Venta registrada", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (MessageBox.Show($"¿Confirmar la venta de la cotización {quote.OrderCode} por {quote.Calculation.RecommendedPrice:N2} {State.Settings.CurrencySymbol}?", "Confirmar venta", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            if (!State.Database.ConfirmSale(quote.Id)) throw new InvalidOperationException("La venta ya estaba registrada o la cotización no existe.");
            State.NotifyChanged(); QuoteDetails updated = State.Database.GetQuote(quote.Id)!; LoadQuote(updated);
            MessageBox.Show("Venta confirmada y agregada al reporte de ventas.", "Venta registrada", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void WatchInputChanges()
    {
        _customer.TextChanged += (_, _) => InvalidateCurrentQuote(); _project.TextChanged += (_, _) => InvalidateCurrentQuote(); _notes.TextChanged += (_, _) => InvalidateCurrentQuote();
        _printer.SelectedIndexChanged += (_, _) => InvalidateCurrentQuote(); _hours.ValueChanged += (_, _) => InvalidateCurrentQuote(); _minutes.ValueChanged += (_, _) => InvalidateCurrentQuote();
        _quantity.ValueChanged += (_, _) => { RefreshUsageGrids(); InvalidateCurrentQuote(); }; _manualExtra.ValueChanged += (_, _) => InvalidateCurrentQuote(); _multiplier.ValueChanged += (_, _) => InvalidateCurrentQuote();
    }

    private void InvalidateCurrentQuote()
    {
        if (_loadingQuote) return;
        _currentQuoteId = null; _quoteStatus.Text = "Nueva cotización o cambios sin guardar"; _quoteStatus.ForeColor = Theme.Muted; _calculation = null;
    }

    private void InvalidateCalculation() { _calculation = null; InvalidateCurrentQuote(); }
    private void Clear() { _loadingQuote = true; try { _customer.Clear(); _project.Clear(); _hours.Value = 1; _minutes.Value = 0; _quantity.Value = 1; _manualExtra.Value = 0; _multiplier.Value = Math.Min(_multiplier.Maximum, State.Settings.DefaultProfitMultiplier); _notes.Clear(); _filamentUsages.Clear(); _materialUsages.Clear(); RefreshUsageGrids(); ShowCalculation(new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)); _calculation = null; _currentQuoteId = null; _quoteStatus.Text = "Nueva cotización"; _quoteStatus.ForeColor = Theme.Muted; } finally { _loadingQuote = false; } }
    private static void SetValue(NumericUpDown control, decimal value) => control.Value = Math.Min(control.Maximum, Math.Max(control.Minimum, value));
    private static void Bind<T>(ComboBox combo, IReadOnlyList<T> items, string display, long? selectedId, T? preferred) where T : class
    {
        combo.DataSource = null;
        combo.DisplayMember = display;
        combo.ValueMember = "Id";
        combo.DataSource = items.ToList();
        T? selected = items.FirstOrDefault(x => Convert.ToInt64(x.GetType().GetProperty("Id")!.GetValue(x)) == selectedId);
        combo.SelectedItem = selected ?? preferred ?? items.FirstOrDefault();
    }
}
