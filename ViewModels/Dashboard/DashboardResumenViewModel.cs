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

        // Opcional: si quieres mostrar también piezas buenas
        public int TotalPiezasBuenas => TotalPiezas - TotalDefectos;
    }
}
