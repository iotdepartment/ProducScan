namespace ProducScan.ViewModels.Dashboard
{
    public class DashboardResumenViewModel
    {
        public int ProduccionAyer { get; set; }
        public double PromedioSemanal { get; set; }

        public List<TopTMViewModel> TopTeamMembers { get; set; }
        public List<TopDefectoViewModel> TopDefectos { get; set; }
        public List<ProduccionPorTurnoViewModel> ProduccionPorTurno { get; set; }
        public DateOnly Fecha { get; set; }

        // Totales
        public int TotalPiezas { get; set; }
        public int TotalDefectos { get; set; }

        // Indicadores de calidad
        public double FPY { get; set; }
        public double Scrap { get; set; }

        // Defectos categorizados
        public double PorcentajePrintIllegible { get; set; }
        public double PorcentajeMaterialLub { get; set; }
        public double PorcentajeVulcanization { get; set; }
        public double PorcentajeUncured { get; set; }

        // Piezas buenas
        public int TotalPiezasBuenas => TotalPiezas - TotalDefectos;

        /* ============================================================
           PROPIEDADES FORMATEADAS (LAS QUE FALTABAN)
        ============================================================ */

        public string TotalPiezasFmt { get; set; } = string.Empty;
        public string TotalBuenasFmt { get; set; } = string.Empty;
        public string TotalDefectosFmt { get; set; } = string.Empty;

        public string FPYFmt { get; set; } = string.Empty;
        public string ScrapFmt { get; set; } = string.Empty;

        public string PrintIllegibleFmt { get; set; } = string.Empty;
        public string MaterialLubFmt { get; set; } = string.Empty;
        public string VulcanizationFmt { get; set; } = string.Empty;
        public string UncuredFmt { get; set; } = string.Empty;
    }
}