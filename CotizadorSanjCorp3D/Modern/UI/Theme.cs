using System.Drawing.Drawing2D;

namespace CotizadorSanjCorp3D.UI;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(10, 18, 30);
    public static readonly Color Sidebar = Color.FromArgb(13, 24, 39);
    public static readonly Color Card = Color.FromArgb(20, 32, 49);
    public static readonly Color CardAlt = Color.FromArgb(27, 41, 60);
    public static readonly Color Border = Color.FromArgb(51, 70, 94);
    public static readonly Color Text = Color.FromArgb(235, 241, 248);
    public static readonly Color Muted = Color.FromArgb(163, 178, 197);
    public static readonly Color Blue = Color.FromArgb(47, 139, 255);
    public static readonly Color Green = Color.FromArgb(76, 218, 120);
    public static readonly Color Purple = Color.FromArgb(190, 103, 255);
    public static readonly Color Orange = Color.FromArgb(255, 159, 50);
    public static readonly Color Danger = Color.FromArgb(239, 82, 96);

    public static Font Font(float size = 10f, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style);

    public static Button Button(string text, EventHandler onClick, bool primary = false, Color? accent = null)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(110, 38),
            Padding = new Padding(12, 4, 12, 4),
            FlatStyle = FlatStyle.Flat,
            Font = Font(10, FontStyle.Bold),
            Cursor = Cursors.Hand,
            BackColor = primary ? accent ?? Blue : CardAlt,
            ForeColor = Text,
            TabStop = true
        };
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = primary ? button.BackColor : Border;
        button.Click += onClick;
        return button;
    }

    public static Label Label(string text, float size = 10, FontStyle style = FontStyle.Regular, Color? color = null)
        => new() { Text = text, AutoSize = true, Font = Font(size, style), ForeColor = color ?? Text, BackColor = Color.Transparent };

    public static TextBox TextBox(string value = "") => new()
    {
        Text = value,
        Font = Font(),
        BackColor = CardAlt,
        ForeColor = Text,
        BorderStyle = BorderStyle.FixedSingle,
        MinimumSize = new Size(160, 30),
        Margin = new Padding(3, 4, 3, 8)
    };

    public static NumericUpDown Number(decimal value = 0, decimal maximum = 1000000, int decimals = 2, decimal increment = 0.1m)
        => new()
        {
            Minimum = 0,
            Maximum = maximum,
            Value = Math.Min(value, maximum),
            DecimalPlaces = decimals,
            Increment = increment,
            ThousandsSeparator = true,
            Font = Font(),
            BackColor = CardAlt,
            ForeColor = Text,
            BorderStyle = BorderStyle.FixedSingle,
            MinimumSize = new Size(120, 30),
            Margin = new Padding(3, 4, 3, 8)
        };

    public static ComboBox Combo() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Font = Font(),
        BackColor = CardAlt,
        ForeColor = Text,
        FlatStyle = FlatStyle.Flat,
        MinimumSize = new Size(170, 30),
        Margin = new Padding(3, 4, 3, 8)
    };

    public static DataGridView Grid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Card,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            GridColor = Border,
            Font = Font(),
            ForeColor = Text,
            ColumnHeadersHeight = 38,
            RowTemplate = { Height = 36 }
        };
        grid.DefaultCellStyle.BackColor = Card;
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(34, 76, 126);
        grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.BackColor = CardAlt;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
        grid.ColumnHeadersDefaultCellStyle.Font = Font(9, FontStyle.Bold);
        return grid;
    }

    public static TableLayoutPanel FormGrid(int columns = 2) => new()
    {
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Dock = DockStyle.Top,
        ColumnCount = columns,
        Padding = new Padding(0),
        BackColor = Color.Transparent
    };

    public static void AddField(TableLayoutPanel grid, string label, Control control, int column = 0)
    {
        int row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 4, 12, 4),
            BackColor = Color.Transparent
        };
        panel.Controls.Add(Label(label, 9, FontStyle.Regular, Muted));
        control.Width = Math.Max(control.Width, 190);
        panel.Controls.Add(control);
        grid.Controls.Add(panel, column, row);
    }
}

internal sealed class CardPanel : Panel
{
    public CardPanel()
    {
        BackColor = Theme.Card;
        Padding = new Padding(18);
        Margin = new Padding(8);
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Theme.Border);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}
