using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProyectoConstruccion_APAZA_CUTIPA.Models
{
    public class clsCamara
    {

            // Atributos
            public string IdCamara { get; set; }
            public string Ubicacion { get; set; }
            public string Estado { get; set; }

            // Métodos
            public void IniciarTransmision()
            {
            Estado = "En transmisión";
            Console.WriteLine($"Cámara {IdCamara} iniciada en {DateTime.Now}");
        }

            public void DetenerTransmision()
            {
            Estado = "Detenida";
            Console.WriteLine($"Cámara {IdCamara} detenida en {DateTime.Now}");
            }

        public string ObtenerEstado()
            {
                return Estado; 
            }
    }
}