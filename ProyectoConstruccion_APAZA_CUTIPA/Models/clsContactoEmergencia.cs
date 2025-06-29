using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;

namespace ProyectoConstruccion_APAZA_CUTIPA.Models
{
    public class clsContactoEmergencia
    {
        public int IdContactoEmergencia { get; set; }
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string NumeroTelefono { get; set; }
        public string Parentesco { get; set; }
        public bool EsPrincipal { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string ParentescoOtro { get; set; }

        private static readonly string _conexion = ConfigurationManager.ConnectionStrings["MySqlConexion"].ConnectionString;

        public static List<clsContactoEmergencia> ObtenerPorUsuario(int idUsuario, out string mensajeError)
        {
            mensajeError = string.Empty;
            List<clsContactoEmergencia> contactos = new List<clsContactoEmergencia>();
            try
            {
                using (MySqlConnection con = new MySqlConnection(_conexion))
                {
                    con.Open();
                    string query = @"SELECT IdContactoEmergencia, IdUsuario, Nombre, Apellido, 
                                    NumeroTelefono, Parentesco, EsPrincipal, FechaCreacion 
                                    FROM tbContactoEmergencia 
                                    WHERE IdUsuario = @IdUsuario 
                                    ORDER BY EsPrincipal DESC, Nombre ASC";
                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            contactos.Add(new clsContactoEmergencia
                            {
                                IdContactoEmergencia = Convert.ToInt32(reader["IdContactoEmergencia"]),
                                IdUsuario = Convert.ToInt32(reader["IdUsuario"]),
                                Nombre = reader["Nombre"].ToString(),
                                Apellido = reader["Apellido"].ToString(),
                                NumeroTelefono = reader["NumeroTelefono"].ToString(),
                                Parentesco = reader["Parentesco"].ToString(),
                                EsPrincipal = Convert.ToBoolean(reader["EsPrincipal"]),
                                FechaCreacion = Convert.ToDateTime(reader["FechaCreacion"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                mensajeError = "Error al cargar contactos: " + ex.Message;
            }
            return contactos;
        }

        public static bool Agregar(clsContactoEmergencia contacto, int idUsuario, out string mensajeError)
        {
            mensajeError = string.Empty;
            try
            {
                if (contacto.EsPrincipal)
                {
                    DesmarcarPrincipalAnterior(idUsuario, 0);
                }
                using (MySqlConnection con = new MySqlConnection(_conexion))
                {
                    con.Open();
                    string query = "INSERT INTO tbContactoEmergencia (IdUsuario, Nombre, Apellido, NumeroTelefono, Parentesco, EsPrincipal) " +
                                   "VALUES (@IdUsuario, @Nombre, @Apellido, @NumeroTelefono, @Parentesco, @EsPrincipal)";
                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    cmd.Parameters.AddWithValue("@Nombre", contacto.Nombre ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Apellido", contacto.Apellido ?? string.Empty);
                    cmd.Parameters.AddWithValue("@NumeroTelefono", contacto.NumeroTelefono ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Parentesco", contacto.Parentesco ?? string.Empty);
                    cmd.Parameters.AddWithValue("@EsPrincipal", contacto.EsPrincipal);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (MySqlException ex)
            {
                mensajeError = "Error al guardar: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                mensajeError = "Error general al guardar: " + ex.Message;
                return false;
            }
        }

        public static clsContactoEmergencia ObtenerPorId(int idContactoEmergencia, int idUsuario, out string mensajeError)
        {
            mensajeError = string.Empty;
            clsContactoEmergencia contacto = null;
            try
            {
                using (MySqlConnection con = new MySqlConnection(_conexion))
                {
                    con.Open();
                    string query = "SELECT IdContactoEmergencia, Nombre, Apellido, NumeroTelefono, Parentesco, EsPrincipal " +
                                   "FROM tbContactoEmergencia WHERE IdContactoEmergencia = @IdContactoEmergencia AND IdUsuario = @IdUsuario";
                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@IdContactoEmergencia", idContactoEmergencia);
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            contacto = new clsContactoEmergencia
                            {
                                IdContactoEmergencia = Convert.ToInt32(reader["IdContactoEmergencia"]),
                                Nombre = reader["Nombre"].ToString(),
                                Apellido = reader["Apellido"].ToString(),
                                NumeroTelefono = reader["NumeroTelefono"].ToString(),
                                Parentesco = reader["Parentesco"].ToString(),
                                EsPrincipal = Convert.ToBoolean(reader["EsPrincipal"])
                            };
                        }
                        else
                        {
                            mensajeError = "Contacto no encontrado.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                mensajeError = "Error al obtener contacto: " + ex.Message;
            }
            return contacto;
        }

        public static bool Editar(clsContactoEmergencia contacto, int idUsuario, out string mensajeError)
        {
            mensajeError = string.Empty;
            try
            {
                if (contacto.EsPrincipal)
                {
                    DesmarcarPrincipalAnterior(idUsuario, contacto.IdContactoEmergencia);
                }
                using (MySqlConnection con = new MySqlConnection(_conexion))
                {
                    con.Open();
                    string query = "UPDATE tbContactoEmergencia SET Nombre = @Nombre, Apellido = @Apellido, " +
                                   "NumeroTelefono = @NumeroTelefono, Parentesco = @Parentesco, EsPrincipal = @EsPrincipal " +
                                   "WHERE IdContactoEmergencia = @IdContactoEmergencia AND IdUsuario = @IdUsuario";
                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Nombre", contacto.Nombre ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Apellido", contacto.Apellido ?? string.Empty);
                    cmd.Parameters.AddWithValue("@NumeroTelefono", contacto.NumeroTelefono ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Parentesco", contacto.Parentesco ?? string.Empty);
                    cmd.Parameters.AddWithValue("@EsPrincipal", contacto.EsPrincipal);
                    cmd.Parameters.AddWithValue("@IdContactoEmergencia", contacto.IdContactoEmergencia);
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (MySqlException ex)
            {
                mensajeError = "Error al actualizar: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                mensajeError = "Error general al actualizar: " + ex.Message;
                return false;
            }
        }

        public static bool MarcarComoPrincipal(int idContactoEmergencia, int idUsuario, out string mensajeError)
        {
            mensajeError = string.Empty;
            try
            {
                DesmarcarPrincipalAnterior(idUsuario, idContactoEmergencia);
                using (MySqlConnection con = new MySqlConnection(_conexion))
                {
                    con.Open();
                    string queryMarcar = "UPDATE tbContactoEmergencia SET EsPrincipal = 1 WHERE IdContactoEmergencia = @IdContactoEmergencia AND IdUsuario = @IdUsuario";
                    MySqlCommand cmdMarcar = new MySqlCommand(queryMarcar, con);
                    cmdMarcar.Parameters.AddWithValue("@IdContactoEmergencia", idContactoEmergencia);
                    cmdMarcar.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    return cmdMarcar.ExecuteNonQuery() > 0;
                }
            }
            catch (MySqlException ex)
            {
                mensajeError = "Error al marcar como principal: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                mensajeError = "Error general al marcar como principal: " + ex.Message;
                return false;
            }
        }

        public static bool Eliminar(int idContactoEmergencia, int idUsuario, out string mensajeError)
        {
            mensajeError = string.Empty;
            try
            {
                using (MySqlConnection con = new MySqlConnection(_conexion))
                {
                    con.Open();
                    string query = "DELETE FROM tbContactoEmergencia WHERE IdContactoEmergencia = @IdContactoEmergencia AND IdUsuario = @IdUsuario";
                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@IdContactoEmergencia", idContactoEmergencia);
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (MySqlException ex)
            {
                mensajeError = "Error al eliminar: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                mensajeError = "Error general al eliminar: " + ex.Message;
                return false;
            }
        }

        public static void DesmarcarPrincipalAnterior(int idUsuario, int idContactoAExcluirSiSeEstaMarcando)
        {
            using (MySqlConnection con = new MySqlConnection(_conexion))
            {
                con.Open();
                string query = "UPDATE tbContactoEmergencia SET EsPrincipal = 0 WHERE IdUsuario = @IdUsuario AND EsPrincipal = 1";
                if (idContactoAExcluirSiSeEstaMarcando > 0)
                {
                    query += " AND IdContactoEmergencia != @IdContactoAExcluirSiSeEstaMarcando";
                }
                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                if (idContactoAExcluirSiSeEstaMarcando > 0)
                {
                    cmd.Parameters.AddWithValue("@IdContactoAExcluirSiSeEstaMarcando", idContactoAExcluirSiSeEstaMarcando);
                }
                cmd.ExecuteNonQuery();
            }
        }
    }
}