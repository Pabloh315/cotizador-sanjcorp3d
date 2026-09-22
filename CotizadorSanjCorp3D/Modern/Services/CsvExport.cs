using System.Globalization;
using System.Text;
using CotizadorSanjCorp3D.Domain;

namespace CotizadorSanjCorp3D.Services;

public static class CsvExport
{
    public static void Quotes(string path, IEnumerable<QuoteSummary> quotes, string currency)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
        writer.WriteLine("Código;Estado;Fecha;Cliente;Pieza o proyecto;Impresora;Peso total (g);Costo total;Precio recomendado;Moneda");
        foreach (var quote in quotes)
        {
            writer.WriteLine(string.Join(';', Escape(quote.OrderCode), Escape(quote.IsSold ? "Vendida" : "Cotización"), Escape(quote.CreatedAt.ToString("yyyy-MM-dd HH:mm")),
                Escape(quote.Customer), Escape(quote.ProjectName), Escape(quote.PrinterName), Number(quote.TotalWeight),
                Number(quote.CostTotal), Number(quote.RecommendedPrice), Escape(currency)));
        }
    }

    public static void Sales(string path, IEnumerable<SaleSummary> sales, string currency)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
        writer.WriteLine("Código;Fecha de venta;Cliente;Pieza o proyecto;Costo total;Venta;Ganancia;Moneda");
        foreach (var sale in sales)
        {
            writer.WriteLine(string.Join(';', Escape(sale.OrderCode), Escape(sale.SoldAt.ToString("yyyy-MM-dd HH:mm")),
                Escape(sale.Customer), Escape(sale.ProjectName), Number(sale.CostTotal), Number(sale.SaleAmount),
                Number(sale.Profit), Escape(currency)));
        }
    }

    private static string Number(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture);
    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
