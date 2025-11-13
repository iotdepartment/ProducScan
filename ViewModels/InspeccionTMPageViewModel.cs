namespace ProducScan.ViewModels
{
    public class InspeccionTMPageViewModel
    {
        public int Año { get; set; }
        public int Mes { get; set; }
        public int Dia { get; set; }
        public string Turno { get; set; }
        public string FechaSeleccionada { get; set; }
        public List<InspeccionTMViewModel> Inspecciones { get; set; } = new();
    }
}