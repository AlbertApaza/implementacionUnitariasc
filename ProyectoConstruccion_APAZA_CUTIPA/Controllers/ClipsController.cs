// Controllers/ClipsController.cs
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using ProyectoConstruccion_APAZA_CUTIPA.Models;
using System.Text; // Para StringBuilder

namespace ProyectoConstruccion_APAZA_CUTIPA.Controllers
{
    public class ClipsController : Controller
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["MySqlConexion"].ConnectionString;

        // GET: Clips
        public ActionResult Index(
            string sortOrder = "desc",
            string filtroTipo = null,
            string filtroSeveridad = null,
            DateTime? filtroFechaDesde = null,
            DateTime? filtroFechaHasta = null,
            TimeSpan? filtroHoraDesde = null,
            TimeSpan? filtroHoraHasta = null,
            int pagina = 1) // Para paginación
        {
            List<EventoClipViewModel> clips = new List<EventoClipViewModel>();
            int totalRegistros = 0;
            int registrosPorPagina = 12; // O el número que prefieras para la cuadrícula

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    var sqlBuilder = new StringBuilder();
                    sqlBuilder.Append("SELECT idEvento, fecha, hora, tipo, subtipo, severidad, ubicacion, confianza, estado, IDdrive ");
                    sqlBuilder.Append("FROM tbDeteccionEventos ");
                    sqlBuilder.Append("WHERE IDdrive IS NOT NULL AND IDdrive <> '' ");

                    var parameters = new List<MySqlParameter>();

                    if (!string.IsNullOrEmpty(filtroTipo) && filtroTipo.ToLower() != "todos")
                    {
                        sqlBuilder.Append("AND tipo = @Tipo ");
                        parameters.Add(new MySqlParameter("@Tipo", filtroTipo));
                    }
                    if (!string.IsNullOrEmpty(filtroSeveridad) && filtroSeveridad.ToLower() != "todas")
                    {
                        sqlBuilder.Append("AND severidad = @Severidad ");
                        parameters.Add(new MySqlParameter("@Severidad", filtroSeveridad));
                    }
                    if (filtroFechaDesde.HasValue)
                    {
                        sqlBuilder.Append("AND fecha >= @FechaDesde ");
                        parameters.Add(new MySqlParameter("@FechaDesde", filtroFechaDesde.Value));
                    }
                    if (filtroFechaHasta.HasValue)
                    {
                        // Ajustar para incluir el día completo
                        sqlBuilder.Append("AND fecha <= @FechaHasta ");
                        parameters.Add(new MySqlParameter("@FechaHasta", filtroFechaHasta.Value));
                    }
                    if (filtroHoraDesde.HasValue)
                    {
                        sqlBuilder.Append("AND hora >= @HoraDesde ");
                        parameters.Add(new MySqlParameter("@HoraDesde", filtroHoraDesde.Value));
                    }
                    if (filtroHoraHasta.HasValue)
                    {
                        sqlBuilder.Append("AND hora <= @HoraHasta ");
                        parameters.Add(new MySqlParameter("@HoraHasta", filtroHoraHasta.Value));
                    }

                    // Contar total de registros con filtros aplicados (antes de paginación)
                    string countQuery = sqlBuilder.ToString().Replace("SELECT idEvento, fecha, hora, tipo, subtipo, severidad, ubicacion, confianza, estado, IDdrive", "SELECT COUNT(*)");
                    using (MySqlCommand countCmd = new MySqlCommand(countQuery, conn))
                    {
                        countCmd.Parameters.AddRange(parameters.ToArray());
                        totalRegistros = Convert.ToInt32(countCmd.ExecuteScalar());
                    }


                    // Aplicar ordenamiento
                    if (sortOrder == "asc")
                    {
                        sqlBuilder.Append("ORDER BY fecha ASC, hora ASC ");
                    }
                    else
                    {
                        sqlBuilder.Append("ORDER BY fecha DESC, hora DESC ");
                    }

                    // Aplicar paginación
                    int offset = (pagina - 1) * registrosPorPagina;
                    sqlBuilder.Append("LIMIT @Offset, @RegistrosPorPagina;");
                    parameters.Add(new MySqlParameter("@Offset", offset));
                    parameters.Add(new MySqlParameter("@RegistrosPorPagina", registrosPorPagina));


                    using (MySqlCommand cmd = new MySqlCommand(sqlBuilder.ToString(), conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                clips.Add(new EventoClipViewModel
                                {
                                    IdEvento = reader.GetInt32("idEvento"),
                                    Fecha = reader.GetDateTime("fecha"),
                                    Hora = reader.GetTimeSpan("hora"),
                                    Tipo = reader.GetString("tipo"),
                                    Subtipo = reader.IsDBNull(reader.GetOrdinal("subtipo")) ? null : reader.GetString("subtipo"),
                                    Severidad = reader.IsDBNull(reader.GetOrdinal("severidad")) ? null : reader.GetString("severidad"),
                                    Ubicacion = reader.IsDBNull(reader.GetOrdinal("ubicacion")) ? null : reader.GetString("ubicacion"),
                                    Confianza = reader.IsDBNull(reader.GetOrdinal("confianza")) ? (decimal?)null : reader.GetDecimal("confianza"),
                                    Estado = reader.IsDBNull(reader.GetOrdinal("estado")) ? null : reader.GetString("estado"),
                                    IDdrive = reader.GetString("IDdrive"),
                                    NombreClip = $"Evento {reader.GetString("tipo")} - {reader.GetDateTime("fecha"):dd/MM/yyyy}"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al conectar con la base de datos: " + ex.Message;
                System.Diagnostics.Debug.WriteLine("Error DB Clips: " + ex.ToString());
            }

            // Pasar los valores de los filtros a la vista para mantener su estado
            ViewBag.CurrentSortOrder = sortOrder;
            ViewBag.CurrentFiltroTipo = filtroTipo;
            ViewBag.CurrentFiltroSeveridad = filtroSeveridad;
            ViewBag.CurrentFiltroFechaDesde = filtroFechaDesde?.ToString("yyyy-MM-dd");
            ViewBag.CurrentFiltroFechaHasta = filtroFechaHasta?.ToString("yyyy-MM-dd");
            ViewBag.CurrentFiltroHoraDesde = filtroHoraDesde?.ToString(@"hh\:mm");
            ViewBag.CurrentFiltroHoraHasta = filtroHoraHasta?.ToString(@"hh\:mm");

            // Paginación
            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalRegistros / registrosPorPagina);
            ViewBag.TotalRegistros = totalRegistros;


            // Obtener valores únicos para los dropdowns de Tipo y Severidad
            ViewBag.TiposDisponibles = ObtenerValoresUnicosDeColumna("tipo");
            ViewBag.SeveridadesDisponibles = ObtenerValoresUnicosDeColumna("severidad");


            return View(clips);
        }

        private List<string> ObtenerValoresUnicosDeColumna(string nombreColumna)
        {
            var valores = new List<string>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    // Asegurarse de que el nombre de columna es seguro y no permite inyección SQL
                    // Aquí asumimos que nombreColumna es "tipo" o "severidad"
                    if (nombreColumna != "tipo" && nombreColumna != "severidad")
                    {
                        throw new ArgumentException("Nombre de columna no válido para obtener valores únicos.");
                    }

                    string query = $"SELECT DISTINCT {nombreColumna} FROM tbDeteccionEventos WHERE {nombreColumna} IS NOT NULL AND {nombreColumna} <> '' ORDER BY {nombreColumna} ASC;";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                valores.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo valores únicos para {nombreColumna}: {ex.Message}");
            }
            return valores;
        }

        [HttpGet] // Importante para permitir llamadas GET desde JS
        public JsonResult GetEventoPorDriveId(string idDrive)
        {
            if (string.IsNullOrEmpty(idDrive))
            {
                return Json(null, JsonRequestBehavior.AllowGet); // O un error apropiado
            }

            EventoClipViewModel eventoInfo = null;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT idEvento, fecha, hora, tipo, subtipo, severidad, ubicacion, confianza, estado, IDdrive " +
                                   "FROM tbDeteccionEventos WHERE IDdrive = @IDdrive LIMIT 1;";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IDdrive", idDrive);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                eventoInfo = new EventoClipViewModel // Usa el modelo que ya tienes
                                {
                                    IdEvento = reader.GetInt32("idEvento"),
                                    Fecha= reader.GetDateTime("fecha"),
                                    Hora = reader.GetTimeSpan("hora"),
                                    Tipo = reader.GetString("tipo"),
                                    Subtipo = reader.IsDBNull(reader.GetOrdinal("subtipo")) ? null : reader.GetString("subtipo"),
                                    Severidad= reader.IsDBNull(reader.GetOrdinal("severidad")) ? null : reader.GetString("severidad"),
                                    Ubicacion = reader.IsDBNull(reader.GetOrdinal("ubicacion")) ? null : reader.GetString("ubicacion"),
                                    Confianza= reader.IsDBNull(reader.GetOrdinal("confianza")) ? (decimal?)null : reader.GetDecimal("confianza"),
                                    Estado= reader.IsDBNull(reader.GetOrdinal("estado")) ? null : reader.GetString("estado"),
                                    IDdrive = reader.GetString("IDdrive") // Redundante aquí, pero para completar el modelo
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en GetEventoPorDriveId ({idDrive}): {ex.Message}");
                // Podrías retornar un HttpStatusCode.InternalServerError aquí si lo deseas
                return Json(new { error = "Error al consultar la base de datos." }, JsonRequestBehavior.AllowGet);
            }
            return Json(eventoInfo, JsonRequestBehavior.AllowGet); // Devuelve el evento o null si no se encuentra
        }

    }
}