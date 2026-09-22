using Microsoft.Win32;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SanjCorp3D.Installer;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
            return InstallerServices.SelfTest();

        ApplicationConfiguration.Initialize();
        int snapshotIndex = Array.FindIndex(args, value => value.Equals("--snapshot-path", StringComparison.OrdinalIgnoreCase));
        if (snapshotIndex >= 0 && snapshotIndex + 1 < args.Length)
            return RenderSnapshot(args[snapshotIndex + 1]);
        bool uninstall = args.Contains("--uninstall", StringComparer.OrdinalIgnoreCase);
        Application.Run(new SetupForm(uninstall));
        return 0;
    }

    private static int RenderSnapshot(string path)
    {
        try
        {
            using var form = new SetupForm(false)
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-32000, -32000),
                ShowInTaskbar = false
            };
            form.Show();
            form.PerformLayout();
            Application.DoEvents();
            using var bitmap = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            form.Hide();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}

internal sealed class SetupForm : Form
{
    private readonly bool _uninstall;
    private readonly Button _actionButton = new();
    private readonly CheckBox _desktopShortcut = new();
    private readonly Label _status = new();

    public SetupForm(bool uninstall)
    {
        _uninstall = uninstall;
        Text = uninstall ? "Desinstalar SANJ CORP 3D" : "Instalar SANJ CORP 3D";
        Icon = InstallerBranding.AppIcon;
        ClientSize = new Size(640, 570);
        MinimumSize = new Size(590, 540);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(7, 19, 37);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var logo = new PictureBox
        {
            Image = InstallerBranding.Logo,
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Top,
            Height = 250,
            BackColor = Color.FromArgb(7, 19, 37),
            AccessibleName = "Logo oficial de SANJ CORP TECHNOLOGY"
        };
        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(38, 20, 38, 28) };
        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 42,
            Text = uninstall ? "Desinstalar Cotizador 3D" : "Cotizador 3D · Versión 1.0.0",
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(61, 230, 130),
            TextAlign = ContentAlignment.MiddleCenter
        };
        var description = new Label
        {
            Dock = DockStyle.Top,
            Height = 68,
            Text = uninstall
                ? "Se quitará la aplicación y sus accesos directos. Tus cotizaciones y ajustes locales se conservarán."
                : $"La aplicación se instalará para este usuario en:\n{InstallerServices.InstallDirectory}",
            ForeColor = Color.FromArgb(191, 209, 232),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _desktopShortcut.Dock = DockStyle.Top;
        _desktopShortcut.Height = 34;
        _desktopShortcut.Text = "Crear acceso directo en el escritorio";
        _desktopShortcut.Checked = true;
        _desktopShortcut.ForeColor = Color.White;
        _desktopShortcut.Visible = !uninstall;

        _status.Dock = DockStyle.Top;
        _status.Height = 32;
        _status.TextAlign = ContentAlignment.MiddleCenter;
        _status.ForeColor = Color.FromArgb(191, 209, 232);

        _actionButton.Dock = DockStyle.Bottom;
        _actionButton.Height = 50;
        _actionButton.Text = uninstall ? "Desinstalar" : "Instalar ahora";
        _actionButton.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _actionButton.BackColor = uninstall ? Color.FromArgb(190, 65, 65) : Color.FromArgb(46, 139, 246);
        _actionButton.ForeColor = Color.White;
        _actionButton.FlatStyle = FlatStyle.Flat;
        _actionButton.FlatAppearance.BorderSize = 0;
        _actionButton.Click += ActionButtonClick;

        content.Controls.Add(_actionButton);
        content.Controls.Add(_status);
        content.Controls.Add(_desktopShortcut);
        content.Controls.Add(description);
        content.Controls.Add(title);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 250F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(logo, 0, 0);
        layout.Controls.Add(content, 0, 1);
        Controls.Add(layout);
    }

    private async void ActionButtonClick(object? sender, EventArgs eventArgs)
    {
        if (InstallerServices.IsApplicationRunning())
        {
            MessageBox.Show("Cierra Cotizador SANJ CORP 3D antes de continuar.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _actionButton.Enabled = false;
        _desktopShortcut.Enabled = false;
        _status.Text = _uninstall ? "Desinstalando…" : "Instalando…";
        try
        {
            if (_uninstall)
            {
                await Task.Run(InstallerServices.Uninstall);
                MessageBox.Show("La aplicación fue desinstalada. Tus datos locales se conservaron.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                bool desktopShortcut = _desktopShortcut.Checked;
                await Task.Run(() => InstallerServices.Install(desktopShortcut));
                var result = MessageBox.Show("Instalación completada correctamente.\n\n¿Deseas abrir el cotizador ahora?", Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(InstallerServices.AppPath) { UseShellExecute = true });
            }
            Close();
        }
        catch (Exception exception)
        {
            _status.Text = "No se pudo completar la operación.";
            MessageBox.Show($"Ocurrió un error:\n\n{exception.Message}", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _actionButton.Enabled = true;
            _desktopShortcut.Enabled = true;
        }
    }
}

internal static class InstallerServices
{
    private const string AppResource = "SanjCorp3D.Payload.CotizadorSanjCorp3D.exe";
    private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\SanjCorp3D";
    private const int MoveFileDelayUntilReboot = 0x4;

    public static string InstallDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "SanjCorp3D");
    public static string AppPath => Path.Combine(InstallDirectory, "CotizadorSanjCorp3D.exe");
    private static string UninstallerPath => Path.Combine(InstallDirectory, "Uninstall.exe");
    private static string StartMenuShortcut => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "SANJ CORP 3D.lnk");
    private static string DesktopShortcut => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "SANJ CORP 3D.lnk");

    public static int SelfTest()
    {
        string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
        bool valid = resources.Contains(AppResource, StringComparer.Ordinal)
            && resources.Contains(InstallerBranding.LogoResource, StringComparer.Ordinal)
            && Assembly.GetExecutingAssembly().GetManifestResourceStream(AppResource)?.Length > 0;
        Console.WriteLine(valid ? "Installer self-test OK" : "Installer self-test FAILED");
        return valid ? 0 : 1;
    }

    public static bool IsApplicationRunning() =>
        System.Diagnostics.Process.GetProcessesByName("CotizadorSanjCorp3D").Length > 0;

    public static void Install(bool createDesktopShortcut)
    {
        Directory.CreateDirectory(InstallDirectory);
        string temporaryApp = AppPath + ".new";
        using (Stream source = Assembly.GetExecutingAssembly().GetManifestResourceStream(AppResource)
            ?? throw new InvalidOperationException("El instalador no contiene la aplicación."))
        using (var destination = new FileStream(temporaryApp, FileMode.Create, FileAccess.Write, FileShare.None))
            source.CopyTo(destination);
        File.Move(temporaryApp, AppPath, true);

        File.Copy(Environment.ProcessPath ?? throw new InvalidOperationException("No se pudo localizar el instalador."),
            UninstallerPath, true);
        Shortcut.Create(StartMenuShortcut, AppPath, InstallDirectory, "Cotizador SANJ CORP 3D", AppPath);
        if (createDesktopShortcut)
            Shortcut.Create(DesktopShortcut, AppPath, InstallDirectory, "Cotizador SANJ CORP 3D", AppPath);
        else if (File.Exists(DesktopShortcut))
            File.Delete(DesktopShortcut);

        using RegistryKey key = Registry.CurrentUser.CreateSubKey(UninstallKey, true);
        key.SetValue("DisplayName", "Cotizador SANJ CORP 3D");
        key.SetValue("DisplayVersion", "1.0.0");
        key.SetValue("Publisher", "SANJ CORP TECHNOLOGY");
        key.SetValue("InstallLocation", InstallDirectory);
        key.SetValue("DisplayIcon", AppPath);
        key.SetValue("UninstallString", $"\"{UninstallerPath}\" --uninstall");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    public static void Uninstall()
    {
        if (File.Exists(AppPath)) File.Delete(AppPath);
        if (File.Exists(StartMenuShortcut)) File.Delete(StartMenuShortcut);
        if (File.Exists(DesktopShortcut)) File.Delete(DesktopShortcut);
        Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false);

        string? runningPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(runningPath)) MoveFileEx(runningPath, null, MoveFileDelayUntilReboot);
        MoveFileEx(InstallDirectory, null, MoveFileDelayUntilReboot);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool MoveFileEx(string existingFileName, string? newFileName, int flags);
}

