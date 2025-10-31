namespace ProducScan.ViewModels
{
    public class ProduccionPorTMViewModel
    {
        public string TM { get; set; }
        public int TotalPiezas { get; set; }
        public List<ProduccionPorMandrel> Mandriles { get; set; }
    }
}
