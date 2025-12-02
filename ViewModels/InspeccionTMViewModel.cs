namespace ProducScan.ViewModels
{
    public class InspeccionTMViewModel
    {
        public string TM { get; set; }
        public string Mesa { get; set; }
        public string NumeroEmpleado { get; set; }
        public string FotoUrl { get; set; }
        public int PiezasBuenas { get; set; }
        public int PiezasMalas { get; set; }
        public int TotalPiezas { get; set; }
        public int Meta { get; set; }              // meta total de la mesa
        public int MetaEsperada { get; set; }      // meta proporcional al tiempo transcurrido
        public string Estado { get; set; }         // texto (En meta, Sobreproducción, etc.)
        public string ColorClass { get; set; }     // clase CSS para el card
                                                   // 🔥 nueva propiedad para mostrar el último mandrel escaneado
        public string Mandril { get; set; }

    }
}
