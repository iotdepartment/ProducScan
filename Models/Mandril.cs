namespace ProducScan.Models
{
    public class Mandril
    {
        public int Id { get; set; }
        public string? MandrilNombre { get; set; }
        public string? CentrodeCostos { get; set; }
        public string? CantidaddeEmpaque { get; set; }
        public string? Barcode { get; set; }
        public string? Area { get; set; }
        public string? Kanban { get; set; }
        public string? Estacion { get; set; }
        public double? Costo { get; set; }
        public string? Familia { get; set; }
        public string? Proceso { get; set; }
        public double? PesoMax { get; set; }
        public double? PesoMin { get; set; }

    }
}