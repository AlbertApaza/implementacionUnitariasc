using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProyectoConstruccion_APAZA_CUTIPA.Models
{
    public class SavedReportConfiguration
    {
        public int IdReporte { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string TipoGrafico { get; set; } // "line", "bar", "pie"

        public string FiltrosJson { get; set; }

        public bool EsPublico { get; set; }
        public int? IdUsuarioCreador { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

    }
}