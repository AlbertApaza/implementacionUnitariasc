using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

using ProyectoConstruccion_APAZA_CUTIPA.Models;

namespace TEST_APAZA_CUTIPA
{
    public class ReporteTests
    {
        private clsReporte CreateSampleEvent(int id, DateTime date, TimeSpan time, string type, string subtype, string severity, string location, decimal confidence, string estado)
        {
            return new clsReporte
            {
                IdEvento = id,
                FechaEvento = date,
                HoraEvento = time,
                TipoEvento = type,
                SubtipoEvento = subtype,
                SeveridadEvento = severity,
                UbicacionEvento = location,
                ConfianzaEvento = confidence,
                EstadoEvento = estado
            };
        }

        private List<clsReporte> CreateSampleEventList()
        {
            return new List<clsReporte>
            {
                CreateSampleEvent(1, new DateTime(2023, 10, 26), new TimeSpan(10, 30, 0), "person", "detection", "baja", "Area A", 0.85m, "Activo"),
                CreateSampleEvent(2, new DateTime(2023, 10, 25), new TimeSpan(15, 0, 0), "mask", null, "alta", "Entrada", 0.99m, "Cerrado"),
                CreateSampleEvent(3, new DateTime(2023, 10, 25), new TimeSpan(11, 45, 0), "vehicle", "car", "media", "Parqueo", 0.70m, "Activo"),
                CreateSampleEvent(4, new DateTime(2023, 10, 24), new TimeSpan(08, 0, 0), "person", "entry", "baja", "Area B", 0.90m, "Activo"),
                CreateSampleEvent(5, new DateTime(2023, 10, 24), new TimeSpan(22, 10, 0), "mask", null, "crítico", "Salida", 1.00m, "Cerrado"),
            };
        }

        private List<clsReporte> CreateEventListWithSpecialChars()
        {
            return new List<clsReporte>
            {
                CreateSampleEvent(10, new DateTime(2023, 11, 01), new TimeSpan(9, 0, 0), "person", "detection", "baja", "Area \"Especial\", Norte", 0.75m, "Activo, Nuevo"),
                CreateSampleEvent(11, new DateTime(2023, 11, 01), new TimeSpan(9, 5, 0), "mask", null, "alta", "Zona C\nFrente", 0.95m, "Abierto\nValidando")
            };
        }

        [Fact]
        public void ExportarCSV_SingleEvent_GeneratesCorrectCsvString()
        {
            var evento = CreateSampleEvent(1, new DateTime(2023, 10, 26), new TimeSpan(10, 30, 0), "person", "detection", "baja", "Area A", 0.85m, "Activo");
            evento.Nombre = "Test Event Report";
            evento.TipoGrafico = "Detail";
            evento.DatosIncluidos = "Full Details";
            evento.FechaGeneracion = new DateTime(2023, 10, 27, 14, 0, 0);


            var csv = evento.ExportarCSV();

            Assert.NotNull(csv);
            Assert.Contains("Campo,Valor", csv);
            Assert.Contains("Nombre del Reporte,Test Event Report", csv);
            Assert.Contains("Fecha de Generación del Reporte,2023-10-27 14:00:00", csv);
            Assert.Contains("Tipo de Gráfico (Definición),Detail", csv);
            Assert.Contains("Datos Incluidos (Definición),Full Details", csv);
            Assert.Contains("Detalles del Evento Específico,", csv);
            Assert.Contains("ID Evento,1", csv);
            Assert.Contains("Fecha Evento,2023-10-26", csv);
            Assert.Contains("Hora Evento,10:30:00", csv);
            Assert.Contains("Tipo Evento,person", csv);
            Assert.Contains("Subtipo Evento,detection", csv);
            Assert.Contains("Severidad Evento,baja", csv);
            Assert.Contains("Ubicación Evento,Area A", csv);
            Assert.Contains("Confianza Evento,0.85", csv);
            Assert.Contains("Estado Evento,Activo", csv);
        }

