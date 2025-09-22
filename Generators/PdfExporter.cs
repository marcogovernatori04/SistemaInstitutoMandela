using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using SistemaInstituto.Models;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace SistemaInstituto.Generators
{
    public static class PdfExporter
    {
        public static void Exportar(string ruta, IEnumerable datos)
        {
            using var writer = new PdfWriter(ruta);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf);

            var lista = datos.Cast<object>().ToList();
            if (!lista.Any())
            {
                doc.Add(new Paragraph("No hay datos para exportar"));
                return;
            }

            // Fuente para negrita
            PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            // Título
            var titulo = new Paragraph("Listado de pagos")
                .SetFont(boldFont)
                .SetFontSize(18)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10);
            doc.Add(titulo);

            var tipo = lista.First().GetType();

            // Solo propiedades marcadas con [Exportable]
            var propiedades = tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                  .Where(p => p.GetCustomAttribute<ExportableAttribute>() != null)
                                  .ToArray();

            var tabla = new Table(propiedades.Length).UseAllAvailableWidth();

            // Headers
            foreach (var prop in propiedades)
                tabla.AddHeaderCell(new Cell().Add(new Paragraph(prop.Name).SetFont(boldFont)));

            decimal totalMonto = 0;

            // Filas
            foreach (var item in lista)
            {
                foreach (var prop in propiedades)
                {
                    var valor = prop.GetValue(item);
                    string texto;

                    if (valor is DateTime dt)
                        texto = dt.ToString("dd/MM/yyyy");
                    else if (valor is decimal dec)
                    {
                        texto = dec.ToString("C", CultureInfo.CurrentCulture);
                        if (prop.Name.Equals("Monto", StringComparison.OrdinalIgnoreCase))
                            totalMonto += dec;
                    }
                    else
                        texto = valor?.ToString() ?? string.Empty;

                    tabla.AddCell(new Cell().Add(new Paragraph(texto)));
                }
            }

            // Fila final de total
            tabla.AddCell(new Cell(1, propiedades.Length - 1)
                .Add(new Paragraph("Total montos:").SetFont(boldFont)));

            tabla.AddCell(new Cell()
                .Add(new Paragraph(totalMonto.ToString("C", CultureInfo.CurrentCulture)).SetFont(boldFont)));

            doc.Add(tabla);
        }
    }
}
