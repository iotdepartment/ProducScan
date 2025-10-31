namespace ProducScan.ViewModels
{

    public class ProduccionPorMesaViewModel
    {
        public List<string> TeamMembers { get; set; } = new();
        public string NuMesa { get; set; }
        public string Turno { get; set; }
        public int TotalPiezas { get; set; }
        public List<ProduccionPorMandrel> Mandriles { get; set; } = new();


        public List<ProduccionPorTMViewModel> TeamMembersProduccion { get; set; }


        public int TotalDefectos { get; set; }
        public List<(string Defecto, int Total)> DefectosPorDefecto { get; set; }
        public List<DefectoPorTMViewModel> DefectosPorTM { get; set; }
        public List<DefectoPorMandrelViewModel> DefectosPorMandrel { get; set; }

    }
}