        [Fact]
        public void ExportarCSV_SingleEventWithNulls_HandlesNullsGracefully()
        {
            var evento = new clsReporte
            {
                IdEvento = 99,
                FechaEvento = new DateTime(2024, 1, 1),
                HoraEvento = new TimeSpan(1, 1, 1),
                TipoEvento = "generic",
                SubtipoEvento = null,
                SeveridadEvento = null,
                UbicacionEvento = null,
                ConfianzaEvento = null,
                EstadoEvento = null
            };
            evento.Nombre = "Null Test Report";
            evento.FechaGeneracion = new DateTime(2024, 1, 2, 5, 5, 5);

            var csv = evento.ExportarCSV();

            Assert.NotNull(csv);
            Assert.Contains("Subtipo Evento,", csv);
            Assert.Contains("Severidad Evento,", csv);
            Assert.Contains("Ubicación Evento,", csv);
            Assert.Contains("Confianza Evento,", csv);
            Assert.Contains("Estado Evento,", csv);
        }

        [Fact]
        public void ExportarCSV_SingleEventWithSpecialChars_QuotesFields()
        {
            var evento = CreateSampleEvent(10, new DateTime(2023, 11, 01), new TimeSpan(9, 0, 0), "person", "detection", "baja", "Area \"Especial\", Norte", 0.75m, "Activo, Nuevo\nDetail");
            evento.Nombre = "Special, Char Report";
            evento.FechaGeneracion = new DateTime(2023, 11, 02, 10, 0, 0);

            var csv = evento.ExportarCSV();

            Assert.NotNull(csv);
            Assert.Contains("Nombre del Reporte,\"Special, Char Report\"", csv);
            Assert.Contains("Ubicación Evento,\"Area \"\"Especial\"\", Norte\"", csv);
            Assert.Contains("Estado Evento,\"Activo, Nuevo\nDetail\"", csv);
        }

        [Fact]
        public void ExportarPDF_SingleEvent_GeneratesByteArray()
        {
            var evento = CreateSampleEvent(1, new DateTime(2023, 10, 26), new TimeSpan(10, 30, 0), "person", "detection", "baja", "Area A", 0.85m, "Activo");
            evento.Nombre = "Test Event Report";

            var pdfBytes = evento.ExportarPDF();

            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 0);
        }

        [Fact]
        public void ExportarListaCSV_ListOfEvents_GeneratesCorrectCsvString()
        {
            var eventos = CreateSampleEventList();
            string title = "Sample Events List";

            var csv = clsReporte.ExportarListaCSV(eventos, title);

            Assert.NotNull(csv);
            Assert.Contains("\"Sample Events List\"", csv);
            Assert.Contains("ID Evento,Fecha,Hora,Tipo,Subtipo,Severidad,Ubicación,Confianza,Estado", csv);

            Assert.Contains("1,2023-10-26,10:30:00,person,detection,baja,Area A,0.85,Activo", csv);
            Assert.Contains("2,2023-10-25,15:00:00,mask,,alta,Entrada,0.99,Cerrado", csv);
            Assert.Contains("5,2023-10-24,22:10:00,mask,,crítico,Salida,1.00,Cerrado", csv);

            var lines = csv.Trim().Split('\n');
            Assert.True(lines.Length >= 8);
        }

        [Fact]
        public void ExportarListaCSV_EmptyList_GeneratesCsvWithHeaderOnly()
        {
            var eventos = new List<clsReporte>();
            string title = "Empty List Report";

            var csv = clsReporte.ExportarListaCSV(eventos, title);

            Assert.NotNull(csv);
            Assert.Contains("\"Empty List Report\"", csv);
            Assert.Contains("ID Evento,Fecha,Hora,Tipo,Subtipo,Severidad,Ubicación,Confianza,Estado", csv);
            var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.True(lines.Length <= 4);
            Assert.DoesNotContain("1,", csv);
        }

