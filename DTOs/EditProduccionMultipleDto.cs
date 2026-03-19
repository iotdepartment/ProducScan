namespace ProducScan.DTOs
{
    public class EditProduccionMultipleDto
    {
        public List<int> Ids { get; set; }
        public string? NuMesa { get; set; }
        public string? Turno { get; set; }
        public string? Mandrel { get; set; }
        public string? NDPiezas { get; set; }
    }
}
