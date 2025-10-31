namespace ProducScan.Models
{
    public class Log
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Usuario { get; set; } = "Sistema";
        public string Accion { get; set; } = string.Empty;
        public string Detalles { get; set; } = string.Empty;
        public string Nivel { get; set; } = "Info"; // Info, Warning, Error, Critical
        public string Ip { get; set; } = string.Empty;
        public string Categoria { get; set; } = "Sistema"; // 👈 Nuevo: Sistema o Producción
    }
}