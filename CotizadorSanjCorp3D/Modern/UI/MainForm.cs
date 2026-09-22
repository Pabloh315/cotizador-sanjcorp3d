using CotizadorSanjCorp3D.Services;
using CotizadorSanjCorp3D.UI.Pages;

namespace CotizadorSanjCorp3D.UI;

internal sealed class MainForm : Form
{
    private readonly AppState _state;
    private readonly Panel _content = new() { Dock = DockStyle.Fill, BackColor = Theme.Background };
    private readonly Label _pageTitle = Theme.Label("Cotizador", 18, FontStyle.Bold);
    private PageBase? _current;
    private Button? _selectedButton;

    public MainForm(AppState state)
    {
        _state = state;
        Text = "Cotizador 3D · SANJ CORP 3D";
        Icon = Branding.AppIcon;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = Theme.Font();
        MinimumSize = new Size(1180, 720);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        AutoScaleMode = AutoScaleMode.Dpi;
        Controls.Add(_content);
        Controls.Add(CreateHeader());
        Controls.Add(CreateSidebar());
        _state.DataChanged += (_, _) => _current?.RefreshData();
        Shown += (_, _) => Navigate("Cotizador", () => new QuotePage(_state), FindNavButton("Cotizador"));
    }

    private Control CreateHeader()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Theme.Sidebar, Padding = new Padding(24, 16, 24, 10) };
        _pageTitle.Dock = DockStyle.Left;
        var status = Theme.Label("●  Datos locales protegidos", 9, FontStyle.Regular, Theme.Green);
        status.Dock = DockStyle.Right;
        status.TextAlign = ContentAlignment.MiddleRight;
        header.Controls.Add(status);
        header.Controls.Add(_pageTitle);
        return header;
    }

    private Control CreateSidebar()
    {
        var sidebar = new Panel { Name = "Sidebar", Dock = DockStyle.Left, Width = 230, BackColor = Theme.Sidebar, Padding = new Padding(14) };
        var logo = new PictureBox
        {
            Name = "OfficialLogo",
            Dock = DockStyle.Top,
            Height = 155,
            Image = Branding.Logo,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Theme.Sidebar,
            AccessibleName = "Logo oficial de SANJ CORP TECHNOLOGY",
            Margin = Padding.Empty,
            Padding = new Padding(10)
        };
        sidebar.Controls.Add(logo);
        var menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 14, 0, 0),
            BackColor = Color.Transparent
        };
        AddNav(menu, "▦  Cotizador", "Cotizador", () => new QuotePage(_state));
        AddNav(menu, "▣  Impresoras", "Impresoras", () => new PrintersPage(_state));
        AddNav(menu, "◉  CONSUMIBLES", "Consumibles", () => new FilamentsPage(_state));
        AddNav(menu, "◇  Materiales", "Materiales", () => new MaterialsPage(_state));
        AddNav(menu, "▤  Historial", "Historial", () => new HistoryPage(_state));
        AddNav(menu, "▥  Reportes", "Reportes", () => new ReportsPage(_state));
        AddNav(menu, "⚙  Configuración", "Configuración", () => new SettingsPage(_state));
        AddNav(menu, "?  Ayuda", "Ayuda", () => new HelpPage(_state));
        sidebar.Controls.Add(menu);
        sidebar.Controls.SetChildIndex(logo, 1);
        return sidebar;
    }

    private void AddNav(Control parent, string text, string title, Func<PageBase> factory)
    {
        var button = Theme.Button(text, (_, _) => { }, false);
        button.Name = $"Nav{title}"; button.Width = 192; button.Height = 48; button.TextAlign = ContentAlignment.MiddleLeft;
        button.FlatAppearance.BorderSize = 0; button.BackColor = Theme.Sidebar; button.Font = Theme.Font(10.5f);
        parent.Controls.Add(button);
        button.Click += (_, _) => Navigate(title, factory, button);
    }

    private Button FindNavButton(string title) => Controls.Find($"Nav{title}", true).OfType<Button>().First();

    private void Navigate(string title, Func<PageBase> factory, Button selected)
    {
        _current?.Dispose();
        _content.Controls.Clear();
        _current = factory();
        _content.Controls.Add(_current);
        _pageTitle.Text = title;
        if (_selectedButton is not null) _selectedButton.BackColor = Theme.Sidebar;
        _selectedButton = selected;
        selected.BackColor = Color.FromArgb(28, 66, 112);
        _current.RefreshData();
    }
}
