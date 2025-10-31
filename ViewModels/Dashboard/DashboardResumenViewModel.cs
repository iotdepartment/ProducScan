namespace ProducScan.ViewModels.Dashboard
{
    public class DashboardResumenViewModel
    {
        public int TotalPiezas { get; set; }
        public int TotalDefectos { get; set; }

        public int ProduccionAyer { get; set; }
        public double PromedioSemanal { get; set; }

        public List<TopTMViewModel> TopTeamMembers { get; set; }
        public List<TopDefectoViewModel> TopDefectos { get; set; }
        public List<ProduccionPorTurnoViewModel> ProduccionPorTurno { get; set; }
        public DateOnly Fecha { get; set; }
    }
}
