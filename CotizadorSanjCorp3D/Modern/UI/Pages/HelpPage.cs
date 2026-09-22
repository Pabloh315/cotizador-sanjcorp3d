using CotizadorSanjCorp3D.Services;

namespace CotizadorSanjCorp3D.UI.Pages;

internal sealed class HelpPage : PageBase
{
    public HelpPage(AppState state) : base(state)
    {
        var heading = new Panel { Dock = DockStyle.Top, Height = 86 }; var title = Theme.Label("AYUDA", 17, FontStyle.Bold, Theme.Blue); title.Location = new Point(0, 2); var sub = Theme.Label("Guía rápida del cotizador local.", 9.5f, FontStyle.Regular, Theme.Muted); sub.Location = new Point(0, 42); heading.Controls.Add(title); heading.Controls.Add(sub);
        var stack = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(0, 4, 8, 8) };
        Add(stack, "¿Cómo creo una cotización?", "Primero configura al menos una impresora y un consumible o resina. En Cotizador ingresa cliente, pieza, tiempo y cantidad. Agrega los consumos con sus gramos por pieza, pulsa Calcular precio y finalmente Guardar cotización.");
        Add(stack, "¿Cómo se calcula el precio?", "El costo suma consumibles o resina, electricidad, mantenimiento por impresión y adicionales. Después aplica el multiplicador de ganancia introducido manualmente y los impuestos configurados.");
        Add(stack, "¿Cómo cotizo varios colores?", "Agrega una fila por color. Cada fila puede usar un consumible o resina diferente con su propio tipo, precio y peso. La aplicación suma todos los consumos respetando el precio por kilogramo o litro.");
        Add(stack, "¿Dónde cambio las tarifas?", "En Configuración → Costos se definen electricidad, mantenimiento por impresión, multiplicador e impuestos. Los precios por kilogramo o litro se modifican en Consumibles → Filamentos y resinas.");
        Add(stack, "¿Cómo controlo las existencias?", "En Consumibles → Inventario registra manualmente cuántos rollos de filamento o botellas de resina tienes por tipo y color. Si la existencia es cero, el Cotizador mostrará una alerta y no agregará ese consumo.");
        Add(stack, "¿Cómo recupero una cotización?", "En el lado de Resultado pulsa Buscar cotización. Ingresa el código, nombre del cliente o proyecto, selecciona el resultado y pulsa Cargar cotización. Todos sus datos y consumos volverán al Cotizador.");
        Add(stack, "¿Cómo confirmo una venta?", "Guarda o recupera una cotización y pulsa VENTA. Después de confirmar, aparecerá como vendida en Historial y en el reporte Ventas generadas.");
        Add(stack, "¿Cómo copio el código?", "Al guardar se muestra el código en una casilla seleccionable y un botón Copiar código. Todo se genera unido: inicial del cliente, inicial de impresora, fecha y número correlativo; por ejemplo, ME202608240001.");
        Add(stack, "¿Dónde quedan mis datos?", "En una base SQLite dentro de la carpeta local de la aplicación. Configuración → Respaldo muestra la ruta y permite crear o restaurar copias.");
        Add(stack, "¿Qué información ofrecen los laminadores?", "Cura, PrusaSlicer, OrcaSlicer y otros estiman tiempo, longitud o peso de filamento y, según el equipo, consumo por extrusor/color. El proyecto 3MF y el G-code pueden conservar esos datos. Un STL solo contiene geometría: no trae tiempo ni consumo hasta que se lamina con un perfil concreto.");
        Add(stack, "¿Puedo exportar información?", "Historial y Reportes exportan cotizaciones y ventas a CSV, compatible con Excel y Google Sheets.");
        stack.SizeChanged += (_, _) => ResizeCards(stack);
        Controls.Add(stack); Controls.Add(heading);
    }
    private static void Add(FlowLayoutPanel stack, string question, string answer)
    {
        var card = new CardPanel { MinimumSize = new Size(780, 0), Width = 880, Padding = new Padding(18), Margin = new Padding(0, 5, 0, 5) };
        var q = Theme.Label(question, 10.5f, FontStyle.Bold, Theme.Text); q.Location = new Point(18, 18);
        var a = Theme.Label(answer, 9.5f, FontStyle.Regular, Theme.Muted); a.Location = new Point(18, 50); a.MaximumSize = new Size(830, 0); a.Tag = "answer";
        card.Controls.Add(q); card.Controls.Add(a); card.Size = new Size(920, a.Bottom + 18); stack.Controls.Add(card);
    }
    private static void ResizeCards(FlowLayoutPanel stack)
    {
        int width = Math.Max(780, stack.ClientSize.Width - 38);
        foreach (CardPanel card in stack.Controls.OfType<CardPanel>())
        {
            Label? answer = card.Controls.OfType<Label>().FirstOrDefault(label => Equals(label.Tag, "answer"));
            if (answer is null) continue;
            card.Width = width; answer.MaximumSize = new Size(Math.Max(730, width - 50), 0); card.Height = answer.Bottom + 18;
        }
    }
}
