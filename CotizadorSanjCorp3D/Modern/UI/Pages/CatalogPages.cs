using CotizadorSanjCorp3D.Domain;
using CotizadorSanjCorp3D.Services;

namespace CotizadorSanjCorp3D.UI.Pages;

internal abstract class CatalogPageBase : PageBase
{
    protected readonly DataGridView Grid = Theme.Grid();
    protected readonly FlowLayoutPanel Actions = new() { Dock = DockStyle.Top, Height = 68, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 6) };
    protected CatalogPageBase(AppState state, string title, string subtitle, Color accent) : base(state)
    {
        var heading = new Panel { Dock = DockStyle.Top, Height = 86, BackColor = Color.Transparent };
        var titleLabel = Theme.Label(title, 17, FontStyle.Bold, accent); titleLabel.Location = new Point(0, 2);
        var sub = Theme.Label(subtitle, 9.5f, FontStyle.Regular, Theme.Muted); sub.Location = new Point(0, 42);
        heading.Controls.Add(titleLabel); heading.Controls.Add(sub);
        var card = new CardPanel { Dock = DockStyle.Fill }; card.Controls.Add(Grid);
        Controls.Add(card); Controls.Add(Actions); Controls.Add(heading);
        Grid.CellDoubleClick += (_, _) => EditSelected();
    }
    protected abstract void EditSelected();
    protected long SelectedId() => Grid.CurrentRow is null ? 0 : Convert.ToInt64(Grid.CurrentRow.Cells["Id"].Value);
    protected bool ConfirmArchive(string noun) => MessageBox.Show($"¿Archivar {noun}? Ya no aparecerá en nuevas cotizaciones, pero el historial se conserva.", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
}

internal sealed class PrintersPage : CatalogPageBase
{
    private IReadOnlyList<Printer> _items = Array.Empty<Printer>();
    public PrintersPage(AppState state) : base(state, "IMPRESORAS", "Administra equipos, volumen, boquilla, velocidad y potencia eléctrica.", Theme.Orange)
    {
        Actions.Controls.Add(Theme.Button("+ Nueva impresora", (_, _) => Create(), true, Theme.Orange));
        Actions.Controls.Add(Theme.Button("Editar", (_, _) => EditSelected()));
        Actions.Controls.Add(Theme.Button("Archivar", (_, _) => Archive()));
    }
    public override void RefreshData() { _items = State.Database.GetPrinters(); Grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells; Grid.DataSource = _items.Select(x => new { x.Id, Nombre = x.Name, Volumen = $"{x.BuildX:0} × {x.BuildY:0} × {x.BuildZ:0} mm", Boquilla = $"{x.Nozzle:0.##} mm", Velocidad = $"{x.Speed:0} mm/s", Potencia = $"{x.PowerWatts:0} W", Predeterminada = x.IsDefault ? "Sí" : "" }).ToList(); Grid.Columns["Id"].Visible = false; }
    private void Create() { try { var item = CatalogDialogs.PrinterDialog(this, null); if (item is null) return; State.Database.SavePrinter(item); State.NotifyChanged(); } catch (Exception ex) { ShowError(ex); } }
    protected override void EditSelected() { try { var current = _items.FirstOrDefault(x => x.Id == SelectedId()); if (current is null) return; var item = CatalogDialogs.PrinterDialog(this, current); if (item is null) return; State.Database.SavePrinter(item); State.NotifyChanged(); } catch (Exception ex) { ShowError(ex); } }
    private void Archive() { try { long id = SelectedId(); if (id == 0 || !ConfirmArchive("la impresora seleccionada")) return; State.Database.ArchiveCatalogItem("printers", id); State.NotifyChanged(); } catch (Exception ex) { ShowError(ex); } }
}

internal sealed class FilamentsPage : PageBase
{
    private readonly DataGridView _consumables = Theme.Grid();
    private readonly DataGridView _resins = Theme.Grid();
    private readonly DataGridView _inventory = Theme.Grid();
    private DataGridView _activeGrid;
    private IReadOnlyList<Filament> _items = Array.Empty<Filament>();

