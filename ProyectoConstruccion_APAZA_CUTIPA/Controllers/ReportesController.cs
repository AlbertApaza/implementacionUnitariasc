using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text; // Para StringBuilder
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json; // Para serializar/deserializar FiltrosJson
using ProyectoConstruccion_APAZA_CUTIPA.Models; // Asegúrate que este namespace sea correcto

namespace ProyectoConstruccion_APAZA_CUTIPA.Controllers
{
    public class ReportesController : Controller
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["MySqlConexion"].ConnectionString;

        private int GetCurrentUserId()
        {
            if (Session["UserId"] == null)
            {
                // Para pruebas, asignamos un ID. En producción, autenticar.
                // Asegúrate que el usuario con ID 1 exista en tu tabla `usuario`
                Session["UserId"] = 1;
            }
            return (int)Session["UserId"];
        }

        public ActionResult Index()
        {
            ViewBag.CurrentUserId = GetCurrentUserId(); // Pasar a la vista para lógica de UI
            return View();
        }

        [HttpPost]
        public ActionResult GetDeteccionData(ReportFiltersViewModel filtros)
        {
            if (filtros == null)
            {
                return Json(new { success = false, message = "Filtros no proporcionados." });
            }
            // Podrías añadir validación de ModelState aquí si usas DataAnnotations en ReportFiltersViewModel

            List<ChartDataItem> chartData = new List<ChartDataItem>();
            var sqlBuilder = new StringBuilder("SELECT ");
            var groupByClauseSql = new StringBuilder();
            var whereClauseSql = new StringBuilder(" WHERE 1=1");
            List<MySqlParameter> parameters = new List<MySqlParameter>();

            // Construir SELECT y GROUP BY basados en GroupByFilter
            switch (filtros.GroupByFilter?.ToLower())
            {
                case "day":
                    sqlBuilder.Append("DATE_FORMAT(fecha, '%Y-%m-%d') AS Label, COUNT(*) AS Value ");
                    groupByClauseSql.Append("GROUP BY DATE_FORMAT(fecha, '%Y-%m-%d') ORDER BY Label ASC");
                    break;
                case "week":
                    sqlBuilder.Append("CONCAT(YEAR(fecha), '-W', LPAD(WEEK(fecha,1), 2, '0')) AS Label, COUNT(*) AS Value ");
                    groupByClauseSql.Append("GROUP BY YEARWEEK(fecha, 1), Label ORDER BY Label ASC");
                    break;
                case "type":
                    sqlBuilder.Append("IFNULL(tipo, 'N/A') AS Label, COUNT(*) AS Value ");
                    groupByClauseSql.Append("GROUP BY tipo ORDER BY Label ASC");
                    break;
                case "severity":
                    sqlBuilder.Append("IFNULL(severidad, 'N/A') AS Label, COUNT(*) AS Value ");
                    groupByClauseSql.Append("GROUP BY severidad ORDER BY Label ASC");
                    break;
                case "month":
                default:
                    sqlBuilder.Append("DATE_FORMAT(fecha, '%Y-%m') AS Label, COUNT(*) AS Value ");
                    groupByClauseSql.Append("GROUP BY DATE_FORMAT(fecha, '%Y-%m') ORDER BY Label ASC");
                    break;
            }
            sqlBuilder.Append("FROM tbDeteccionEventos ");

            // Filtro de Fecha
            if (!string.IsNullOrEmpty(filtros.StartDate) && !string.IsNullOrEmpty(filtros.EndDate))
            {
                if (DateTime.TryParseExact(filtros.StartDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startDate) &&
                    DateTime.TryParseExact(filtros.EndDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime endDate))
                {
                    whereClauseSql.Append(" AND fecha >= @startDate AND fecha <= @endDate");
                    parameters.Add(new MySqlParameter("@startDate", startDate.ToString("yyyy-MM-dd")));
                    parameters.Add(new MySqlParameter("@endDate", endDate.ToString("yyyy-MM-dd")));
                }
            }

            // Filtro de Tipo de Detección
            List<string> tiposSeleccionados = new List<string>();
            // IMPORTANTE: Estos strings DEBEN coincidir con los valores en tu columna `tipo`
            if (filtros.CheckPersonas) tiposSeleccionados.Add("Personas Sospechosas");
            if (filtros.CheckArmasBlancas) tiposSeleccionados.Add("Armas Blancas");
            if (filtros.CheckArmasFuego) tiposSeleccionados.Add("Armas de Fuego");

            if (tiposSeleccionados.Any())
            {
                whereClauseSql.Append(" AND tipo IN (");
                for (int i = 0; i < tiposSeleccionados.Count; i++)
                {
                    string paramName = "@tipoParam" + i;
                    whereClauseSql.Append((i > 0 ? "," : "") + paramName);
                    parameters.Add(new MySqlParameter(paramName, tiposSeleccionados[i]));
                }
                whereClauseSql.Append(")");
            }
            else
            {
                // Si no se marca ningún checkbox de tipo, no se aplica filtro por tipo específico,
                // o se puede forzar a no devolver nada si esa es la lógica deseada:
                // whereClauseSql.Append(" AND 1=0"); 
            }

            // Filtro de Severidad
            if (!string.IsNullOrEmpty(filtros.SeverityFilter) && filtros.SeverityFilter.ToLower() != "all")
            {
                // IMPORTANTE: El valor de filtros.SeverityFilter debe coincidir con los valores en tu columna `severidad`
                whereClauseSql.Append(" AND severidad = @severidad");
                parameters.Add(new MySqlParameter("@severidad", filtros.SeverityFilter));
            }

            string finalQuery = sqlBuilder.ToString() + whereClauseSql.ToString() + " " + groupByClauseSql.ToString();

            System.Diagnostics.Debug.WriteLine("--- GetDeteccionData ---");
            System.Diagnostics.Debug.WriteLine("Query SQL: " + finalQuery);
            foreach (var p in parameters) { System.Diagnostics.Debug.WriteLine($"Param: {p.ParameterName} = '{p.Value}'"); }

            try
            {
                using (MySqlConnection con = new MySqlConnection(connectionString))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(finalQuery, con))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                chartData.Add(new ChartDataItem
                                {
                                    Label = reader["Label"]?.ToString() ?? "N/A",
                                    Value = Convert.ToInt32(reader["Value"])
                                });
                            }
                        }
                    }
                }
                System.Diagnostics.Debug.WriteLine($"Datos encontrados para gráfico: {chartData.Count}");
                return Json(new { success = true, data = chartData });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR en GetDeteccionData: " + ex.ToString());
                return Json(new { success = false, message = "Error al obtener datos para el gráfico. Revise logs del servidor." });
            }
        }

        [HttpPost]
        public ActionResult SaveReportConfiguration(SaveReportRequestViewModel request)
        {
            if (request == null || request.FiltrosPanel == null)
            {
                return Json(new { success = false, message = "Datos de solicitud incompletos." });
            }
            if (string.IsNullOrWhiteSpace(request.NombreModal))
            {
                return Json(new { success = false, message = "El nombre del reporte es obligatorio." });
            }

            int currentUserId = GetCurrentUserId();
            var configToSave = new SavedReportConfiguration
            {
                IdReporte = request.IdReporte,
                Nombre = request.NombreModal,
                Descripcion = request.DescripcionModal,
                TipoGrafico = request.TipoGraficoPanel,
                FiltrosJson = JsonConvert.SerializeObject(request.FiltrosPanel),
                EsPublico = request.EsPublicoModal,
                IdUsuarioCreador = currentUserId
            };

            System.Diagnostics.Debug.WriteLine("--- SaveReportConfiguration ---");
            System.Diagnostics.Debug.WriteLine($"Guardando reporte: ID={configToSave.IdReporte}, Nombre='{configToSave.Nombre}'");
            System.Diagnostics.Debug.WriteLine($"Filtros JSON: {configToSave.FiltrosJson}");

            using (MySqlConnection con = new MySqlConnection(connectionString))
            {
                try
                {
                    con.Open();
                    string query;
                    bool isUpdate = configToSave.IdReporte > 0;

                    if (isUpdate)
                    {
                        query = @"UPDATE tbReporte SET 
                                    Nombre = @Nombre, Descripcion = @Descripcion, TipoGrafico = @TipoGrafico, 
                                    Filtros = @FiltrosJson, EsPublico = @EsPublico, FechaModificacion = CURRENT_TIMESTAMP()
                                  WHERE idReporte = @IdReporte AND idUsuarioCreador = @IdUsuarioCreador";
                    }
                    else
                    {
                        query = @"INSERT INTO tbReporte (Nombre, Descripcion, TipoGrafico, Filtros, EsPublico, idUsuarioCreador, FechaCreacion) 
                                  VALUES (@Nombre, @Descripcion, @TipoGrafico, @FiltrosJson, @EsPublico, @IdUsuarioCreador, CURRENT_TIMESTAMP())";
                    }

                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Nombre", configToSave.Nombre);
                    cmd.Parameters.AddWithValue("@Descripcion", string.IsNullOrWhiteSpace(configToSave.Descripcion) ? (object)DBNull.Value : configToSave.Descripcion);
                    cmd.Parameters.AddWithValue("@TipoGrafico", configToSave.TipoGrafico);
                    cmd.Parameters.AddWithValue("@FiltrosJson", configToSave.FiltrosJson); // Guardar el string JSON
                    cmd.Parameters.AddWithValue("@EsPublico", configToSave.EsPublico);
                    cmd.Parameters.AddWithValue("@IdUsuarioCreador", configToSave.IdUsuarioCreador);
                    if (isUpdate)
                    {
                        cmd.Parameters.AddWithValue("@IdReporte", configToSave.IdReporte);
                    }

                    int result = cmd.ExecuteNonQuery();
                    if (result > 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Reporte guardado/actualizado exitosamente.");
                        return Json(new { success = true, message = "Configuración de reporte guardada correctamente." });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No se afectaron filas al guardar/actualizar el reporte.");
                        return Json(new { success = false, message = isUpdate ? "No se pudo actualizar o no tiene permiso." : "No se pudo guardar." });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR en SaveReportConfiguration: " + ex.ToString());
                    return Json(new { success = false, message = "Error al guardar configuración: " + ex.Message });
                }
            }
        }

        [HttpGet]
        public ActionResult GetSavedReports()
        {
            int currentUserId = GetCurrentUserId();
            List<SavedReportConfiguration> reportes = new List<SavedReportConfiguration>();
            System.Diagnostics.Debug.WriteLine("--- GetSavedReports ---");

            using (MySqlConnection con = new MySqlConnection(connectionString))
            {
                try
                {
                    con.Open();
                    // Columna 'Filtros' en BD se mapea a 'FiltrosJson' en el modelo
                    string query = @"SELECT idReporte, Nombre, Descripcion, TipoGrafico, Filtros AS FiltrosJson, 
                                            EsPublico, idUsuarioCreador, FechaCreacion, FechaModificacion 
                                     FROM tbReporte 
                                     WHERE idUsuarioCreador = @idUsuarioCreador OR EsPublico = 1
                                     ORDER BY FechaCreacion DESC";
                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@idUsuarioCreador", currentUserId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reportes.Add(new SavedReportConfiguration
                            {
                                IdReporte = Convert.ToInt32(reader["idReporte"]),
                                Nombre = reader["Nombre"].ToString(),
                                Descripcion = reader["Descripcion"]?.ToString(),
                                TipoGrafico = reader["TipoGrafico"].ToString(),
                                FiltrosJson = reader["FiltrosJson"].ToString(),
                                EsPublico = Convert.ToBoolean(reader["EsPublico"]),
                                IdUsuarioCreador = reader["idUsuarioCreador"] != DBNull.Value ? Convert.ToInt32(reader["idUsuarioCreador"]) : (int?)null,
                                FechaCreacion = Convert.ToDateTime(reader["FechaCreacion"]),
                                FechaModificacion = reader["FechaModificacion"] != DBNull.Value ? Convert.ToDateTime(reader["FechaModificacion"]) : (DateTime?)null
                            });
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"Reportes guardados encontrados: {reportes.Count}");
                    return Json(new { success = true, data = reportes }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR en GetSavedReports: " + ex.ToString());
                    return Json(new { success = false, message = "Error al cargar reportes guardados." }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [HttpPost]
        public ActionResult DeleteReport(int idReporte)
        {
            if (idReporte <= 0)
            {
                return Json(new { success = false, message = "ID de reporte inválido." });
            }
            int currentUserId = GetCurrentUserId();
            System.Diagnostics.Debug.WriteLine($"--- DeleteReport: ID={idReporte}, UserID={currentUserId} ---");

            using (MySqlConnection con = new MySqlConnection(connectionString))
            {
                try
                {
                    con.Open();
                    string query = "DELETE FROM tbReporte WHERE idReporte = @idReporte AND idUsuarioCreador = @idUsuarioCreador";
                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@idReporte", idReporte);
                    cmd.Parameters.AddWithValue("@idUsuarioCreador", currentUserId);
                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Reporte eliminado exitosamente.");
                        return Json(new { success = true, message = "Reporte eliminado." });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No se eliminó el reporte (no encontrado o sin permiso).");
                        return Json(new { success = false, message = "No se pudo eliminar o no tiene permiso." });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR en DeleteReport: " + ex.ToString());
                    return Json(new { success = false, message = "Error al eliminar reporte: " + ex.Message });
                }
            }
        }
    }
}