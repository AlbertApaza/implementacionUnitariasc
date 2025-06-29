using System.Collections.Generic;
using ProyectoConstruccion_APAZA_CUTIPA.Models;

namespace ProyectoConstruccion_APAZA_CUTIPA.Repositories
{
    public interface IUsuarioRepository
    {
        // Métodos existentes
        clsUsuario Autenticar(string correo, string contrasena, out string mensajeError, out bool esAprobado);
        bool VerificarCorreoExistente(string correo, out bool estaAprobado, out string mensaje);
        bool Registrar(clsUsuario usuario, out string mensajeError);
        clsUsuario ObtenerOInsertarUsuarioGoogle(string email, string nombre, out bool necesitaAprobacion, out string mensajeProceso);

        // Métodos clsUsuario 
        clsUsuario ProcesarUsuarioGoogleApi(string email, string name, out bool esNuevoUsuarioPendiente, out bool yaAprobado, out string mensaje);
        List<clsUsuario> ObtenerSolicitudesPendientes();
        bool AprobarSolicitud(int id, string rolAsignado);
        bool ExistePorCorreo(string correo);
        bool ActualizarContrasena(string correo, string nuevaContrasena);
        bool GuardarTokenFcmParaUsuario(int usuarioId, string tokenFcm);

    }
}