    public FilamentsPage(AppState state) : base(state)
    {
        _activeGrid = _consumables;
        var heading = new Panel { Dock = DockStyle.Top, Height = 86, BackColor = Color.Transparent };
        var title = Theme.Label("CONSUMIBLES", 17, FontStyle.Bold, Theme.Green); title.Location = new Point(0, 2);
        var subtitle = Theme.Label("Administra filamentos, resinas y sus existencias por tipo y color.", 9.5f, FontStyle.Regular, Theme.Muted); subtitle.Location = new Point(0, 42);
        heading.Controls.Add(title); heading.Controls.Add(subtitle);

        var tabs = new TabControl { Dock = DockStyle.Fill, Font = Theme.Font(), Padding = new Point(14, 8) };
        tabs.TabPages.Add(CreateCatalogTab()); tabs.TabPages.Add(CreateInventoryTab());
        tabs.SelectedIndexChanged += (_, _) => BeginInvoke(new Action(() => AutoScrollPosition = Point.Empty));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.Transparent };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        heading.Dock = DockStyle.Fill; layout.Controls.Add(heading, 0, 0); layout.Controls.Add(tabs, 0, 1); Controls.Add(layout);
    }

    private TabPage CreateCatalogTab()
    {
        var page = Tab("FILAMENTOS Y RESINAS");
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 68, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 6) };
        actions.Controls.Add(Theme.Button("+ Nuevo consumible o resina", (_, _) => Create(), true, Theme.Green));
        actions.Controls.Add(Theme.Button("Editar selección", (_, _) => EditSelected()));
        actions.Controls.Add(Theme.Button("Archivar selección", (_, _) => Archive()));

        var content = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.Transparent };
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); content.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        content.Controls.Add(GridSection("FILAMENTOS", "Precio configurado por kilogramo", _consumables, Theme.Green), 0, 0);
        content.Controls.Add(GridSection("RESINAS", "Precio configurado por litro · SLA se administra aquí", _resins, Theme.Purple), 0, 1);
        foreach (var grid in new[] { _consumables, _resins })
        {
            grid.Enter += (_, _) => _activeGrid = grid;
            grid.CellClick += (_, _) => _activeGrid = grid;
            grid.CellDoubleClick += (_, _) => { _activeGrid = grid; EditSelected(); };
        }
        page.Controls.Add(content); page.Controls.Add(actions); return page;
    }

    private TabPage CreateInventoryTab()
    {
        var page = Tab("INVENTARIO");
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 68, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 6) };
        actions.Controls.Add(Theme.Button("Actualizar existencia", (_, _) => UpdateStock(), true, Theme.Green));
        actions.Controls.Add(Theme.Button("+ 1 unidad", (_, _) => AdjustStock(1)));
        actions.Controls.Add(Theme.Button("- 1 unidad", (_, _) => AdjustStock(-1)));
        var card = new CardPanel { Dock = DockStyle.Fill };
        var note = Theme.Label("Control manual de rollos y botellas. Una existencia en cero impedirá agregar ese consumible a una cotización.", 9, FontStyle.Regular, Theme.Muted); note.Dock = DockStyle.Top; note.Height = 42;
        card.Controls.Add(_inventory); card.Controls.Add(note); page.Controls.Add(card); page.Controls.Add(actions);
        _inventory.CellDoubleClick += (_, _) => UpdateStock(); return page;
    }

    public override void RefreshData()
    {
        _items = State.Database.GetFilaments();
        Bind(_consumables, _items.Where(item => !item.IsResin));
        Bind(_resins, _items.Where(item => item.IsResin));
        _inventory.DataSource = _items.Select(item => new
        {
            item.Id,
            Sección = item.IsResin ? "RESINA" : "FILAMENTO",
            Nombre = item.Name,
            Tipo = item.Material,
            Color = item.Color,
            Existencia = item.StockQuantity,
            Unidad = item.IsResin ? "botellas" : "rollos",
            Estado = item.StockQuantity > 0 ? "DISPONIBLE" : "SIN EXISTENCIA"
        }).ToList();
        _inventory.Columns["Id"].Visible = false;
        _inventory.Columns["Sección"].MinimumWidth = 135; _inventory.Columns["Nombre"].MinimumWidth = 130;
        _inventory.Columns["Sección"].FillWeight = 85; _inventory.Columns["Nombre"].FillWeight = 125; _inventory.Columns["Tipo"].FillWeight = 80;
        _inventory.Columns["Color"].FillWeight = 95; _inventory.Columns["Existencia"].FillWeight = 80; _inventory.Columns["Unidad"].FillWeight = 80; _inventory.Columns["Estado"].FillWeight = 115;
    }

    private static Control GridSection(string title, string subtitle, DataGridView grid, Color accent)
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(4, 4, 4, 8) };
        var header = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
        var label = Theme.Label(title, 11, FontStyle.Bold, accent); label.Location = new Point(0, 0);
        var description = Theme.Label(subtitle, 8.5f, FontStyle.Regular, Theme.Muted); description.Location = new Point(0, 26);
        header.Controls.Add(label); header.Controls.Add(description); card.Controls.Add(grid); card.Controls.Add(header); return card;
    }

    private void Bind(DataGridView grid, IEnumerable<Filament> source)
    {
        grid.DataSource = source.Select(x => new { x.Id, Nombre = x.Name, Tipo = x.Material, Color = x.Color, Stock = x.StockQuantity, Precio = $"{x.PricePerUnit:0.00} {State.Settings.CurrencySymbol}", Predet = x.IsDefault ? "Sí" : "" }).ToList();
        grid.Columns["Id"].Visible = false;
    }

    private long SelectedId() => _activeGrid.CurrentRow is null ? 0 : Convert.ToInt64(_activeGrid.CurrentRow.Cells["Id"].Value);
    private long SelectedInventoryId() => _inventory.CurrentRow is null ? 0 : Convert.ToInt64(_inventory.CurrentRow.Cells["Id"].Value);
    private void Create() { try { var item = CatalogDialogs.FilamentDialog(this, null); if (item is null) return; State.Database.SaveFilament(item); State.NotifyChanged(); } catch (Exception ex) { ShowError(ex); } }
    private void EditSelected() { try { var current = _items.FirstOrDefault(x => x.Id == SelectedId()); if (current is null) { MessageBox.Show("Seleccione un consumible o resina para editar."); return; } var item = CatalogDialogs.FilamentDialog(this, current); if (item is null) return; State.Database.SaveFilament(item); State.NotifyChanged(); } catch (Exception ex) { ShowError(ex); } }
    private void Archive() { try { long id = SelectedId(); if (id == 0) { MessageBox.Show("Seleccione un consumible o resina para archivar."); return; } if (MessageBox.Show("¿Archivar la selección? El historial conservará sus datos.", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return; State.Database.ArchiveCatalogItem("filaments", id); State.NotifyChanged(); } catch (Exception ex) { ShowError(ex); } }
    private void UpdateStock() { try { Filament? current = _items.FirstOrDefault(item => item.Id == SelectedInventoryId()); if (current is null) { MessageBox.Show("Seleccione una fila del inventario."); return; } int? quantity = CatalogDialogs.StockDialog(this, current); if (!quantity.HasValue) return; State.Database.UpdateFilamentStock(current.Id, quantity.Value); State.NotifyChanged(); } catch (Exception ex) { ShowError(ex); } }
    private void AdjustStock(int change) { try { Filament? current = _items.FirstOrDefault(item => item.Id == SelectedInventoryId()); if (current is null) { MessageBox.Show("Seleccione una fila del inventario."); return; } int quantity = Math.Max(0, current.StockQuantity + change); State.Database.UpdateFilamentStock(current.Id, quantity); State.NotifyChanged(); } catch (Exception ex) { ShowError(ex); } }
    private static TabPage Tab(string title) => new(title) { BackColor = Theme.Background, ForeColor = Theme.Text, Padding = new Padding(8) };
}

internal sealed class MaterialsPage : CatalogPageBase
{
    private IReadOnlyList<ExtraMaterial> _items = Array.Empty<ExtraMaterial>();
    public MaterialsPage(AppState state) : base(state, "MATERIALES", "Registra consumibles, repuestos y servicios adicionales.", Theme.Purple)
    {
        Actions.Controls.Add(Theme.Button("+ Nuevo material", (_, _) => Create(), true, Theme.Purple)); Actions.Controls.Add(Theme.Button("Editar", (_, _) => EditSelected())); Actions.Controls.Add(Theme.Button("Archivar", (_, _) => Archive()));
    }
    public override void RefreshData() { _items = State.Database.GetMaterials(); Grid.DataSource = _items.Select(x => new { x.Id, Nombre = x.Name, Categoría = x.Category, Unidad = x.Unit, Precio = $"{x.UnitPrice:0.00} {State.Settings.CurrencySymbol}" }).ToList(); Grid.Columns["Id"].Visible = false; }
    private void Create() { try { var item = CatalogDialogs.MaterialDialog(this, null); if (item is null) return; State.Database.SaveMaterial(item); State.NotifyChanged(); } catch (Exception ex) { ShowError(ex); } }
    protected override void EditSelected() { try { var current = _items.FirstOrDefault(x => x.Id == SelectedId()); if (current is null) return; var item = CatalogDialogs.MaterialDialog(this, current); if (item is null) return; State.Database.SaveMaterial(item); State.NotifyChanged(); } catch (Exception ex) { ShowError(ex); } }
    private void Archive() { try { long id = SelectedId(); if (id == 0 || !ConfirmArchive("el material seleccionado")) return; State.Database.ArchiveCatalogItem("materials", id); State.NotifyChanged(); } catch (Exception ex) { ShowError(ex); } }
}
