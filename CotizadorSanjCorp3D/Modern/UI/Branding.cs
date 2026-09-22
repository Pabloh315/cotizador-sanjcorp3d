using System.Reflection;
using System.Runtime.InteropServices;

namespace CotizadorSanjCorp3D.UI;

internal static class Branding
{
    private const string LogoResource = "CotizadorSanjCorp3D.Assets.logo-oficial.jpeg";

    public static Image Logo { get; } = LoadLogo();
    public static Icon AppIcon { get; } = CreateIcon();

    private static Image LoadLogo()
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LogoResource)
            ?? throw new InvalidOperationException("No se encontró el logo oficial integrado.");
        using var source = Image.FromStream(stream);
        return new Bitmap(source);
    }

    private static Icon CreateIcon()
    {
        using var bitmap = new Bitmap(64, 64);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.FromArgb(7, 19, 37));
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(Logo, new Rectangle(0, 0, 64, 64));
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using Icon temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);
}
