using iText.IO.Font;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using SistemaInstituto.Models;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using TextAlignment = iText.Layout.Properties.TextAlignment;
using VerticalAlignment = iText.Layout.Properties.VerticalAlignment;

namespace SistemaInstituto.Generators
{
    public class FacturaGenerator
    {

        public static void GenerarFactura(Pago pago)
        {
            if (pago == null)
                throw new ArgumentNullException(nameof(pago), "El objeto pago no puede ser null.");

            if (pago.IdAlumnoNavigation == null)
                throw new InvalidOperationException("El pago no tiene asociado un Alumno válido.");

            string nombreAlumno = pago.IdAlumnoNavigation.Nombre.Replace(' ', '_');

            var carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SistemaInstituto", "Recibos");
            Directory.CreateDirectory(carpeta);

            var ruta = Path.Combine(carpeta, $"Recibo_{pago.Id}_{nombreAlumno}.pdf");

            using (var writer = new PdfWriter(ruta))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf))
            {
                document.SetMargins(20, 20, 20, 20);

                string pathBahnschrift = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "bahnschrift.ttf");

                FontProgram fontProgram = FontProgramFactory.CreateFont(pathBahnschrift);

                PdfFont regular = PdfFontFactory.CreateFont(fontProgram, PdfEncodings.IDENTITY_H);
                PdfFont bold = PdfFontFactory.CreateFont(fontProgram, PdfEncodings.IDENTITY_H);
                Color colorTitulo = new DeviceRgb(39, 165, 153);
                Color colorFila = new DeviceRgb(245, 245, 245);

                Table table = new Table(new float[] { 3, 1 });
                table.SetWidth(UnitValue.CreatePercentValue(100));

