namespace ProducScan.ViewModels
{
    public class InspeccionTMViewModel
    {
        public string TM { get; set; }
        public string Mesa { get; set; } // ✅ nueva propiedad
        public string NumeroEmpleado { get; set; }
        public string FotoUrl { get; set; }
        public int PiezasBuenas { get; set; }
        public int PiezasMalas { get; set; }
        public int TotalPiezas { get; set; }
        public int Meta { get; set; }
        public string ColorCard { get; set; }

        // 🔥 nueva propiedad para mostrar el último mandrel escaneado
        public string UltimoMandrel { get; set; }
    }
}
