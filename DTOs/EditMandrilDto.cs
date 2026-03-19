namespace ProducScan.DTOs
{
    public class EditMandrilDto
    {
        public List<int> Ids { get; set; } = new List<int>();
        public string? MandrilNombre { get; set; }
        public string? CentrodeCostos { get; set; }
        public string? CantidaddeEmpaque { get; set; }
        public string? Barcode { get; set; }
        public string? Kanban { get; set; }
        public string? Estacion { get; set; }
        public string? Familia { get; set; }
        public string? Proceso { get; set; }
        public double? PesoMax { get; set; }
        public double? PesoMin { get; set; }
    }
}