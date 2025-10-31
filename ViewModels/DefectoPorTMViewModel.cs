namespace ProducScan.ViewModels
{
    public class DefectoPorTMViewModel
    {
        public string TM { get; set; }
        public string NuMesa { get; set; }
        public string Turno { get; set; }
        public DateOnly Fecha { get; set; }
        public int TotalDefectos { get; set; }
        public List<DefectoDetalleViewModel> Defectos { get; set; } = new();
    }


}
