using CotizadorSanjCorp3D.Services;

namespace CotizadorSanjCorp3D.UI;

internal abstract class PageBase : UserControl
{
    protected PageBase(AppState state)
    {
        State = state;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Dock = DockStyle.Fill;
        Padding = new Padding(20);
        AutoScroll = true;
        AutoScaleMode = AutoScaleMode.Dpi;
    }

    protected AppState State { get; }
    public virtual void RefreshData() { }

    protected static void ShowError(Exception exception) => MessageBox.Show(exception.Message, "No se pudo completar",
        MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
