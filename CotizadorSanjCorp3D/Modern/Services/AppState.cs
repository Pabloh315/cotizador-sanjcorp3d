using CotizadorSanjCorp3D.Domain;
using CotizadorSanjCorp3D.Infrastructure;

namespace CotizadorSanjCorp3D.Services;

public sealed class AppState
{
    public AppState(Database database)
    {
        Database = database ?? throw new ArgumentNullException(nameof(database));
        Settings = database.GetSettings();
    }

    public Database Database { get; }
    public AppSettings Settings { get; private set; }
    public event EventHandler? DataChanged;

    public void ReloadSettings()
    {
        Settings = Database.GetSettings();
        NotifyChanged();
    }

    public void NotifyChanged() => DataChanged?.Invoke(this, EventArgs.Empty);
}
