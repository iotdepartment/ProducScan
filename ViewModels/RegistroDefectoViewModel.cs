using ProducScan.Models;

namespace ProducScan.ViewModels
{
    public class DetalleDefectosPorUsuarioViewModel
    {
        public string Usuario { get; set; }
        public string Fecha { get; set; }   // Fecha laboral
        public List<RegistroDefectoViewModel> Defectos { get; set; }
    }

    public class RegistroDefectoViewModel
    {
        public int Id { get; set; }
        public DateOnly Fecha { get; set; } // Fecha real
        public string Tm { get; set; }
        public string NuMesa { get; set; }
        public string Turno { get; set; }
        public string Mandrel { get; set; }
        public string CodigodeDefecto { get; set; }
        public string Defecto { get; set; }
        public TimeOnly Hora { get; set; }

        // 👇 Nueva propiedad para distinguir fecha laboral
        public DateTime FechaLaboral { get; set; }
    }
}
