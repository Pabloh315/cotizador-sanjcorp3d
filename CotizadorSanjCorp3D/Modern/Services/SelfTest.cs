using CotizadorSanjCorp3D.Domain;
using CotizadorSanjCorp3D.Infrastructure;
using CotizadorSanjCorp3D.UI;
using CotizadorSanjCorp3D.UI.Pages;

namespace CotizadorSanjCorp3D.Services;

internal static class SelfTest
{
    public static int Run()
    {
        string path = Path.Combine(Path.GetTempPath(), $"sanjcorp3d-selftest-{Guid.NewGuid():N}.db");
        string backupPath = Path.ChangeExtension(path, ".backup.db");
        string csvPath = Path.ChangeExtension(path, ".csv");
        string salesCsvPath = Path.Combine(Path.GetDirectoryName(path)!, $"{Path.GetFileNameWithoutExtension(path)}-sales.csv");
        try
        {
            var database = new Database(path);
            database.Initialize();
            AppSettings settings = database.GetSettings();
            database.SaveSettings(settings with { BusinessName = "SANJ CORP 3D TEST" });
            if (database.GetSettings().BusinessName != "SANJ CORP 3D TEST")
                throw new InvalidOperationException("La configuración no se actualizó.");
            Printer printer = database.GetPrinters().First();
            Filament filament = database.GetFilaments().First();
            database.UpdateFilamentStock(filament.Id, 0);
            if (database.GetFilaments().Single(item => item.Id == filament.Id).StockQuantity != 0)
                throw new InvalidOperationException("El inventario no registró la falta de existencia.");
            database.UpdateFilamentStock(filament.Id, 2);
            if (database.GetFilaments().Single(item => item.Id == filament.Id).StockQuantity != 2)
                throw new InvalidOperationException("El inventario no actualizó la cantidad de rollos.");
            long testPrinterId = database.SavePrinter(new Printer(0, "Impresora de prueba", 200, 200, 200, 0.4m, 150, 300, 3m, false));
            database.SavePrinter(new Printer(testPrinterId, "Impresora de prueba editada", 210, 210, 210, 0.6m, 160, 320, 3.5m, false));
            if (database.GetPrinters().All(item => item.Name != "Impresora de prueba editada"))
                throw new InvalidOperationException("El catálogo de impresoras no se actualizó.");
            using (var owner = new Form { Location = new Point(-32000, -32000), ShowInTaskbar = false })
            {
                owner.Show();
                Printer? dialogPrinter = RunDialog(owner, () => CatalogDialogs.PrinterDialog(owner, null), "Impresora desde diálogo");
                if (dialogPrinter is null) throw new InvalidOperationException("El diálogo de nueva impresora no devolvió datos.");
                long dialogPrinterId = database.SavePrinter(dialogPrinter);
                Printer sourcePrinter = database.GetPrinters().First(item => item.Id == dialogPrinterId);
                Printer? editedPrinter = RunDialog(owner, () => CatalogDialogs.PrinterDialog(owner, sourcePrinter), "Impresora editada desde diálogo");
                if (editedPrinter is null) throw new InvalidOperationException("El diálogo de edición de impresora no devolvió datos.");
                database.SavePrinter(editedPrinter);

                Filament? dialogFilament = RunDialog(owner, () => CatalogDialogs.FilamentDialog(owner, null), "Consumible desde diálogo", "Rojo");
                if (dialogFilament is null) throw new InvalidOperationException("El diálogo de nuevo consumible no devolvió datos.");
                long dialogFilamentId = database.SaveFilament(dialogFilament);
                Filament sourceFilament = database.GetFilaments().First(item => item.Id == dialogFilamentId);
                Filament? editedFilament = RunDialog(owner, () => CatalogDialogs.FilamentDialog(owner, sourceFilament), "Consumible editado desde diálogo", "Negro");
                if (editedFilament is null) throw new InvalidOperationException("El diálogo de edición de consumible no devolvió datos.");
                database.SaveFilament(editedFilament);
                owner.Hide();
            }
            long resinId = database.SaveFilament(new Filament(0, "Resina de prueba", "Resina", "SLA", "Gris", 210m, 1.1m, false));
            if (database.GetFilaments().All(item => item.Id != resinId || !item.IsResin))
                throw new InvalidOperationException("La resina no se guardó en su sección.");
            ExtraMaterial materialToArchive = database.GetMaterials().First();
            database.ArchiveCatalogItem("materials", materialToArchive.Id);
            if (database.GetMaterials().Any(item => item.Id == materialToArchive.Id))
                throw new InvalidOperationException("El material no se archivó.");
            var input = new QuoteInput("Cliente de prueba", "Engranaje", printer, 8.5m, 2, 3m,
                settings.DefaultProfitMultiplier, "Prueba automática",
                new[] { new FilamentUsage(filament.Id, filament.Name, filament.Category, filament.Material, filament.Color, 125.5m, filament.PricePerUnit, filament.Density) },
                Array.Empty<MaterialUsage>());
            QuoteCalculation calculation = CalculationService.Calculate(input, settings);
            if (calculation.RecommendedPrice <= calculation.Subtotal || calculation.TotalWeight != 251m || calculation.MachineCost != 0m)
                throw new InvalidOperationException("El cálculo no produjo los valores esperados.");
            SavedQuote saved = database.SaveQuote(input, calculation);
            if (!System.Text.RegularExpressions.Regex.IsMatch(saved.OrderCode, "^[A-Z0-9]{2}[0-9]{12,}$"))
                throw new InvalidOperationException("El código no respeta inicial de cliente, inicial de impresora, fecha y correlativo.");
            SavedQuote nextQuote = database.SaveQuote(input with { ProjectName = "Engranaje correlativo" }, calculation);
            if (!nextQuote.OrderCode.EndsWith("0002", StringComparison.Ordinal) || nextQuote.OrderCode == saved.OrderCode)
                throw new InvalidOperationException("El número correlativo de la cotización no avanzó.");
            database.DeleteQuote(nextQuote.Id);
            if (database.GetQuotes(saved.OrderCode).Count != 1 || database.GetQuotes(input.Customer).Count != 1 || database.GetQuotes(input.ProjectName).Count != 1)
                throw new InvalidOperationException("La cotización no se guardó o recuperó correctamente.");
            QuoteDetails? details = database.GetQuote(saved.Id);
            if (details is null || details.Customer != input.Customer || details.ProjectName != input.ProjectName || details.Filaments.Count != 1 || details.Quantity != input.Quantity || details.Filaments[0].Material != filament.Material || details.Filaments[0].Color != filament.Color)
                throw new InvalidOperationException("La recuperación completa de la cotización no conservó todos sus datos.");
            using (var owner = new Form { Location = new Point(-32000, -32000), ShowInTaskbar = false })
            {
                owner.Show();
                if (RunQuoteSearchDialog(owner, database) != saved.Id)
                    throw new InvalidOperationException("El diálogo de búsqueda no devolvió la cotización seleccionada.");
                if (!InspectSavedCodeDialog(owner, saved.OrderCode))
                    throw new InvalidOperationException("El diálogo de guardado no mostró el código en un campo copiable.");
                owner.Hide();
            }
            if (!database.ConfirmSale(saved.Id) || database.ConfirmSale(saved.Id))
                throw new InvalidOperationException("La venta no se confirmó una única vez.");
            if (!database.GetQuotes(saved.OrderCode).Single().IsSold)
                throw new InvalidOperationException("La cotización confirmada no figura como vendida.");
            ReportSummary report = database.GetReport(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));
            if (report.QuoteCount != 1 || report.SaleCount != 1 || report.SalesRevenue != calculation.RecommendedPrice)
                throw new InvalidOperationException("El reporte no contabilizó la cotización y su venta.");
            CsvExport.Quotes(csvPath, database.GetQuotes(), settings.CurrencySymbol);
            if (!File.Exists(csvPath) || new FileInfo(csvPath).Length == 0)
                throw new InvalidOperationException("La exportación CSV no generó contenido.");
            CsvExport.Sales(salesCsvPath, database.GetSales(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1)), settings.CurrencySymbol);
            if (!File.Exists(salesCsvPath) || new FileInfo(salesCsvPath).Length == 0)
                throw new InvalidOperationException("La exportación de ventas no generó contenido.");
            database.BackupTo(backupPath);
            if (!File.Exists(backupPath)) throw new InvalidOperationException("No se creó el respaldo SQLite.");
            var state = new AppState(database);
            PageBase[] pages =
            {
                new QuotePage(state), new PrintersPage(state), new FilamentsPage(state), new MaterialsPage(state),
                new HistoryPage(state), new ReportsPage(state), new SettingsPage(state), new HelpPage(state)
            };
            foreach (PageBase page in pages)
            {
                page.CreateControl();
                page.RefreshData();
                page.Dispose();
            }
            Console.WriteLine($"SELF-TEST OK | {saved.OrderCode} | {calculation.RecommendedPrice} {settings.CurrencySymbol}");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"SELF-TEST FAILED: {exception}");
            return 1;
        }
        finally
        {
            foreach (string candidate in new[] { path, path + "-wal", path + "-shm", backupPath, csvPath, salesCsvPath })
            {
                if (File.Exists(candidate)) File.Delete(candidate);
            }
        }
    }

    private static T? RunDialog<T>(Form owner, Func<T?> openDialog, params string[] textValues)
    {
        using var timer = new System.Windows.Forms.Timer { Interval = 100 };
        timer.Tick += (_, _) =>
        {
            Form? dialog = Application.OpenForms.Cast<Form>().FirstOrDefault(form => !ReferenceEquals(form, owner));
            if (dialog is null) return;
            TextBox[] boxes = FindChildren<TextBox>(dialog).ToArray();
            for (int index = 0; index < Math.Min(boxes.Length, textValues.Length); index++) boxes[index].Text = textValues[index];
            Button? save = FindChildren<Button>(dialog).FirstOrDefault(button => button.Text == "Guardar");
            if (save is null) return;
            timer.Stop();
            save.PerformClick();
        };
        timer.Start();
        return openDialog();
    }

    private static long? RunQuoteSearchDialog(Form owner, Database database)
    {
        using var timer = new System.Windows.Forms.Timer { Interval = 100 };
        timer.Tick += (_, _) =>
        {
            Form? dialog = Application.OpenForms.Cast<Form>().FirstOrDefault(form => !ReferenceEquals(form, owner));
            DataGridView? grid = dialog is null ? null : FindChildren<DataGridView>(dialog).FirstOrDefault();
            Button? load = dialog is null ? null : FindChildren<Button>(dialog).FirstOrDefault(button => button.Text == "Cargar cotización");
            if (grid is null || load is null || grid.Rows.Count == 0) return;
            grid.Rows[0].Selected = true; timer.Stop(); load.PerformClick();
        };
        timer.Start();
        return QuoteDialogs.Search(owner, database);
    }

    private static bool InspectSavedCodeDialog(Form owner, string expectedCode)
    {
        bool valid = false;
        using var timer = new System.Windows.Forms.Timer { Interval = 100 };
        timer.Tick += (_, _) =>
        {
            Form? dialog = Application.OpenForms.Cast<Form>().FirstOrDefault(form => !ReferenceEquals(form, owner));
            TextBox? code = dialog is null ? null : FindChildren<TextBox>(dialog).FirstOrDefault(box => box.ReadOnly);
            Button? close = dialog is null ? null : FindChildren<Button>(dialog).FirstOrDefault(button => button.Text == "Cerrar");
            if (code is null || close is null) return;
            valid = code.Text == expectedCode && FindChildren<Button>(dialog!).Any(button => button.Text == "Copiar código");
            timer.Stop(); close.PerformClick();
        };
        timer.Start();
        QuoteDialogs.ShowSaved(owner, expectedCode);
        return valid;
    }

    private static IEnumerable<T> FindChildren<T>(Control parent) where T : Control
    {
        foreach (Control child in parent.Controls)
        {
            if (child is T match) yield return match;
            foreach (T nested in FindChildren<T>(child)) yield return nested;
        }
    }
}
