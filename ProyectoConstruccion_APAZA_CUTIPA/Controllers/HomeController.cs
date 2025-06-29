using ProyectoConstruccion_APAZA_CUTIPA.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using MySql.Data.MySqlClient;

namespace ProyectoConstruccion_APAZA_CUTIPA.Controllers
{
    public class HomeController : Controller
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["MySqlConexion"].ConnectionString;

        private List<clsReporte> ObtenerEventosDesdeDB()
        {
            List<clsReporte> eventos = new List<clsReporte>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT idEvento, fecha, hora, tipo, subtipo, severidad, ubicacion, confianza, estado FROM tbDeteccionEventos ORDER BY fecha DESC, hora DESC;";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                eventos.Add(new clsReporte
                                {
                                    Nombre = "Datos de Evento",
                                    FechaGeneracion = DateTime.Now,
                                    IdEvento = reader.GetInt32("idEvento"),
                                    FechaEvento = reader.GetDateTime("fecha"),
                                    HoraEvento = reader.GetTimeSpan("hora"),
                                    TipoEvento = reader.GetString("tipo"),
                                    SubtipoEvento = reader.IsDBNull(reader.GetOrdinal("subtipo")) ? null : reader.GetString("subtipo"),
                                    SeveridadEvento = reader.IsDBNull(reader.GetOrdinal("severidad")) ? null : reader.GetString("severidad"),
                                    UbicacionEvento = reader.IsDBNull(reader.GetOrdinal("ubicacion")) ? null : reader.GetString("ubicacion"),
                                    ConfianzaEvento = reader.IsDBNull(reader.GetOrdinal("confianza")) ? (decimal?)null : reader.GetDecimal("confianza"),
                                    EstadoEvento = reader.IsDBNull(reader.GetOrdinal("estado")) ? null : reader.GetString("estado")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { ViewBag.ErrorMessage = "Error DB: " + ex.Message; System.Diagnostics.Debug.WriteLine("Err DB: " + ex.ToString()); return new List<clsReporte>(); }
            return eventos;
        }

        private clsReporte ObtenerUnEventoEspecifico(int idEvento)
        {
            clsReporte evento = null;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT idEvento, fecha, hora, tipo, subtipo, severidad, ubicacion, confianza, estado FROM tbDeteccionEventos WHERE idEvento = @IdEvento;";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdEvento", idEvento);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                evento = new clsReporte
                                {
                                    Nombre = $"Detalle Ev.{reader.GetInt32("idEvento")}",
                                    FechaGeneracion = DateTime.Now,
                                    TipoGrafico = "N/A",
                                    DatosIncluidos = "Detalle evento.",
                                    IdEvento = reader.GetInt32("idEvento"),
                                    FechaEvento = reader.GetDateTime("fecha"),
                                    HoraEvento = reader.GetTimeSpan("hora"),
                                    TipoEvento = reader.GetString("tipo"),
                                    SubtipoEvento = reader.IsDBNull(reader.GetOrdinal("subtipo")) ? null : reader.GetString("subtipo"),
                                    SeveridadEvento = reader.IsDBNull(reader.GetOrdinal("severidad")) ? null : reader.GetString("severidad"),
                                    UbicacionEvento = reader.IsDBNull(reader.GetOrdinal("ubicacion")) ? null : reader.GetString("ubicacion"),
                                    ConfianzaEvento = reader.IsDBNull(reader.GetOrdinal("confianza")) ? (decimal?)null : reader.GetDecimal("confianza"),
                                    EstadoEvento = reader.IsDBNull(reader.GetOrdinal("estado")) ? null : reader.GetString("estado")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Err Ev.{idEvento}: {ex.Message}"); return null; }
            return evento;
        }

        public ActionResult Index(int pagina = 1)
        {
            List<clsReporte> todosEv = ObtenerEventosDesdeDB();
            if (ViewBag.ErrorMessage != null) { return View(new List<clsReporte>()); }
            ViewBag.TotalDetecciones = todosEv.Count;
            ViewBag.AlertasCriticas = todosEv.Count(e => e.SeveridadEvento?.ToLower() == "alta" || e.SeveridadEvento?.ToLower() == "crítico");
            ViewBag.PersonasDetectadas = todosEv.Count(e => e.TipoEvento?.ToLower() == "person");
            ViewBag.EventosTipoMask = todosEv.Count(e => e.TipoEvento?.ToLower() == "mask");
            ViewBag.DeteccionesRecientes = todosEv.OrderByDescending(e => e.FechaHoraCompletaEvento).Take(5).ToList();
            var tendData = todosEv.Where(e => e.FechaEvento.HasValue && e.FechaEvento.Value >= DateTime.Now.AddYears(-1)).GroupBy(e => new { e.FechaEvento.Value.Year, e.FechaEvento.Value.Month }).Select(g => new { MA = new DateTime(g.Key.Year, g.Key.Month, 1), C = g.Count() }).OrderBy(x => x.MA).ToList();
            ViewBag.LabelsTendencia = tendData.Select(td => td.MA.ToString("MMM yy")).ToList(); ViewBag.DataTendencia = tendData.Select(td => td.C).ToList();
            var tiposData = todosEv.GroupBy(e => e.TipoEvento).Select(g => new { T = g.Key, C = g.Count() }).OrderByDescending(x => x.C).ToList();
            ViewBag.LabelsTiposDeteccion = tiposData.Select(td => string.IsNullOrEmpty(td.T) ? "N/A" : td.T).ToList(); ViewBag.DataTiposDeteccion = tiposData.Select(td => td.C).ToList();
            var sevData = todosEv.GroupBy(e => e.SeveridadEvento).Select(g => new { S = g.Key, C = g.Count() }).OrderByDescending(x => x.C).ToList();
            ViewBag.LabelsSeveridad = sevData.Select(sd => string.IsNullOrEmpty(sd.S) ? "N/A" : sd.S).ToList(); ViewBag.DataSeveridad = sevData.Select(sd => sd.C).ToList();
            int regPorPag = 10; ViewBag.TotalRegistrosDatosCompletos = todosEv.Count;
            var evPaginados = todosEv.OrderByDescending(e => e.FechaHoraCompletaEvento).Skip((pagina - 1) * regPorPag).Take(regPorPag).ToList();
            ViewBag.PaginaActualDatosCompletos = pagina; ViewBag.TotalPaginasDatosCompletos = (int)Math.Ceiling((double)todosEv.Count / regPorPag);
            return View(evPaginados);
        }

        public ActionResult ExportarEventoCSV(int idEvento) { var r = ObtenerUnEventoEspecifico(idEvento); if (r == null) return HttpNotFound(); return File(Encoding.UTF8.GetBytes(r.ExportarCSV()), "text/csv", $"Ev_{idEvento}_{DateTime.Now:yyyyMMddHHmmss}.csv"); }
        public ActionResult ExportarEventoPDF(int idEvento) { var r = ObtenerUnEventoEspecifico(idEvento); if (r == null) return HttpNotFound(); return File(r.ExportarPDF(), "application/pdf", $"Ev_{idEvento}_{DateTime.Now:yyyyMMddHHmmss}.pdf"); }
        public ActionResult ExportarTodosLosEventosCSV() { var ev = ObtenerEventosDesdeDB(); if (ViewBag.ErrorMessage != null || !ev.Any()) { TempData["ExportError"] = ViewBag.ErrorMessage ?? "Sin datos"; return RedirectToAction("Index"); } return File(Encoding.UTF8.GetBytes(clsReporte.ExportarListaCSV(ev, "Listado Completo")), "text/csv", $"TodosEv_{DateTime.Now:yyyyMMddHHmmss}.csv"); }
        public ActionResult ExportarTodosLosEventosPDF() { var ev = ObtenerEventosDesdeDB(); if (ViewBag.ErrorMessage != null || !ev.Any()) { TempData["ExportError"] = ViewBag.ErrorMessage ?? "Sin datos"; return RedirectToAction("Index"); } return File(clsReporte.ExportarListaPDF(ev, "Listado Completo"), "application/pdf", $"TodosEv_{DateTime.Now:yyyyMMddHHmmss}.pdf"); }
        public ActionResult ExportarRecientesCSV() { var ev = ObtenerEventosDesdeDB(); if (ViewBag.ErrorMessage != null) { TempData["ExportError"] = ViewBag.ErrorMessage; return RedirectToAction("Index"); } var rec = ev.OrderByDescending(e => e.FechaHoraCompletaEvento).Take(5).ToList(); if (!rec.Any()) { TempData["ExportError"] = "Sin recientes"; return RedirectToAction("Index"); } return File(Encoding.UTF8.GetBytes(clsReporte.ExportarListaCSV(rec, "Recientes")), "text/csv", $"Recientes_{DateTime.Now:yyyyMMddHHmmss}.csv"); }
        public ActionResult ExportarRecientesPDF() { var ev = ObtenerEventosDesdeDB(); if (ViewBag.ErrorMessage != null) { TempData["ExportError"] = ViewBag.ErrorMessage; return RedirectToAction("Index"); } var rec = ev.OrderByDescending(e => e.FechaHoraCompletaEvento).Take(5).ToList(); if (!rec.Any()) { TempData["ExportError"] = "Sin recientes"; return RedirectToAction("Index"); } return File(clsReporte.ExportarListaPDF(rec, "Recientes"), "application/pdf", $"Recientes_{DateTime.Now:yyyyMMddHHmmss}.pdf"); }

        public ActionResult ExportarDashboardPrincipalPDF()
        {
            var todosEv = ObtenerEventosDesdeDB(); if (ViewBag.ErrorMessage != null) { TempData["ExportError"] = ViewBag.ErrorMessage; return RedirectToAction("Index"); }
            int totalD = todosEv.Count; int alertC = todosEv.Count(e => e.SeveridadEvento?.ToLower() == "alta" || e.SeveridadEvento?.ToLower() == "crítico");
            int persD = todosEv.Count(e => e.TipoEvento?.ToLower() == "person"); int evM = todosEv.Count(e => e.TipoEvento?.ToLower() == "mask");
            var rec = todosEv.OrderByDescending(e => e.FechaHoraCompletaEvento).Take(5).ToList();
            var tendData = todosEv.Where(e => e.FechaEvento.HasValue && e.FechaEvento.Value >= DateTime.Now.AddYears(-1)).GroupBy(e => new { e.FechaEvento.Value.Year, e.FechaEvento.Value.Month }).Select(g => new { MA = new DateTime(g.Key.Year, g.Key.Month, 1), C = g.Count() }).OrderBy(x => x.MA).ToList();
            var lblT = tendData.Select(td => td.MA.ToString("MMM yy")).ToList(); var dataT = tendData.Select(td => td.C).ToList();
            var tiposData = todosEv.GroupBy(e => e.TipoEvento).Select(g => new { T = g.Key, C = g.Count() }).OrderByDescending(x => x.C).ToList();
            var lblTipos = tiposData.Select(td => string.IsNullOrEmpty(td.T) ? "N/A" : td.T).ToList(); var dataTipos = tiposData.Select(td => td.C).ToList();
            var sevData = todosEv.GroupBy(e => e.SeveridadEvento).Select(g => new { S = g.Key, C = g.Count() }).OrderByDescending(x => x.C).ToList();
            var lblSev = sevData.Select(sd => string.IsNullOrEmpty(sd.S) ? "N/A" : sd.S).ToList(); var dataSev = sevData.Select(sd => sd.C).ToList();
            byte[] pdfBytes = clsReporte.ExportarDashboardPDF(totalD, alertC, persD, evM, rec, lblT, dataT, lblTipos, dataTipos, lblSev, dataSev, todosEv);
            return File(pdfBytes, "application/pdf", $"DashboardP_{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }

        public ActionResult ExportarDashboardPrincipalCSV()
        {
            var todosEv = ObtenerEventosDesdeDB(); if (ViewBag.ErrorMessage != null) { TempData["ExportError"] = ViewBag.ErrorMessage; return RedirectToAction("Index"); }
            int totalD = todosEv.Count; int alertC = todosEv.Count(e => e.SeveridadEvento?.ToLower() == "alta" || e.SeveridadEvento?.ToLower() == "crítico");
            int persD = todosEv.Count(e => e.TipoEvento?.ToLower() == "person"); int evM = todosEv.Count(e => e.TipoEvento?.ToLower() == "mask");
            var rec = todosEv.OrderByDescending(e => e.FechaHoraCompletaEvento).Take(5).ToList();
            var tendData = todosEv.Where(e => e.FechaEvento.HasValue && e.FechaEvento.Value >= DateTime.Now.AddYears(-1)).GroupBy(e => new { e.FechaEvento.Value.Year, e.FechaEvento.Value.Month }).Select(g => new { MA = new DateTime(g.Key.Year, g.Key.Month, 1), C = g.Count() }).OrderBy(x => x.MA).ToList();
            var lblT = tendData.Select(td => td.MA.ToString("MMM yy")).ToList(); var dataT = tendData.Select(td => td.C).ToList();
            var tiposData = todosEv.GroupBy(e => e.TipoEvento).Select(g => new { T = g.Key, C = g.Count() }).OrderByDescending(x => x.C).ToList();
            var lblTipos = tiposData.Select(td => string.IsNullOrEmpty(td.T) ? "N/A" : td.T).ToList(); var dataTipos = tiposData.Select(td => td.C).ToList();
            var sevData = todosEv.GroupBy(e => e.SeveridadEvento).Select(g => new { S = g.Key, C = g.Count() }).OrderByDescending(x => x.C).ToList();
            var lblSev = sevData.Select(sd => string.IsNullOrEmpty(sd.S) ? "N/A" : sd.S).ToList(); var dataSev = sevData.Select(sd => sd.C).ToList();
            string csvContent = clsReporte.ExportarDashboardCSV(totalD, alertC, persD, evM, rec, lblT, dataT, lblTipos, dataTipos, lblSev, dataSev, todosEv);
            return File(Encoding.UTF8.GetBytes(csvContent), "text/csv", $"DashboardP_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }
    }
}