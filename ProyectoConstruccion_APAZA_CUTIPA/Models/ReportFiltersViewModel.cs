using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProyectoConstruccion_APAZA_CUTIPA.Models
{
    public class ReportFiltersViewModel
    {
        public string ReportTitle { get; set; } // Título para la previsualización del gráfico
        public string StartDate { get; set; } // Formato "DD/MM/YYYY"
        public string EndDate { get; set; }   // Formato "DD/MM/YYYY"
        public bool CheckPersonas { get; set; }
        public bool CheckArmasBlancas { get; set; }
        public bool CheckArmasFuego { get; set; }
        public string SeverityFilter { get; set; } // "all", "Critical", "High", etc.
        public string GroupByFilter { get; set; }  // "day", "week", "month", "type", "severity"
    }
}