        [Fact]
        public void ExportarListaPDF_ListOfEvents_GeneratesByteArray()
        {
            var eventos = CreateSampleEventList();
            string title = "Sample Events List";

            var pdfBytes = clsReporte.ExportarListaPDF(eventos, title);

            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 0);
        }

        [Fact]
        public void ExportarListaPDF_EmptyList_GeneratesByteArray()
        {
            var eventos = new List<clsReporte>();
            string title = "Empty List Report";

            var pdfBytes = clsReporte.ExportarListaPDF(eventos, title);

            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 0);
        }

        [Fact]
        public void ExportarDashboardCSV_WithData_GeneratesCorrectCsvString()
        {
            var rec = CreateSampleEventList().Take(3).ToList();
            var todosEv = CreateSampleEventList();
            var tendLabels = new List<string> { "Oct 23", "Nov 23" };
            var tendData = new List<int> { 10, 15 };
            var tiposLabels = new List<string> { "person", "mask" };
            var tiposData = new List<int> { 20, 5 };
            var sevLabels = new List<string> { "alta", "baja" };
            var sevData = new List<int> { 7, 18 };

            var csv = clsReporte.ExportarDashboardCSV(
                totalD: 50,
                alertasC: 10,
                personasD: 30,
                eventosM: 15,
                rec: rec,
                lblT: tendLabels,
                dataT: tendData,
                lblTipos: tiposLabels,
                dataTipos: tiposData,
                lblSev: sevLabels,
                dataSev: sevData,
                todosEv: todosEv
            );

            Assert.NotNull(csv);
            Assert.Contains("KPIs,Valor", csv);
            Assert.Contains("\"Total Detecciones\",50", csv);
            Assert.Contains("\"Alertas Críticas\",10", csv);
            Assert.Contains("\"Personas Detectadas\",30", csv);
            Assert.Contains("\"Eventos Mascarilla\",15", csv);

            Assert.Contains("Recientes", csv);
            Assert.Contains("ID,Fecha,Hora,Tipo,Subtipo,Severidad,Ubicación,Confianza,Estado", csv);
            Assert.Contains("1,2023-10-26,10:30:00,person,detection,baja,Area A,0.85,Activo", csv);

            Assert.Contains("Tendencia", csv);
            Assert.Contains("Mes/Año,Cantidad", csv);
            Assert.Contains("\"Oct 23\",10", csv);

            Assert.Contains("Tipos", csv);
            Assert.Contains("Tipo,Cantidad", csv);
            Assert.Contains("\"person\",20", csv);

            Assert.Contains("Severidad", csv);
            Assert.Contains("Severidad,Cantidad", csv);
            Assert.Contains("\"alta\",7", csv);

            Assert.Contains("Listado Completo", csv);
            Assert.Contains("ID Evento,Fecha,Hora,Tipo,Subtipo,Severidad,Ubicación,Confianza,Estado", csv);
            Assert.Contains("5,2023-10-24,22:10:00,mask,,crítico,Salida,1.00,Cerrado", csv);
        }

        [Fact]
        public void ExportarDashboardPDF_WithData_GeneratesByteArray()
        {
            var rec = CreateSampleEventList().Take(3).ToList();
            var todosEv = CreateSampleEventList();
            var tendLabels = new List<string> { "Oct 23", "Nov 23" };
            var tendData = new List<int> { 10, 15 };
            var tiposLabels = new List<string> { "person", "mask" };
            var tiposData = new List<int> { 20, 5 };
            var sevLabels = new List<string> { "alta", "baja" };
            var sevData = new List<int> { 7, 18 };


            var pdfBytes = clsReporte.ExportarDashboardPDF(
                totalD: 50,
                alertasC: 10,
                personasD: 30,
                eventosM: 15,
                rec: rec,
                lblT: tendLabels,
                dataT: tendData,
                lblTipos: tiposLabels,
                dataTipos: tiposData,
                lblSev: sevLabels,
                dataSev: sevData,
                todosEv: todosEv
            );

            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 0);
        }

        [Fact]
        public void ExportarDashboardPDF_WithEmptyLists_GeneratesByteArray()
        {
            var rec = new List<clsReporte>();
            var todosEv = new List<clsReporte>();
            var tendLabels = new List<string>();
            var tendData = new List<int>();
            var tiposLabels = new List<string>();
            var tiposData = new List<int>();
            var sevLabels = new List<string>();
            var sevData = new List<int>();

            var pdfBytes = clsReporte.ExportarDashboardPDF(
                totalD: 0,
                alertasC: 0,
                personasD: 0,
                eventosM: 0,
                rec: rec,
                lblT: tendLabels,
                dataT: tendData,
                lblTipos: tiposLabels,
                dataTipos: tiposData,
                lblSev: sevLabels,
                dataSev: sevData,
                todosEv: todosEv
            );

            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 0);
        }
    }
}