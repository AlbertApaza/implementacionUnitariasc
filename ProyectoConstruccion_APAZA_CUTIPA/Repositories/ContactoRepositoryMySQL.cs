using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;
using ProyectoConstruccion_APAZA_CUTIPA.Models;

namespace ProyectoConstruccion_APAZA_CUTIPA.Repositories
{
    public class ContactoRepositoryMySQL : IContactoRepository
    {
        private readonly string _connectionString;

        public ContactoRepositoryMySQL()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MySqlConexion"].ConnectionString;
        }

        public List<clsContactoEmergencia> GetByUserId(int idUsuario, out string errorMessage)
        {
            errorMessage = string.Empty;
            List<clsContactoEmergencia> contactos = new List<clsContactoEmergencia>();
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
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
                                IdContactoEmergencia = reader.GetInt32("IdContactoEmergencia"),
                                IdUsuario = reader.GetInt32("IdUsuario"),
                                Nombre = reader.GetString("Nombre"),
                                Apellido = reader.GetString("Apellido"),
                                NumeroTelefono = reader.GetString("NumeroTelefono"),
                                Parentesco = reader.GetString("Parentesco"),
                                EsPrincipal = reader.GetBoolean("EsPrincipal"),
                                FechaCreacion = reader.GetDateTime("FechaCreacion")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ContactoRepositoryMySQL.GetByUserId({idUsuario}): {ex.ToString()}");
                errorMessage = "Error al cargar contactos: " + ex.Message;
                return new List<clsContactoEmergencia>();
            }
            return contactos;
        }

        public bool Add(clsContactoEmergencia contacto, out string errorMessage)
        {
            errorMessage = string.Empty;

            // Basic validation
            if (contacto == null)
            {
                errorMessage = "Datos del contacto no proporcionados.";
                return false;
            }
            if (contacto.IdUsuario <= 0)
            {
                errorMessage = "ID de usuario no válido en los datos del contacto.";
                return false;
            }


            MySqlConnection con = null;
            MySqlTransaction transaction = null; 

            try
            {
                con = new MySqlConnection(_connectionString);
                con.Open();
                transaction = con.BeginTransaction(); 

                if (contacto.EsPrincipal)
                {
                    DesmarcarPrincipalAnteriorInternal(con, transaction, contacto.IdUsuario, 0);
                }

                string query = @"INSERT INTO tbContactoEmergencia 
                               (IdUsuario, Nombre, Apellido, NumeroTelefono, Parentesco, EsPrincipal, FechaCreacion) 
                               VALUES 
                               (@IdUsuario, @Nombre, @Apellido, @NumeroTelefono, @Parentesco, @EsPrincipal, NOW())"; 

                MySqlCommand cmd = new MySqlCommand(query, con, transaction); 

                cmd.Parameters.AddWithValue("@IdUsuario", contacto.IdUsuario);
                cmd.Parameters.AddWithValue("@Nombre", contacto.Nombre ?? string.Empty);
                cmd.Parameters.AddWithValue("@Apellido", contacto.Apellido ?? string.Empty);
                cmd.Parameters.AddWithValue("@NumeroTelefono", contacto.NumeroTelefono ?? string.Empty);
                cmd.Parameters.AddWithValue("@Parentesco", contacto.Parentesco ?? string.Empty);
                cmd.Parameters.AddWithValue("@EsPrincipal", contacto.EsPrincipal);

                cmd.ExecuteNonQuery();


                transaction.Commit();
                return true;
            }
            catch (MySqlException ex)
            {
                transaction?.Rollback();
                System.Diagnostics.Debug.WriteLine("Error in ContactoRepositoryMySQL.Add (MySQL): " + ex.ToString());
                errorMessage = "Error al guardar el contacto: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                System.Diagnostics.Debug.WriteLine("Error in ContactoRepositoryMySQL.Add (General): " + ex.ToString());
                errorMessage = "Error general al guardar el contacto: " + ex.Message;
                return false;
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
        }

        public clsContactoEmergencia GetById(int idContactoEmergencia, int idUsuario, out string errorMessage)
        {
            errorMessage = string.Empty;
            clsContactoEmergencia contacto = null;
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"SELECT IdContactoEmergencia, IdUsuario, Nombre, Apellido, NumeroTelefono, Parentesco, EsPrincipal, FechaCreacion
                                   FROM tbContactoEmergencia
                                   WHERE IdContactoEmergencia = @IdContactoEmergencia AND IdUsuario = @IdUsuario"; // Crucially filter by user ID

                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@IdContactoEmergencia", idContactoEmergencia);
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            contacto = new clsContactoEmergencia
                            {
                                IdContactoEmergencia = reader.GetInt32("IdContactoEmergencia"),
                                IdUsuario = reader.GetInt32("IdUsuario"),
                                Nombre = reader.GetString("Nombre"),
                                Apellido = reader.GetString("Apellido"),
                                NumeroTelefono = reader.GetString("NumeroTelefono"),
                                Parentesco = reader.GetString("Parentesco"),
                                EsPrincipal = reader.GetBoolean("EsPrincipal"),
                                FechaCreacion = reader.GetDateTime("FechaCreacion")
                            };
                        }
                        else
                        {
                            errorMessage = "Contacto no encontrado para este usuario.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ContactoRepositoryMySQL.GetById({idContactoEmergencia}, {idUsuario}): {ex.ToString()}");
                errorMessage = "Error al obtener el contacto: " + ex.Message;
            }
            return contacto;
        }

        public bool Update(clsContactoEmergencia contacto, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (contacto == null || contacto.IdContactoEmergencia <= 0 || contacto.IdUsuario <= 0)
            {
                errorMessage = "Datos de contacto para actualizar inválidos.";
                return false;
            }

            MySqlConnection con = null;
            MySqlTransaction transaction = null; 

            try
            {
                con = new MySqlConnection(_connectionString);
                con.Open();
                transaction = con.BeginTransaction();

                if (contacto.EsPrincipal)
                {
                    DesmarcarPrincipalAnteriorInternal(con, transaction, contacto.IdUsuario, contacto.IdContactoEmergencia);
                }

                string query = @"UPDATE tbContactoEmergencia SET
                               Nombre = @Nombre,
                               Apellido = @Apellido,
                               NumeroTelefono = @NumeroTelefono,
                               Parentesco = @Parentesco,
                               EsPrincipal = @EsPrincipal
                               WHERE IdContactoEmergencia = @IdContactoEmergencia AND IdUsuario = @IdUsuario"; // Crucially filter by user ID

                MySqlCommand cmd = new MySqlCommand(query, con, transaction); 

                cmd.Parameters.AddWithValue("@Nombre", contacto.Nombre ?? string.Empty);
                cmd.Parameters.AddWithValue("@Apellido", contacto.Apellido ?? string.Empty);
                cmd.Parameters.AddWithValue("@NumeroTelefono", contacto.NumeroTelefono ?? string.Empty);
                cmd.Parameters.AddWithValue("@Parentesco", contacto.Parentesco ?? string.Empty);
                cmd.Parameters.AddWithValue("@EsPrincipal", contacto.EsPrincipal);
                cmd.Parameters.AddWithValue("@IdContactoEmergencia", contacto.IdContactoEmergencia);
                cmd.Parameters.AddWithValue("@IdUsuario", contacto.IdUsuario);

                int rowsAffected = cmd.ExecuteNonQuery();

                transaction.Commit();

                return rowsAffected > 0;
            }
            catch (MySqlException ex)
            {
                transaction?.Rollback(); 
                System.Diagnostics.Debug.WriteLine($"Error in ContactoRepositoryMySQL.Update({contacto?.IdContactoEmergencia}, {contacto?.IdUsuario}) (MySQL): {ex.ToString()}");
                errorMessage = "Error al actualizar el contacto: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                System.Diagnostics.Debug.WriteLine($"Error in ContactoRepositoryMySQL.Update({contacto?.IdContactoEmergencia}, {contacto?.IdUsuario}) (General): {ex.ToString()}");
                errorMessage = "Error general al actualizar el contacto: " + ex.Message;
                return false;
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
        }

        public bool SetAsPrincipal(int idContactoEmergencia, int idUsuario, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (idContactoEmergencia <= 0 || idUsuario <= 0)
            {
                errorMessage = "IDs proporcionados no son válidos.";
                return false;
            }

            MySqlConnection con = null;
            MySqlTransaction transaction = null;

            try
            {
                con = new MySqlConnection(_connectionString);
                con.Open();
                transaction = con.BeginTransaction();

                // 1. Desmarcar el anterior principal para este usuario, excluyendo el que vamos a marcar
                DesmarcarPrincipalAnteriorInternal(con, transaction, idUsuario, idContactoEmergencia);

                // 2. Marcar el contacto especificado como principal
                string queryMarcar = @"UPDATE tbContactoEmergencia
                                     SET EsPrincipal = 1
                                     WHERE IdContactoEmergencia = @IdContactoEmergencia AND IdUsuario = @IdUsuario"; 

                MySqlCommand cmdMarcar = new MySqlCommand(queryMarcar, con, transaction); 
                cmdMarcar.Parameters.AddWithValue("@IdContactoEmergencia", idContactoEmergencia);
                cmdMarcar.Parameters.AddWithValue("@IdUsuario", idUsuario);

                int rowsAffected = cmdMarcar.ExecuteNonQuery();

                transaction.Commit();

                return rowsAffected > 0;

            }
            catch (MySqlException ex)
            {
                transaction?.Rollback();
                System.Diagnostics.Debug.WriteLine($"Error in ContactoRepositoryMySQL.SetAsPrincipal({idContactoEmergencia}, {idUsuario}) (MySQL): {ex.ToString()}");
                errorMessage = "Error al marcar como principal: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                System.Diagnostics.Debug.WriteLine($"Error in ContactoRepositoryMySQL.SetAsPrincipal({idContactoEmergencia}, {idUsuario}) (General): {ex.ToString()}");
                errorMessage = "Error general al marcar como principal: " + ex.Message;
                return false;
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
        }


        public bool Delete(int idContactoEmergencia, int idUsuario, out string mensajeError)
        {
            mensajeError = string.Empty;

            if (idContactoEmergencia <= 0 || idUsuario <= 0)
            {
                mensajeError = "IDs proporcionados no son válidos.";
                return false;
            }

            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    con.Open();
                    string query = "DELETE FROM tbContactoEmergencia WHERE IdContactoEmergencia = @IdContactoEmergencia AND IdUsuario = @IdUsuario"; // Crucially filter by user ID

                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@IdContactoEmergencia", idContactoEmergencia);
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    return rowsAffected > 0;
                }
            }
            catch (MySqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ContactoRepositoryMySQL.Delete({idContactoEmergencia}, {idUsuario}) (MySQL): {ex.ToString()}");
                mensajeError = "Error al eliminar: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ContactoRepositoryMySQL.Delete({idContactoEmergencia}, {idUsuario}) (General): {ex.ToString()}");
                mensajeError = "Error general al eliminar: " + ex.Message;
                return false;
            }
        }


        private void DesmarcarPrincipalAnteriorInternal(MySqlConnection con, MySqlTransaction transaction, int idUsuario, int idContactoAExcluirSiSeEstaMarcando)
        {
            if (con == null || con.State != ConnectionState.Open)
            {
                System.Diagnostics.Debug.WriteLine("Error: DesmarcarPrincipalAnteriorInternal called with invalid connection.");
                return;
            }


            string query = @"UPDATE tbContactoEmergencia
                           SET EsPrincipal = 0
                           WHERE IdUsuario = @IdUsuario AND EsPrincipal = 1"; 

            if (idContactoAExcluirSiSeEstaMarcando > 0)
            {
                query += " AND IdContactoEmergencia != @IdContactoAExcluirSiSeEstaMarcando";
            }

            MySqlCommand cmd = new MySqlCommand(query, con, transaction);

            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
            if (idContactoAExcluirSiSeEstaMarcando > 0)
            {
                cmd.Parameters.AddWithValue("@IdContactoAExcluirSiSeEstaMarcando", idContactoAExcluirSiSeEstaMarcando);
            }

            cmd.ExecuteNonQuery();
        }
    }
}