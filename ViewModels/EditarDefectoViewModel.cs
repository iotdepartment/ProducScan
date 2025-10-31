namespace ProducScan.ViewModels
{
    public class EditarDefectoViewModel
    {
        public int Id { get; set; }            // PK del registro
        public string NuMesa { get; set; }
        public string Turno { get; set; }
        public string Mandrel { get; set; }
        public string CodigodeDefecto { get; set; }
        public string Defecto { get; set; }
        public TimeOnly Hora { get; set; }
        
    }

}
