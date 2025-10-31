namespace ProducScan.Models
{
    public class HistorialAccion
    {
        public int Id { get; set; }
        public string Usuario { get; set; }
        public string Accion { get; set; }
        public string Entidad { get; set; }
        public int? EntidadId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