                Cell cellText = new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph("INSTITUTO MANDELA")
                        .SetFont(bold)
                        .SetFontSize(24)
                        .SetFontColor(colorTitulo)
                        .SetTextAlignment(TextAlignment.LEFT))
                    .Add(new Paragraph("RECIBO DE PAGO")
                        .SetFont(bold)
                        .SetFontSize(20)
                        .SetTextAlignment(TextAlignment.LEFT))
                    .Add(new Paragraph($"Recibo N°: {pago.Id} • Fecha de pago: {pago.FechaPago:dd/MM/yyyy}")
                        .SetFont(bold)
                        .SetFontSize(12)
                        .SetTextAlignment(TextAlignment.LEFT));

                table.AddCell(cellText);

                var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo mandela.png");

                if (File.Exists(logoPath))
                {
                    ImageData logoData = ImageDataFactory.Create(logoPath);
                    Image logo = new Image(logoData)
                        .ScaleToFit(100, 100);

                    Cell cellLogo = new Cell()
                        .SetBorder(Border.NO_BORDER)
                        .Add(logo)
                        .SetTextAlignment(TextAlignment.RIGHT)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE);

                    table.AddCell(cellLogo);
                }
                else
                {
                    table.AddCell(new Cell().SetBorder(Border.NO_BORDER));
                }

                document.Add(table);

                document.Add(new LineSeparator(new SolidLine(1f)).SetMarginBottom(15));

                Paragraph datosAlumno = new Paragraph("Datos del Alumno")
                    .SetFont(bold)
                    .SetFontSize(14)
                    .SetFontColor(colorTitulo);
                document.Add(datosAlumno);

                Table tablaAlumno = new Table(UnitValue.CreatePercentArray(new float[] { 30, 70 })).UseAllAvailableWidth();
                tablaAlumno.SetMarginBottom(15);

                void AddCell(string texto, bool isHeader = false)
                {
                    Cell cell = new Cell().Add(new Paragraph(texto).SetFont(isHeader ? bold : regular));
                    cell.SetPadding(2);
                    cell.SetBorder(Border.NO_BORDER);
                    tablaAlumno.AddCell(cell);
                }

                AddCell("Alumno:", true);
                AddCell(pago.IdAlumnoNavigation.Nombre);
                AddCell("Curso:", true);
                AddCell(pago.IdAlumnoNavigation.IdCursoNavigation.Nombre);
                AddCell("Fecha de pago:", true);
                AddCell(pago.FechaPago.ToString("dd/MM/yyyy"));
                AddCell("Método de pago:", true);
                AddCell(pago.IdMetodoPagoNavigation.Nombre);

                document.Add(tablaAlumno);
                document.Add(new LineSeparator(new SolidLine(1f)).SetMarginBottom(15));

                Paragraph detallePago = new Paragraph("Detalle de Pago")
                    .SetFont(bold)
                    .SetFontSize(14)
                    .SetFontColor(colorTitulo);
                document.Add(detallePago);

                Table tablaPago = new Table(UnitValue.CreatePercentArray(new float[] { 70, 30 })).UseAllAvailableWidth();
                tablaPago.SetMarginBottom(15);

                void AddRow(string concepto, string valor)
                {
                    Color bgColor = ColorConstants.WHITE;
                    Cell c1 = new Cell().Add(new Paragraph(concepto).SetFont(regular)).SetBackgroundColor(bgColor).SetPadding(5);
                    Cell c2 = new Cell().Add(new Paragraph(valor).SetFont(regular)).SetBackgroundColor(bgColor).SetPadding(5);
                    tablaPago.AddCell(c1);
                    tablaPago.AddCell(c2);
                }

                AddRow("Concepto", pago.Concepto);
                AddRow("Monto", pago.MontoPagado.ToString("C"));
                AddRow("Arancel", pago.IdArancelNavigation.Concepto);

                document.Add(tablaPago);

                if (!string.IsNullOrEmpty(pago.Notas))
                {
                    Paragraph notasTitulo = new Paragraph("Notas")
                        .SetFont(bold)
                        .SetFontSize(14)
                        .SetFontColor(colorTitulo);
                    document.Add(notasTitulo);

                    Paragraph notas = new Paragraph(pago.Notas).SetFont(regular).SetFontSize(12).SetMarginBottom(15);
                    document.Add(notas);
                }

                Paragraph footer = new Paragraph($"Generado por Sistema Instituto Mandela • {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .SetFont(regular)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontColor(ColorConstants.GRAY);
                document.Add(footer);

                document.Close();
            }

            MessageBox.Show($"Recibo generado: {ruta}");
        }


        public static void GenerarFacturaGrupal(List<Pago> pagos)
        {
            if (pagos == null || pagos.Count == 0)
                throw new ArgumentNullException(nameof(pagos), "La lista de pagos no puede ser null ni vacía.");

            var carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SistemaInstituto", "Recibos");
            Directory.CreateDirectory(carpeta);

            string nombreBeneficio = pagos
                .Select(p => p.IdAlumnoNavigation.AlumnosBeneficio.IdBeneficioNavigation.Nombre)
                .FirstOrDefault(nb => !string.IsNullOrEmpty(nb)) ?? "SinBeneficio";

            nombreBeneficio = Regex.Replace(nombreBeneficio.Trim(), @"\s+", "_");
            var idsPagos = pagos.Select(p => p.Id);

            var ruta = Path.Combine(carpeta, $"Recibos_{string.Join("_", idsPagos)}_{nombreBeneficio}.pdf");

            using (var writer = new PdfWriter(ruta))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf))
            {
                document.SetMargins(20, 20, 20, 20);

                // Fuente Bahnschrift
                string pathBahnschrift = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "bahnschrift.ttf");
                FontProgram fontProgram = FontProgramFactory.CreateFont(pathBahnschrift);
                PdfFont regular = PdfFontFactory.CreateFont(fontProgram, PdfEncodings.IDENTITY_H);
                PdfFont bold = PdfFontFactory.CreateFont(fontProgram, PdfEncodings.IDENTITY_H);

                Color colorTitulo = new DeviceRgb(39, 165, 153);

                Table table = new Table(new float[] { 3, 1 });
                table.SetWidth(UnitValue.CreatePercentValue(100));

                Cell cellText = new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph("INSTITUTO MANDELA")
                        .SetFont(bold)
                        .SetFontSize(24)
                        .SetFontColor(colorTitulo)
                        .SetTextAlignment(TextAlignment.LEFT))
                    .Add(new Paragraph("RECIBO DE PAGO GRUPAL")
                        .SetFont(bold)
                        .SetFontSize(20)
                        .SetTextAlignment(TextAlignment.LEFT))
                    .Add(new Paragraph($"Recibos Nº: {string.Join(", ", pagos.Select(p => p.Id))} • Fecha de pago: {pagos.First().FechaPago:dd/MM/yyyy}")
                        .SetFont(bold)
                        .SetFontSize(12)
                        .SetTextAlignment(TextAlignment.LEFT));

                table.AddCell(cellText);

                var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo mandela.png");

                if (File.Exists(logoPath))
                {
                    ImageData logoData = ImageDataFactory.Create(logoPath);
                    Image logo = new Image(logoData)
                        .ScaleToFit(100, 100);

                    Cell cellLogo = new Cell()
                        .SetBorder(Border.NO_BORDER)
                        .Add(logo)
                        .SetTextAlignment(TextAlignment.RIGHT)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE);

                    table.AddCell(cellLogo);
                }
                else
                {
                    table.AddCell(new Cell().SetBorder(Border.NO_BORDER));
                }

                document.Add(table);

                document.Add(new LineSeparator(new SolidLine(1f)).SetMarginBottom(15));

                // Concepto y método de pago (una sola vez)
                var concepto = pagos.First().IdArancelNavigation?.Concepto ?? pagos.First().Concepto ?? "";
                var metodoPago = pagos.First().IdMetodoPagoNavigation?.Nombre ?? "";

                document.Add(new Paragraph($"Concepto: {concepto}")
                    .SetFont(bold)
                    .SetFontSize(12));

                document.Add(new Paragraph($"Método de pago: {metodoPago}")
                    .SetFont(bold)
                    .SetFontSize(12)
                    .SetMarginBottom(10));

                // Tabla de alumnos
                Table tablaAlumnos = new Table(UnitValue.CreatePercentArray(new float[] { 50, 30, 20 }))
                    .UseAllAvailableWidth()
                    .SetMarginBottom(15);

                string[] headers = { "Alumno", "Curso", "Monto" };
                foreach (var h in headers)
                {
                    tablaAlumnos.AddCell(new Cell()
                        .Add(new Paragraph(h).SetFont(bold))
                        .SetPadding(5)
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                }

                decimal total = 0;
                foreach (var pago in pagos)
                {
                    string nombre = pago.IdAlumnoNavigation?.Nombre ?? "SinNombre";
                    string curso = pago.IdAlumnoNavigation?.IdCursoNavigation?.Nombre ?? "SinCurso";
                    string montoStr = pago.MontoPagado.ToString("C");

                    tablaAlumnos.AddCell(new Cell().Add(new Paragraph(nombre).SetFont(regular)).SetPadding(5));
                    tablaAlumnos.AddCell(new Cell().Add(new Paragraph(curso).SetFont(regular)).SetPadding(5));
                    tablaAlumnos.AddCell(new Cell().Add(new Paragraph(montoStr).SetFont(regular)).SetPadding(5));

                    total += pago.MontoPagado;
                }

                // Total
                tablaAlumnos.AddCell(new Cell(1, 2)
                    .Add(new Paragraph("TOTAL").SetFont(bold))
                    .SetPadding(5)
                    .SetTextAlignment(TextAlignment.RIGHT));

                tablaAlumnos.AddCell(new Cell()
                    .Add(new Paragraph(total.ToString("C")).SetFont(bold))
                    .SetPadding(5));

                document.Add(tablaAlumnos);

                // Notas (solo si hay)
                var notas = string.Join(Environment.NewLine, pagos
                    .Where(p => !string.IsNullOrEmpty(p.Notas))
                    .Select(p => p.Notas)
                    .Distinct());

                if (!string.IsNullOrEmpty(notas))
                {
                    document.Add(new Paragraph("Notas")
                        .SetFont(bold)
                        .SetFontSize(14)
                        .SetFontColor(colorTitulo));

                    document.Add(new Paragraph(notas)
                        .SetFont(regular)
                        .SetFontSize(12)
                        .SetMarginBottom(15));
                }

                // Footer
                document.Add(new Paragraph($"Generado por Sistema Instituto Mandela • {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .SetFont(regular)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontColor(ColorConstants.GRAY));

                document.Close();
            }

            MessageBox.Show($"Recibo grupal generado: {ruta}");
        }
    }
}
