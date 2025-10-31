using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ProducScan.Models
{
    public class RegistrodePiezasEscaneada
    {
        public int Id { get; set; }
        public DateOnly Fecha { get; set; }

        public TimeOnly Hora { get; set; }

        public string? Mandrel { get; set; }

        public string? Ndpiezas { get; set; }

        public string? Turno { get; set; }

        public string? NuMesa { get; set; }

        public string? Tm { get; set; }
    }
}