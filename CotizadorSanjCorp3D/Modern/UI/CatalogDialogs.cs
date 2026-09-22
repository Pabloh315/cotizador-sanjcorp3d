using CotizadorSanjCorp3D.Domain;

namespace CotizadorSanjCorp3D.UI;

internal static class CatalogDialogs
{
    public static Printer? PrinterDialog(IWin32Window owner, Printer? source)
    {
        var name = Theme.TextBox(source?.Name ?? ""); var x = Theme.Number(source?.BuildX ?? 220, 1000, 0, 1); var y = Theme.Number(source?.BuildY ?? 220, 1000, 0, 1);
        var z = Theme.Number(source?.BuildZ ?? 250, 1000, 0, 1); var nozzle = Theme.Number(source?.Nozzle ?? 0.4m, 10, 2, 0.1m); var speed = Theme.Number(source?.Speed ?? 180, 1000, 0, 5);
        var watts = Theme.Number(source?.PowerWatts ?? 350, 5000, 0, 10);
        var preferred = new CheckBox { Text = "Usar como impresora predeterminada", Checked = source?.IsDefault ?? false, AutoSize = true, ForeColor = Theme.Text };
        if (!Show(owner, source is null ? "Nueva impresora" : "Editar impresora", new[] { ("Nombre", (Control)name), ("Volumen X (mm)", x), ("Volumen Y (mm)", y), ("Volumen Z (mm)", z), ("Boquilla (mm)", nozzle), ("Velocidad (mm/s)", speed), ("Potencia eléctrica (W)", watts), ("", preferred) })) return null;
        if (string.IsNullOrWhiteSpace(name.Text)) { MessageBox.Show("Ingrese el nombre de la impresora."); return null; }
        return new(source?.Id ?? 0, name.Text.Trim(), x.Value, y.Value, z.Value, nozzle.Value, speed.Value, watts.Value, 0m, preferred.Checked, true);
    }

    public static Filament? FilamentDialog(IWin32Window owner, Filament? source)
    {
        var name = Theme.TextBox(source?.Name ?? "");
        var category = Theme.Combo(); category.Items.AddRange(new object[] { "Consumible", "Resina" }); category.SelectedItem = source?.Category ?? "Consumible";
        var type = Theme.Combo();
        void PopulateTypes()
        {
            string? selected = source?.Material;
            type.Items.Clear();
            type.Items.AddRange(category.Text == "Resina"
                ? new object[] { "SLA", "DLP", "MSLA", "Resina estándar", "Resina lavable", "Resina ABS-like", "Otra" }
                : new object[] { "PLA", "PLA RAPID", "PLA SILK", "PLA PRO", "PETG", "PETG RAPID", "PETG PRO", "TPU", "ABS", "ASA", "Otro" });
            if (selected is not null && type.Items.Contains(selected)) type.SelectedItem = selected;
            else type.SelectedIndex = 0;
        }
        PopulateTypes(); category.SelectedIndexChanged += (_, _) => PopulateTypes();
        var color = Theme.TextBox(source?.Color ?? ""); var price = Theme.Number(source?.PricePerUnit ?? 145, 100000, 2, 1); var density = Theme.Number(source?.Density ?? 1.24m, 10, 2, 0.01m);
        var preferred = new CheckBox { Text = "Usar como consumible predeterminado", Checked = source?.IsDefault ?? false, AutoSize = true, ForeColor = Theme.Text };
        if (!Show(owner, source is null ? "Nuevo consumible o resina" : "Editar consumible o resina", new[] { ("Sección", (Control)category), ("Nombre", name), ("Tipo", type), ("Color", color), ("Precio por kg (consumible) o litro (resina)", price), ("Densidad (g/cm³)", density), ("", preferred) })) return null;
        if (string.IsNullOrWhiteSpace(name.Text) || string.IsNullOrWhiteSpace(color.Text)) { MessageBox.Show("Nombre y color son obligatorios."); return null; }
        return new(source?.Id ?? 0, name.Text.Trim(), category.Text, type.Text, color.Text.Trim(), price.Value, density.Value, preferred.Checked, true, source?.StockQuantity ?? 0);
    }

    public static int? StockDialog(IWin32Window owner, Filament source)
    {
        var quantity = Theme.Number(source.StockQuantity, 100000, 0, 1);
        string unit = source.IsResin ? "botellas" : "rollos";
        if (!Show(owner, $"Inventario · {source.Name}", new[] { ($"Cantidad de {unit} en existencia", (Control)quantity) })) return null;
        return decimal.ToInt32(quantity.Value);
    }

    public static ExtraMaterial? MaterialDialog(IWin32Window owner, ExtraMaterial? source)
    {
        var name = Theme.TextBox(source?.Name ?? ""); var category = Theme.Combo(); category.Items.AddRange(new object[] { "Consumible", "Repuesto", "Acabado", "Empaque", "Servicio", "Otro" });
        category.SelectedItem = source?.Category; if (category.SelectedIndex < 0) category.SelectedIndex = 0;
        var unit = Theme.Combo(); unit.Items.AddRange(new object[] { "unidad", "g", "kg", "ml", "litro", "metro", "hora" }); unit.SelectedItem = source?.Unit; if (unit.SelectedIndex < 0) unit.SelectedIndex = 0;
        var price = Theme.Number(source?.UnitPrice ?? 0, 100000, 2, 0.5m);
        if (!Show(owner, source is null ? "Nuevo material" : "Editar material", new[] { ("Nombre", (Control)name), ("Categoría", category), ("Unidad", unit), ("Precio por unidad", price) })) return null;
        if (string.IsNullOrWhiteSpace(name.Text)) { MessageBox.Show("Ingrese el nombre del material."); return null; }
        return new(source?.Id ?? 0, name.Text.Trim(), category.Text, unit.Text, price.Value, true);
    }

    private static bool Show(IWin32Window owner, string title, IReadOnlyList<(string Label, Control Control)> fields)
    {
        using var form = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            BackColor = Theme.Background,
            ForeColor = Theme.Text,
            Font = Theme.Font(),
            ClientSize = new Size(500, Math.Min(720, Math.Max(300, fields.Count * 68 + 110))),
            MinimumSize = new Size(500, 300),
            Padding = new Padding(18)
        };
        var body = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent };
        foreach (var field in fields)
        {
            var panel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Width = 440, Margin = new Padding(0, 4, 0, 8) };
            if (!string.IsNullOrEmpty(field.Label)) panel.Controls.Add(Theme.Label(field.Label, 9, FontStyle.Regular, Theme.Muted));
            field.Control.Width = 420; panel.Controls.Add(field.Control); body.Controls.Add(panel);
        }
        var actions = new FlowLayoutPanel { Height = 58, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Padding = new Padding(0, 10, 0, 0) };
        var cancel = Theme.Button("Cancelar", (_, _) => { }); cancel.DialogResult = DialogResult.Cancel;
        var save = Theme.Button("Guardar", (_, _) => { }, true); save.DialogResult = DialogResult.OK;
        actions.Controls.Add(save); actions.Controls.Add(cancel); form.Controls.Add(body); form.Controls.Add(actions); form.AcceptButton = save; form.CancelButton = cancel;
        return form.ShowDialog(owner) == DialogResult.OK;
    }
}
