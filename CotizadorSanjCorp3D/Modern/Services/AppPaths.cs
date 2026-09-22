namespace CotizadorSanjCorp3D.Services;

public static class AppPaths
{
    public static string DataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SanjCorp3D");
    public static string DatabasePath => Path.Combine(DataDirectory, "cotizador.db");
    public static string BackupDirectory => Path.Combine(DataDirectory, "Backups");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(BackupDirectory);
    }
}
