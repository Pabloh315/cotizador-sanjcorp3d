using CotizadorSanjCorp3D.Infrastructure;
using CotizadorSanjCorp3D.Services;
using CotizadorSanjCorp3D.UI;

namespace CotizadorSanjCorp3D;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
            return SelfTest.Run();
        int snapshotIndex = Array.FindIndex(args, value => value.Equals("--snapshot-dir", StringComparison.OrdinalIgnoreCase));
        if (snapshotIndex >= 0 && snapshotIndex + 1 < args.Length)
            return UiSnapshot.Render(args[snapshotIndex + 1]);

        try
        {
            AppPaths.EnsureDirectories();
            var database = new Database(AppPaths.DatabasePath);
            database.Initialize();
            Application.Run(new MainForm(new AppState(database)));
            return 0;
        }
        catch (Exception exception)
        {
            MessageBox.Show($"No se pudo iniciar Cotizador SanjCorp3D.\n\n{exception.Message}",
                "Error de inicio", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