internal static class InstallerBranding
{
    internal const string LogoResource = "SanjCorp3D.Assets.logo-oficial.jpeg";
    public static Image Logo { get; } = LoadLogo();
    public static Icon AppIcon { get; } = CreateIcon();

    private static Image LoadLogo()
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LogoResource)
            ?? throw new InvalidOperationException("No se encontró el logo oficial.");
        using var source = Image.FromStream(stream);
        return new Bitmap(source);
    }

    private static Icon CreateIcon()
    {
        using var bitmap = new Bitmap(64, 64);
        using (Graphics graphics = Graphics.FromImage(bitmap))
            graphics.DrawImage(Logo, new Rectangle(0, 0, 64, 64));
        IntPtr handle = bitmap.GetHicon();
        try
        {
            using Icon temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally { DestroyIcon(handle); }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}

internal static class Shortcut
{
    public static void Create(string shortcutPath, string targetPath, string workingDirectory, string description, string iconPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)
            ?? throw new InvalidOperationException("Ruta de acceso directo inválida."));
        var link = (IShellLinkW)(object)new ShellLink();
        link.SetPath(targetPath);
        link.SetWorkingDirectory(workingDirectory);
        link.SetDescription(description);
        link.SetIconLocation(iconPath, 0);
        ((IPersistFile)link).Save(shortcutPath, true);
        Marshal.FinalReleaseComObject(link);
    }

    [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
    private sealed class ShellLink { }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder file, int maximumPath, IntPtr findData, int flags);
        void GetIDList(out IntPtr itemIdList);
        void SetIDList(IntPtr itemIdList);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder name, int maximumName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string name);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder directory, int maximumPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string directory);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder arguments, int maximumPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string arguments);
        void GetHotkey(out short hotkey);
        void SetHotkey(short hotkey);
        void GetShowCmd(out int showCommand);
        void SetShowCmd(int showCommand);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder iconPath, int maximumPath, out int iconIndex);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string iconPath, int iconIndex);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path, int reserved);
        void Resolve(IntPtr window, int flags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("0000010B-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid classId);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string fileName, uint mode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string fileName, bool remember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string fileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string fileName);
    }
}
