using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProyectoConstruccion_APAZA_CUTIPA.Models
{
    public class ChartDataItem
    {
        public string Label { get; set; } 
        public int Value { get; set; }
    }

    public class DeteccionEventoRaw
    {
        public int IdEvento { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan Hora { get; set; }
        public string Tipo { get; set; }
        public string Subtipo { get; set; }
        public string Severidad { get; set; }
        public string Ubicacion { get; set; }
        public decimal? Confianza { get; set; }
        public string Estado { get; set; }
        public string IDdrive { get; set; }
    }
}