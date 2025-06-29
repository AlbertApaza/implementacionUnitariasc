using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProyectoConstruccion_APAZA_CUTIPA.Models
{
    public class clsAlerta
    {
        // Atributos
        public string IdAlerta { get; set; }
        public string Tipo { get; set; }
        public DateTime HoraDeteccion { get; set; }
        public string ClipAsociado { get; set; }

        // Métodos
        public void ActivarConteo()
        {
        }

        public void DesactivarConteo()
        {
        }

        public void MostrarClip()
        {
        }

        public void ContactarEmergencia()
        {
        }
    }
}