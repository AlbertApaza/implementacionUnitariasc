using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProyectoConstruccion_APAZA_CUTIPA.Models
{
    public class clsClip
    {
        // Atributos
        public string IdClip { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoDeteccion { get; set; }
        public string EstadoRevision { get; set; }
        public string Anotaciones { get; set; }

        // Métodos
        public void Reproducir()
        {
        }

        public void Pausar()
        {
        }

        public void AgregarNota()
        {
        }

        public void MarcarComoRevisado()
        {
        }

        public string GenerarEnlaceDescarga()
        {
            return "download_link";
        }
    }
}