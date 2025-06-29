using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProyectoConstruccion_APAZA_CUTIPA.Models
{
    public class clsContadorPersona
    {
        // Atributos
        public int CantidadPersonas { get; set; } = 0;
        public int CantidadObjetosPeligrosos { get; set; } = 0;

        public DateTime HoraActual { get; set; }

        public void ActivarConteo()
        {
            HoraActual = DateTime.Now;
            CantidadPersonas = 0;
            CantidadObjetosPeligrosos = 0;
        }

        public void DesactivarConteo()
        {
            HoraActual = DateTime.Now;
        }

        public void DetectarPersona()
        {
            CantidadPersonas++;
        }

        public void DetectarObjetoPeligroso()
        {
            CantidadObjetosPeligrosos++;
        }

        public int MostrarContadorPersonas()
        {
            return CantidadPersonas;
        }

        public int MostrarContadorObjetos()
        {
            return CantidadObjetosPeligrosos;
        }
    }
}