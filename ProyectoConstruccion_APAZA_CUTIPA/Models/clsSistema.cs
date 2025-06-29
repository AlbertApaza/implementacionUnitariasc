using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProyectoConstruccion_APAZA_CUTIPA.Models
{
    public class clsSistema
    {
        public string EstadoActual { get; set; }
        public string ConfiguracionesGuardadas { get; set; }

        // Métodos
        public bool AutenticarUsuario()
        {
            return false;
        }

        public void AplicarConfiguraciones()
        {
        }

        public void EnviarNotificacion()
        {
        }

        public void GenerarDashboard()
        {
        }
    }
}