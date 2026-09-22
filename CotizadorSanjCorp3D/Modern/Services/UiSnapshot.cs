using System.Drawing.Imaging;
using CotizadorSanjCorp3D.Domain;
using CotizadorSanjCorp3D.Infrastructure;
using CotizadorSanjCorp3D.UI;
using CotizadorSanjCorp3D.UI.Pages;

namespace CotizadorSanjCorp3D.Services;

internal static class UiSnapshot
{
    public static int Render(string outputDirectory)
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"sanjcorp3d-snapshot-{Guid.NewGuid():N}.db");
        try
        {
            Directory.CreateDirectory(outputDirectory);
            var database = new Database(databasePath);
            database.Initialize();
            AppSettings settings = database.GetSettings(); Printer printer = database.GetPrinters().First(); Filament filament = database.GetFilaments().First();
            var input = new QuoteInput("María Pérez", "Soporte de muestra", printer, 3.5m, 2, 5m, settings.DefaultProfitMultiplier, "Venta de muestra para revisión visual",
                new[] { new FilamentUsage(filament.Id, filament.Name, filament.Category, filament.Material, filament.Color, 85m, filament.PricePerUnit, filament.Density) }, Array.Empty<MaterialUsage>());
            SavedQuote saved = database.SaveQuote(input, CalculationService.Calculate(input, settings)); database.ConfirmSale(saved.Id);
            var state = new AppState(database);
            var pages = new Dictionary<string, Func<PageBase>>
            {
                ["cotizador"] = () => new QuotePage(state),
                ["ayuda"] = () => new HelpPage(state),
                ["impresoras"] = () => new PrintersPage(state),
                ["consumibles-catalogo"] = () => new FilamentsPage(state),
                ["consumibles-inventario"] = () => new FilamentsPage(state),
                ["materiales"] = () => new MaterialsPage(state),
                ["historial"] = () => new HistoryPage(state),
                ["reportes"] = () => new ReportsPage(state),
                ["configuracion"] = () => new SettingsPage(state)
            };
            foreach (var size in new[] { (Width: 1260, Height: 900, Suffix: ""), (Width: 960, Height: 720, Suffix: "-compacto") })
            {
                foreach (var item in pages)
                {
                    using PageBase page = item.Value();
                    using var host = new Form
                    {
                        ClientSize = new Size(size.Width, size.Height),
                        StartPosition = FormStartPosition.Manual,
                        Location = new Point(-32000, -32000),
                        ShowInTaskbar = false,
                        FormBorderStyle = FormBorderStyle.None
                    };
                    host.Controls.Add(page);
                    host.Show();
                    page.RefreshData();
                    TabControl? tabs = FindChild<TabControl>(page);
                    if (page is SettingsPage && tabs is not null && tabs.TabPages.Count > 1) tabs.SelectedIndex = 1;
                    if (item.Key == "consumibles-inventario" && tabs is not null && tabs.TabPages.Count > 1) tabs.SelectedIndex = 1;
                    page.AutoScrollPosition = Point.Empty;
                    host.PerformLayout();
                    Application.DoEvents();
                    page.AutoScrollPosition = Point.Empty;
                    using var bitmap = new Bitmap(host.ClientSize.Width, host.ClientSize.Height);
                    host.DrawToBitmap(bitmap, host.ClientRectangle);
                    bitmap.Save(Path.Combine(outputDirectory, $"{item.Key}{size.Suffix}.png"), ImageFormat.Png);
                    host.Hide();
                }
            }
            using (Form dialog = QuoteDialogs.CreateSavedDialog(saved.OrderCode))
            {
                dialog.StartPosition = FormStartPosition.Manual; dialog.Location = new Point(-32000, -32000); dialog.Show(); Application.DoEvents();
                using var bitmap = new Bitmap(dialog.Width, dialog.Height); dialog.DrawToBitmap(bitmap, dialog.ClientRectangle);
                bitmap.Save(Path.Combine(outputDirectory, "dialogo-codigo.png"), ImageFormat.Png); dialog.Hide();
            }
            using (var application = new MainForm(state)
            {
                WindowState = FormWindowState.Normal,
                ClientSize = new Size(1280, 800),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-32000, -32000),
                ShowInTaskbar = false,
                FormBorderStyle = FormBorderStyle.None
            })
            {
                application.Show();
                application.PerformLayout();
                Application.DoEvents();
                using var bitmap = new Bitmap(application.ClientSize.Width, application.ClientSize.Height);
                application.DrawToBitmap(bitmap, application.ClientRectangle);
                bitmap.Save(Path.Combine(outputDirectory, "aplicacion-logo.png"), ImageFormat.Png);
                application.Hide();
            }
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            foreach (string candidate in new[] { databasePath, databasePath + "-wal", databasePath + "-shm" })
                if (File.Exists(candidate)) File.Delete(candidate);
        }
    }

    private static T? FindChild<T>(Control parent) where T : Control
    {
        foreach (Control child in parent.Controls)
        {
            if (child is T match) return match;
            T? nested = FindChild<T>(child);
            if (nested is not null) return nested;
        }
        return null;
    }
}
