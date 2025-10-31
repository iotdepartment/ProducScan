using Microsoft.EntityFrameworkCore;

namespace ProducScan.Models
{

    [Keyless]
    public class Registro
    {
        public DateOnly Fecha { get; set; }

        public TimeOnly Hora { get; set; }

        public string? Mandrel { get; set; }

        public string? Ndpiezas { get; set; }

        public string? Turno { get; set; }

        public string? NuMesa { get; set; }

        public string? Tm { get; set; }
    }
}
