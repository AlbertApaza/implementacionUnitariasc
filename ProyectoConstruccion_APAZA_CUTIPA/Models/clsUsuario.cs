using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;

namespace ProyectoConstruccion_APAZA_CUTIPA.Models
{
    public class clsUsuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Contrasena { get; set; }
        public string TokenFcm { get; set; }
        public string TipoUsuario { get; set; }
        public string MetodoRegistro { get; set; }
        public bool Aprobado { get; set; }
        public DateTime FechaRegistro { get; set; }

        private static readonly string _conexion = ConfigurationManager.ConnectionStrings["MySqlConexion"].ConnectionString;

        public static clsUsuario Autenticar(string correo, string contrasena, out string mensajeError, out bool esAprobado)
        {
            mensajeError = string.Empty;
            esAprobado = false;
            clsUsuario usuario = null;

            using (MySqlConnection con = new MySqlConnection(_conexion))
            {
                con.Open();
                string query = "SELECT Id, Nombre, Correo, Contrasena, TipoUsuario, Aprobado, MetodoRegistro FROM usuario WHERE Correo = @correo";
                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@correo", correo);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        esAprobado = Convert.ToBoolean(reader["Aprobado"]);
                        string passBD = reader["Contrasena"]?.ToString();

                        if (!esAprobado)
                        {
                            mensajeError = "Usted sigue en espera de aceptación.";
                            return null;
                        }
                        if (passBD == contrasena)
                        {
                            usuario = new clsUsuario
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nombre = reader["Nombre"].ToString(),
                                Correo = reader["Correo"].ToString(),
                                TipoUsuario = reader["TipoUsuario"].ToString(),
                                MetodoRegistro = reader["MetodoRegistro"]?.ToString(),
                                Aprobado = esAprobado,
                                Contrasena = null
                            };
                        }
                        else
                        {
                            mensajeError = "Contraseña incorrecta.";
                        }
                    }
                    else
                    {
                        mensajeError = "Correo no registrado.";
                    }
                }
            }
            return usuario;
        }

        public static bool VerificarCorreoExistente(string correo, out bool estaAprobado, out string mensaje)
        {
            estaAprobado = false;
            mensaje = string.Empty;
            using (MySqlConnection con = new MySqlConnection(_conexion))
            {
                con.Open();
                string checkQuery = "SELECT Aprobado FROM usuario WHERE Correo = @correo";
                MySqlCommand checkCmd = new MySqlCommand(checkQuery, con);
                checkCmd.Parameters.AddWithValue("@correo", correo);
                var aprobadoObj = checkCmd.ExecuteScalar();
                if (aprobadoObj != null)
                {
                    estaAprobado = Convert.ToBoolean(aprobadoObj);
                    if (!estaAprobado)
                    {
                        mensaje = "Usted sigue en espera de aceptación.";
                    }
                    else
                    {
                        mensaje = "Este correo ya está registrado y aprobado.";
                    }
                    return true;
                }
                return false;
            }
        }

        public static bool Registrar(clsUsuario usuario, out string mensajeError)
        {
            mensajeError = string.Empty;
            using (MySqlConnection con = new MySqlConnection(_conexion))
            {
                con.Open();
                string query = "INSERT INTO usuario (Nombre, Correo, Contrasena, TipoUsuario, MetodoRegistro, Aprobado, FechaRegistro) " +
                               "VALUES (@nombre, @correo, @contrasena, 'Invitado', 'Credenciales', 0, NOW())";
                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@nombre", usuario.Nombre);
                cmd.Parameters.AddWithValue("@correo", usuario.Correo);
                cmd.Parameters.AddWithValue("@contrasena", usuario.Contrasena);
                try
                {
                    cmd.ExecuteNonQuery();
                    return true;
                }
                catch (MySqlException ex)
                {
                    mensajeError = "Error al registrar: " + ex.Message;
                    return false;
                }
            }
        }

        public static clsUsuario ObtenerOInsertarUsuarioGoogle(string email, string nombre, out bool necesitaAprobacion, out string mensajeProceso)
        {
            necesitaAprobacion = false;
            mensajeProceso = string.Empty;
            clsUsuario usuario = null;

            using (MySqlConnection con = new MySqlConnection(_conexion))
            {
                con.Open();
                string query = "SELECT Id, Nombre, Correo, TipoUsuario, Aprobado FROM usuario WHERE Correo = @correo";
                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@correo", email);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        usuario = new clsUsuario
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Nombre = reader["Nombre"].ToString(),
                            Correo = reader["Correo"].ToString(),
                            TipoUsuario = reader["TipoUsuario"].ToString(),
                            Aprobado = Convert.ToBoolean(reader["Aprobado"])
                        };
                        reader.Close();

                        if (!usuario.Aprobado)
                        {
                            necesitaAprobacion = true;
                            mensajeProceso = "Usted sigue en espera de aceptación (Google).";
                            return usuario;
                        }

                        string updateQuery = "UPDATE usuario SET MetodoRegistro = 'Google', Contrasena = NULL WHERE Id = @id AND (MetodoRegistro IS NULL OR MetodoRegistro != 'Google')";
                        MySqlCommand updateCmd = new MySqlCommand(updateQuery, con);
                        updateCmd.Parameters.AddWithValue("@id", usuario.Id);
                        updateCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        reader.Close();
                        string insertQuery = "INSERT INTO usuario (Nombre, Correo, MetodoRegistro, TipoUsuario, Aprobado, FechaRegistro, Contrasena) " +
                                             "VALUES (@nombre, @correo, 'Google', 'Invitado', 0, NOW(), NULL)";
                        MySqlCommand insertCmd = new MySqlCommand(insertQuery, con);
                        insertCmd.Parameters.AddWithValue("@nombre", nombre);
                        insertCmd.Parameters.AddWithValue("@correo", email);
                        insertCmd.ExecuteNonQuery();

                        long newUserId = insertCmd.LastInsertedId;
                        usuario = new clsUsuario { Id = (int)newUserId, Nombre = nombre, Correo = email, TipoUsuario = "Invitado", Aprobado = false };
                        necesitaAprobacion = true;
                        mensajeProceso = "Petición de Acceso con Google enviada. Pendiente de aprobación.";
                    }
                }
            }
            return usuario;
        }

        public static clsUsuario ProcesarUsuarioGoogleApi(string email, string name, out bool esNuevoUsuarioPendiente, out bool yaAprobado, out string mensaje)
        {
            esNuevoUsuarioPendiente = false;
            yaAprobado = false;
            mensaje = string.Empty;
            clsUsuario usuario = null;

            using (MySqlConnection con = new MySqlConnection(_conexion))
            {
                con.Open();
                string query = "SELECT Id, Nombre, Correo, TipoUsuario, Aprobado FROM usuario WHERE Correo = @correo";
                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@correo", email);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        usuario = new clsUsuario
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Nombre = reader["Nombre"].ToString(),
                            Correo = reader["Correo"].ToString(),
                            TipoUsuario = reader["TipoUsuario"].ToString(),
                            Aprobado = Convert.ToBoolean(reader["Aprobado"])
                        };
                        reader.Close();

                        if (!usuario.Aprobado)
                        {
                            esNuevoUsuarioPendiente = true;
                            mensaje = "Su cuenta está pendiente de aprobación.";
                            return usuario;
                        }
                        yaAprobado = true;
                        string updateQuery = "UPDATE usuario SET MetodoRegistro = 'Google', Contrasena = NULL WHERE Id = @id AND (MetodoRegistro IS NULL OR MetodoRegistro != 'Google')";
                        MySqlCommand updateCmd = new MySqlCommand(updateQuery, con);
                        updateCmd.Parameters.AddWithValue("@id", usuario.Id);
                        updateCmd.ExecuteNonQuery();
                        mensaje = "Login con Google exitoso.";
                    }
                    else
                    {
                        reader.Close();
                        string insertQuery = "INSERT INTO usuario (Nombre, Correo, MetodoRegistro, TipoUsuario, Aprobado, FechaRegistro, Contrasena) " +
                                             "VALUES (@nombre, @correo, 'Google', 'Invitado', 0, NOW(), NULL)";
                        MySqlCommand insertCmd = new MySqlCommand(insertQuery, con);
                        insertCmd.Parameters.AddWithValue("@nombre", name);
                        insertCmd.Parameters.AddWithValue("@correo", email);
                        insertCmd.ExecuteNonQuery();

                        long newUserId = insertCmd.LastInsertedId;
                        usuario = new clsUsuario { Id = (int)newUserId, Nombre = name, Correo = email, TipoUsuario = "Invitado", Aprobado = false };
                        esNuevoUsuarioPendiente = true;
                        mensaje = "Registro con Google solicitado. Pendiente de aprobación.";
                    }
                }
            }
            return usuario;
        }

        public static List<clsUsuario> ObtenerSolicitudesPendientes()
        {
            List<clsUsuario> solicitudes = new List<clsUsuario>();
            using (MySqlConnection con = new MySqlConnection(_conexion))
            {
                con.Open();
                string query = "SELECT Id, Nombre, Correo, MetodoRegistro, TipoUsuario, FechaRegistro FROM usuario WHERE Aprobado = 0";
                MySqlCommand cmd = new MySqlCommand(query, con);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        solicitudes.Add(new clsUsuario
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Nombre = reader["Nombre"].ToString(),
                            Correo = reader["Correo"].ToString(),
                            MetodoRegistro = reader["MetodoRegistro"].ToString(),
                            TipoUsuario = reader["TipoUsuario"].ToString(),
                            FechaRegistro = Convert.ToDateTime(reader["FechaRegistro"])
                        });
                    }
                }
            }
            return solicitudes;
        }

        public static bool AprobarSolicitud(int id, string rolAsignado)
        {
            using (MySqlConnection con = new MySqlConnection(_conexion))
            {
                con.Open();
                string query = "UPDATE usuario SET Aprobado = 1, TipoUsuario = @rol WHERE Id = @id";
                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@rol", rolAsignado);
                cmd.Parameters.AddWithValue("@id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public static bool ExistePorCorreo(string correo)
        {
            using (MySqlConnection con = new MySqlConnection(_conexion))
            {
                con.Open();
                string query = "SELECT COUNT(1) FROM usuario WHERE Correo = @correo";
                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@correo", correo);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public static bool ActualizarContrasena(string correo, string nuevaContrasena)
        {
            using (MySqlConnection con = new MySqlConnection(_conexion))
            {
                con.Open();
                string query = "UPDATE usuario SET Contrasena = @contrasena, MetodoRegistro = 'Credenciales' WHERE Correo = @correo";
                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@contrasena", nuevaContrasena);
                cmd.Parameters.AddWithValue("@correo", correo);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public static bool GuardarTokenFcmParaUsuario(int usuarioId, string tokenFcm)
        {
            using (MySqlConnection con = new MySqlConnection(_conexion))
            {
                con.Open();
                string query = "UPDATE usuario SET TokenFcm = @token WHERE Id = @id";
                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@token", tokenFcm);
                cmd.Parameters.AddWithValue("@id", usuarioId);
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}