using CotizadorSanjCorp3D.Domain;
using CotizadorSanjCorp3D.Infrastructure;

namespace CotizadorSanjCorp3D.UI;

internal static class QuoteDialogs
{
    public static void ShowSaved(IWin32Window owner, string code)
    {
        using var dialog = CreateSavedDialog(code);
        dialog.ShowDialog(owner);
    }

    internal static Form CreateSavedDialog(string code)
    {
        var dialog = Dialog("Cotización guardada", new Size(570, 285));
        var title = Theme.Label("COTIZACIÓN GUARDADA", 15, FontStyle.Bold, Theme.Green);
        title.Location = new Point(28, 24);
        var help = Theme.Label("Selecciona el código o usa el botón para copiarlo.", 9.5f, FontStyle.Regular, Theme.Muted);
        help.Location = new Point(28, 65);
        var codeBox = Theme.TextBox(code);
        codeBox.ReadOnly = true; codeBox.Font = Theme.Font(15, FontStyle.Bold); codeBox.Location = new Point(28, 100); codeBox.Size = new Size(500, 44); codeBox.TextAlign = HorizontalAlignment.Center;
        var copy = Theme.Button("Copiar código", (_, _) =>
        {
            try { Clipboard.SetText(code); codeBox.SelectAll(); }
            catch (System.Runtime.InteropServices.ExternalException) { MessageBox.Show(dialog, "Windows no permitió acceder al portapapeles. El código quedó seleccionado para copiarlo con Ctrl+C.", "Portapapeles ocupado", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }, true, Theme.Green);
        var close = Theme.Button("Cerrar", (_, _) => dialog.Close());
        var actions = new TableLayoutPanel { Location = new Point(28, 164), Size = new Size(500, 58), ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        copy.Dock = DockStyle.Fill; copy.Margin = new Padding(6); close.Dock = DockStyle.Fill; close.Margin = new Padding(6);
        actions.Controls.Add(copy, 0, 0); actions.Controls.Add(close, 1, 0);
        dialog.Controls.Add(title); dialog.Controls.Add(help); dialog.Controls.Add(codeBox); dialog.Controls.Add(actions);
        dialog.Shown += (_, _) => { codeBox.Focus(); codeBox.SelectAll(); };
        return dialog;
    }

    public static long? Search(IWin32Window owner, Database database)
    {
        using var dialog = Dialog("Buscar cotización", new Size(880, 570));
        long? selectedId = null;
        IReadOnlyList<QuoteSummary> items = Array.Empty<QuoteSummary>();
        var title = Theme.Label("BUSCAR COTIZACIÓN", 15, FontStyle.Bold, Theme.Blue); title.Location = new Point(24, 20);
        var help = Theme.Label("Busca por código, nombre del cliente o proyecto.", 9.5f, FontStyle.Regular, Theme.Muted); help.Location = new Point(24, 58);
        var query = Theme.TextBox(); query.PlaceholderText = "Código, cliente o proyecto"; query.Location = new Point(24, 94); query.Width = 580;
        var grid = Theme.Grid(); grid.Dock = DockStyle.None; grid.Location = new Point(24, 150); grid.Size = new Size(816, 320);
        var search = Theme.Button("Buscar", (_, _) => Refresh(), true); search.Location = new Point(620, 90);
        var load = Theme.Button("Cargar cotización", (_, _) => SelectCurrent(), true, Theme.Green); load.Location = new Point(550, 488);
        var cancel = Theme.Button("Cancelar", (_, _) => dialog.Close()); cancel.Location = new Point(715, 488);

        void Refresh()
        {
            items = database.GetQuotes(query.Text);
            grid.DataSource = items.Select(item => new
            {
                item.Id,
                Código = item.OrderCode,
                Estado = item.IsSold ? "VENDIDA" : "COTIZACIÓN",
                Cliente = item.Customer,
                Proyecto = item.ProjectName,
                Fecha = item.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            }).ToList();
            if (grid.Columns.Contains("Id")) grid.Columns["Id"].Visible = false;
            if (grid.Columns.Contains("Código")) grid.Columns["Código"].MinimumWidth = 185;
        }

        void SelectCurrent()
        {
            if (grid.SelectedRows.Count == 0) return;
            long id = Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
            if (items.All(item => item.Id != id)) return;
            selectedId = id; dialog.DialogResult = DialogResult.OK; dialog.Close();
        }

        query.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Refresh(); } };
        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) SelectCurrent(); };
        dialog.Controls.Add(title); dialog.Controls.Add(help); dialog.Controls.Add(query); dialog.Controls.Add(search); dialog.Controls.Add(grid); dialog.Controls.Add(load); dialog.Controls.Add(cancel);
        dialog.Shown += (_, _) => { Refresh(); query.Focus(); };
        return dialog.ShowDialog(owner) == DialogResult.OK ? selectedId : null;
    }

    private static Form Dialog(string title, Size size) => new()
    {
        Text = title,
        ClientSize = size,
        BackColor = Theme.Background,
        ForeColor = Theme.Text,
        Font = Theme.Font(),
        FormBorderStyle = FormBorderStyle.FixedDialog,
        MaximizeBox = false,
        MinimizeBox = false,
        ShowInTaskbar = false,
        StartPosition = FormStartPosition.CenterParent,
        AutoScaleMode = AutoScaleMode.Dpi
    };
}
