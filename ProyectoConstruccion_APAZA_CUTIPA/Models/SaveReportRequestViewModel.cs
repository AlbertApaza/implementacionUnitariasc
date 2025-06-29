using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProyectoConstruccion_APAZA_CUTIPA.Models
{
    public class SaveReportRequestViewModel
    {
        public int IdReporte { get; set; } // 0 para nuevo, >0 para actualizar metadata
        public string NombreModal { get; set; }
        public string DescripcionModal { get; set; }
        public bool EsPublicoModal { get; set; }

        // Estos vienen del estado actual del panel, no del modal directamente,
        // pero son parte de lo que se guarda.
        public string TipoGraficoPanel { get; set; }
        public ReportFiltersViewModel FiltrosPanel { get; set; }
    }
}