using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProyectoConstruccion_APAZA_CUTIPA.Models
{
    public class clsReporte
    {
        public string Nombre { get; set; }
        public DateTime FechaGeneracion { get; set; }
        public string TipoGrafico { get; set; }
        public string DatosIncluidos { get; set; }
        public int? IdEvento { get; set; }
        public DateTime? FechaEvento { get; set; }
        public TimeSpan? HoraEvento { get; set; }
        public string TipoEvento { get; set; }
        public string SubtipoEvento { get; set; }
        public string SeveridadEvento { get; set; }
        public string UbicacionEvento { get; set; }
        public decimal? ConfianzaEvento { get; set; }
        public string EstadoEvento { get; set; }
        public DateTime? FechaHoraCompletaEvento { get { if (FechaEvento.HasValue && HoraEvento.HasValue) { return FechaEvento.Value.Add(HoraEvento.Value); } return null; } }

        public clsReporte() { FechaGeneracion = DateTime.Now; }

        public string ExportarCSV()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Campo,Valor");
            Action<string, object> AddLine = (campo, valor) => { string v = valor?.ToString() ?? ""; if (v.Contains(",") || v.Contains("\"") || v.Contains("\n")) { v = "\"" + v.Replace("\"", "\"\"") + "\""; } sb.AppendLine($"{campo},{v}"); };
            AddLine("Nombre del Reporte", Nombre); AddLine("Fecha de Generación del Reporte", FechaGeneracion.ToString("yyyy-MM-dd HH:mm:ss"));
            AddLine("Tipo de Gráfico (Definición)", TipoGrafico); AddLine("Datos Incluidos (Definición)", DatosIncluidos);
            if (IdEvento.HasValue) { sb.AppendLine(); sb.AppendLine("Detalles del Evento Específico,"); AddLine("ID Evento", IdEvento); AddLine("Fecha Evento", FechaEvento?.ToString("yyyy-MM-dd")); AddLine("Hora Evento", HoraEvento?.ToString(@"hh\:mm\:ss")); AddLine("Tipo Evento", TipoEvento); AddLine("Subtipo Evento", SubtipoEvento); AddLine("Severidad Evento", SeveridadEvento); AddLine("Ubicación Evento", UbicacionEvento); AddLine("Confianza Evento", ConfianzaEvento?.ToString("F2")); AddLine("Estado Evento", EstadoEvento); }
            return sb.ToString();
        }

        public byte[] ExportarPDF()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            return QuestPDF.Fluent.Document.Create(container => {
                container.Page(page => {
                    page.Margin(30);
                    page.Header().PaddingBottom(10).AlignCenter().Text($"Reporte: {this.Nombre ?? "Sin Nombre"}").SemiBold().FontSize(20).FontColor(QuestPDF.Helpers.Colors.Blue.Medium);
                    page.Content().PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre).Column(column => {
                        column.Spacing(10);
                        Action<string, string> AddRow = (l, v) => column.Item().Text(txt => { txt.Span($"{l}: ").SemiBold(); txt.Span(v ?? "N/A"); });
                        AddRow("Fecha de Generación", this.FechaGeneracion.ToString("yyyy-MM-dd HH:mm:ss")); AddRow("Tipo de Gráfico", this.TipoGrafico); AddRow("Datos Incluidos", this.DatosIncluidos);
                        if (this.IdEvento.HasValue) { column.Item().PaddingTop(15).Text("Detalles del Evento:").Bold().FontSize(14); AddRow("ID", IdEvento.ToString()); AddRow("Fecha", FechaEvento?.ToString("yyyy-MM-dd")); AddRow("Hora", HoraEvento?.ToString(@"hh\:mm\:ss")); AddRow("Tipo", TipoEvento); AddRow("Subtipo", SubtipoEvento); AddRow("Severidad", SeveridadEvento); AddRow("Ubicación", UbicacionEvento); AddRow("Confianza", ConfianzaEvento?.ToString("F2")); AddRow("Estado", EstadoEvento); }
                    });
                    page.Footer().AlignCenter().Text(x => { x.Span("Página "); x.CurrentPageNumber(); x.Span(" de "); x.TotalPages(); });
                });
            }).GeneratePdf();
        }

        public static string ExportarListaCSV(List<clsReporte> eventos, string tituloReporte = "Listado de Eventos")
        {
            var sb = new StringBuilder();
            sb.AppendLine($"\"{tituloReporte.Replace("\"", "\"\"")}\",Generado el: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"); sb.AppendLine();
            sb.AppendLine("ID Evento,Fecha,Hora,Tipo,Subtipo,Severidad,Ubicación,Confianza,Estado");
            Action<StringBuilder, object> AppendCell = (b, v) => { string s = v?.ToString() ?? ""; if (s.Contains(",") || s.Contains("\"") || s.Contains("\n")) { s = "\"" + s.Replace("\"", "\"\"") + "\""; } b.Append(s); };
            foreach (var e in eventos) { AppendCell(sb, e.IdEvento); sb.Append(","); AppendCell(sb, e.FechaEvento?.ToString("yyyy-MM-dd")); sb.Append(","); AppendCell(sb, e.HoraEvento?.ToString(@"hh\:mm\:ss")); sb.Append(","); AppendCell(sb, e.TipoEvento); sb.Append(","); AppendCell(sb, e.SubtipoEvento); sb.Append(","); AppendCell(sb, e.SeveridadEvento); sb.Append(","); AppendCell(sb, e.UbicacionEvento); sb.Append(","); AppendCell(sb, e.ConfianzaEvento?.ToString("F2")); sb.Append(","); AppendCell(sb, e.EstadoEvento); sb.AppendLine(); }
            return sb.ToString();
        }

        public static byte[] ExportarListaPDF(List<clsReporte> eventos, string tituloReporte = "Listado de Eventos")
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            return QuestPDF.Fluent.Document.Create(container => {
                container.Page(page => {
                    page.Margin(20);
                    page.Header().PaddingBottom(10).AlignCenter().Text(tituloReporte).SemiBold().FontSize(18);
                    page.Content().Table(table => {
                        table.ColumnsDefinition(cols => { cols.ConstantColumn(40); cols.ConstantColumn(70); cols.ConstantColumn(55); cols.RelativeColumn(); cols.RelativeColumn(); cols.ConstantColumn(60); cols.RelativeColumn(); cols.ConstantColumn(50); cols.ConstantColumn(60); });
                        table.Header(header => { Action<string> AddHCell = t => header.Cell().Border(0.5f).Background(Colors.Grey.Lighten2).Padding(2).AlignCenter().Text(t).FontSize(9).SemiBold(); AddHCell("ID"); AddHCell("Fecha"); AddHCell("Hora"); AddHCell("Tipo"); AddHCell("Subtipo"); AddHCell("Severidad"); AddHCell("Ubicación"); AddHCell("Conf."); AddHCell("Estado"); });
                        foreach (var e in eventos) { Action<string> AddCell = t => table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(t ?? "").FontSize(8); AddCell(e.IdEvento?.ToString()); AddCell(e.FechaEvento?.ToString("dd/MM/yy")); AddCell(e.HoraEvento?.ToString(@"hh\:mm")); AddCell(e.TipoEvento); AddCell(e.SubtipoEvento); AddCell(e.SeveridadEvento); AddCell(e.UbicacionEvento); AddCell(e.ConfianzaEvento?.ToString("P0")); AddCell(e.EstadoEvento); }
                    });
                    page.Footer().AlignCenter().Text(x => { x.Span("Generado: ").FontSize(8); x.Span(DateTime.Now.ToString("g")).FontSize(8); x.Span(" - Pág "); x.CurrentPageNumber().FontSize(8); });
                });
            }).GeneratePdf();
        }

        public static byte[] ExportarDashboardPDF(int totalD, int alertasC, int personasD, int eventosM, List<clsReporte> rec, List<string> lblT, List<int> dataT, List<string> lblTipos, List<int> dataTipos, List<string> lblSev, List<int> dataSev, List<clsReporte> todosEv)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            return QuestPDF.Fluent.Document.Create(container => {
                container.Page(page => {
                    page.Margin(30);
                    page.Header().PaddingBottom(20).AlignCenter().Text("Dashboard General de Detecciones").SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);
                    page.Content().Column(col => {
                        col.Spacing(15);
                        col.Item().PaddingBottom(5).Text("Resumen de KPIs:").Bold().FontSize(16);
                        col.Item().Table(t => { t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); }); Action<string, int> AddKPI = (l, v) => { t.Cell().PaddingRight(10).Text(l).SemiBold(); t.Cell().Text(v.ToString("N0")); }; AddKPI("Total Detecciones", totalD); AddKPI("Alertas Críticas", alertasC); AddKPI("Personas Detectadas", personasD); AddKPI("Eventos Mascarilla", eventosM); });
                        col.Item().PaddingTop(20).Text("Detecciones Recientes:").Bold().FontSize(16);
                        if (rec.Any()) { col.Item().Table(t => { t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(3); c.RelativeColumn(2); }); t.Header(h => { h.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text("Tipo").FontSize(9).SemiBold(); h.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text("Severidad").FontSize(9).SemiBold(); h.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text("Ubicación").FontSize(9).SemiBold(); h.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text("Fecha/Hora").FontSize(9).SemiBold(); }); foreach (var e in rec.Take(5)) { t.Cell().BorderBottom(0.5f).Padding(2).Text(e.TipoEvento ?? "").FontSize(8); t.Cell().BorderBottom(0.5f).Padding(2).Text(e.SeveridadEvento ?? "").FontSize(8); t.Cell().BorderBottom(0.5f).Padding(2).Text(e.UbicacionEvento ?? "").FontSize(8); t.Cell().BorderBottom(0.5f).Padding(2).Text(e.FechaHoraCompletaEvento?.ToString("g") ?? "").FontSize(8); } }); } else { col.Item().Text("N/A"); }
                        col.Item().PaddingTop(20).Text("Datos de Gráficos:").Bold().FontSize(16);
                        col.Item().PaddingTop(5).Text("Tendencia (Datos):").SemiBold().FontSize(12); col.Item().Table(t => { t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); }); t.Header(h => { h.Cell().Background(Colors.Grey.Lighten3).Padding(1).Text("Mes/Año").FontSize(9); h.Cell().Background(Colors.Grey.Lighten3).Padding(1).Text("Cant.").FontSize(9); }); for (int i = 0; i < lblT.Count; i++) { t.Cell().Padding(1).Text(lblT[i]).FontSize(8); t.Cell().Padding(1).Text(dataT[i].ToString()).FontSize(8); } });
                        col.Item().PaddingTop(5).Text("Tipos (Datos):").SemiBold().FontSize(12); col.Item().Table(t => { t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); }); t.Header(h => { h.Cell().Background(Colors.Grey.Lighten3).Padding(1).Text("Tipo").FontSize(9); h.Cell().Background(Colors.Grey.Lighten3).Padding(1).Text("Cant.").FontSize(9); }); for (int i = 0; i < lblTipos.Count; i++) { t.Cell().Padding(1).Text(lblTipos[i]).FontSize(8); t.Cell().Padding(1).Text(dataTipos[i].ToString()).FontSize(8); } });
                        col.Item().PaddingTop(5).Text("Severidad (Datos):").SemiBold().FontSize(12); col.Item().Table(t => { t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); }); t.Header(h => { h.Cell().Background(Colors.Grey.Lighten3).Padding(1).Text("Severidad").FontSize(9); h.Cell().Background(Colors.Grey.Lighten3).Padding(1).Text("Cant.").FontSize(9); }); for (int i = 0; i < lblSev.Count; i++) { t.Cell().Padding(1).Text(lblSev[i]).FontSize(8); t.Cell().Padding(1).Text(dataSev[i].ToString()).FontSize(8); } });
                    });
                    page.Footer().AlignCenter().Text(x => { x.Span("Pág "); x.CurrentPageNumber(); x.Span("/"); x.TotalPages(); });
                });
                if (todosEv.Any()) { const int chunkSize = 50; var chunks = todosEv.Select((x, i) => new { I = i, V = x }).GroupBy(x => x.I / chunkSize).Select(x => x.Select(v => v.V).ToList()).ToList(); int part = 1; foreach (var chunk in chunks) { container.Page(p => { p.Margin(20); p.Header().PaddingBottom(10).AlignCenter().Text($"Listado Completo (Parte {part++})").SemiBold().FontSize(16); p.Content().Table(t => { t.ColumnsDefinition(c => { c.ConstantColumn(40); c.ConstantColumn(70); c.ConstantColumn(55); c.RelativeColumn(); c.RelativeColumn(); c.ConstantColumn(60); c.RelativeColumn(); c.ConstantColumn(50); c.ConstantColumn(60); }); t.Header(h => { Action<string> AddH = s => h.Cell().Border(0.5f).Background(Colors.Grey.Lighten2).Padding(2).AlignCenter().Text(s).FontSize(9).SemiBold(); AddH("ID"); AddH("Fecha"); AddH("Hora"); AddH("Tipo"); AddH("Subtipo"); AddH("Severidad"); AddH("Ubicación"); AddH("Conf."); AddH("Estado"); }); foreach (var e in chunk) { Action<string> AddC = s => t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(s ?? "").FontSize(8); AddC(e.IdEvento?.ToString()); AddC(e.FechaEvento?.ToString("dd/MM/yy")); AddC(e.HoraEvento?.ToString(@"hh\:mm")); AddC(e.TipoEvento); AddC(e.SubtipoEvento); AddC(e.SeveridadEvento); AddC(e.UbicacionEvento); AddC(e.ConfianzaEvento?.ToString("P0")); AddC(e.EstadoEvento); } }); p.Footer().AlignCenter().Text(x => { x.Span("Pág "); x.CurrentPageNumber(); x.Span("/"); x.TotalPages(); }); }); } }
            }).GeneratePdf();
        }

        public static string ExportarDashboardCSV(int totalD, int alertasC, int personasD, int eventosM, List<clsReporte> rec, List<string> lblT, List<int> dataT, List<string> lblTipos, List<int> dataTipos, List<string> lblSev, List<int> dataSev, List<clsReporte> todosEv)
        {
            var sb = new StringBuilder(); Action<string, object> AddCsvL = (c, v) => { string s = v?.ToString() ?? ""; if (s.Contains(",") || s.Contains("\"") || s.Contains("\n")) { s = "\"" + s.Replace("\"", "\"\"") + "\""; } sb.AppendLine($"\"{c.Replace("\"", "\"\"")}\",{s}"); };
            sb.AppendLine($"\"Dashboard General\",Generado: {DateTime.Now:g}"); sb.AppendLine();
            sb.AppendLine("KPIs,Valor"); AddCsvL("Total Detecciones", totalD); AddCsvL("Alertas Críticas", alertasC); AddCsvL("Personas Detectadas", personasD); AddCsvL("Eventos Mascarilla", eventosM); sb.AppendLine();
            sb.AppendLine("Recientes"); sb.AppendLine("ID,Fecha,Hora,Tipo,Subtipo,Severidad,Ubicación,Confianza,Estado"); foreach (var e in rec.Take(5)) { sb.AppendLine($"{e.IdEvento},{e.FechaEvento:yyyy-MM-dd},{e.HoraEvento:hh\\:mm\\:ss},{e.TipoEvento},{e.SubtipoEvento},{e.SeveridadEvento},{e.UbicacionEvento},{e.ConfianzaEvento:F2},{e.EstadoEvento}"); }
            sb.AppendLine();
            sb.AppendLine("Tendencia"); sb.AppendLine("Mes/Año,Cantidad"); for (int i = 0; i < lblT.Count; i++) { AddCsvL(lblT[i], dataT[i]); }
            sb.AppendLine();
            sb.AppendLine("Tipos"); sb.AppendLine("Tipo,Cantidad"); for (int i = 0; i < lblTipos.Count; i++) { AddCsvL(lblTipos[i], dataTipos[i]); }
            sb.AppendLine();
            sb.AppendLine("Severidad"); sb.AppendLine("Severidad,Cantidad"); for (int i = 0; i < lblSev.Count; i++) { AddCsvL(lblSev[i], dataSev[i]); }
            sb.AppendLine();
            sb.AppendLine("Listado Completo"); string listaCompletaCsv = ExportarListaCSV(todosEv, ""); if (listaCompletaCsv.Contains('\n')) { sb.Append(listaCompletaCsv.Substring(listaCompletaCsv.IndexOf('\n') + 1)); } else { sb.Append(listaCompletaCsv); }
            return sb.ToString();
        }

        public void GenerarVistaPrevia() { Console.WriteLine($"Rep: {Nombre}, Gen: {FechaGeneracion}, TipoG: {TipoGrafico}, Datos: {DatosIncluidos}" + (IdEvento.HasValue ? $", EvID: {IdEvento}, EvFecha: {FechaHoraCompletaEvento}, EvTipo: {TipoEvento}" : "")); }
    }
}