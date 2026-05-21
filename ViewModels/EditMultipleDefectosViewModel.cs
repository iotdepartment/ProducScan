namespace ProducScan.ViewModels
{
    public class EditMultipleDefectosViewModel
    {
        public List<int> Ids { get; set; }

        public string NuMesa { get; set; }      // "MESA#2"
        public string Turno { get; set; }
        public string Mandrel { get; set; }

        public string CodigodeDefecto { get; set; }
        public string Defecto { get; set; }
    }
}
