using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ProducScan.ViewModels
{
    public class ProduccionDetalleViewModel
    {
        public int Id { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeOnly Hora { get; set; }
        public string? Mandrel { get; set; }
        public string? NumeroDePiezas { get; set; }
        public string? TM { get; set; }
        public string NuMesa { get; set; }
        public string? Turno { get; set; }

        public DateTime FechaLaboral { get; set; }
        public DateOnly FechaReal { get; set; }

        public double SegundosPorPieza { get; set; }
    }
}