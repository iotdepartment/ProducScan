namespace ProducScan.ViewModels
{
    public class InspeccionTMViewModel
    {
        public string TM { get; set; }
        public string NumeroEmpleado { get; set; }
        public string FotoUrl { get; set; }
        public int PiezasBuenas { get; set; }
        public int PiezasMalas { get; set; }
        public int TotalPiezas { get; set; }   // NUEVO
        public int Meta { get; set; }
        public string ColorCard { get; set; }
    }
}
