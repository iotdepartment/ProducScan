using System;
using System.Collections.Generic;

namespace ProducScan.Modelos;

public partial class RegistrodePiezasEscaneada
{
    public DateOnly Fecha { get; set; }

    public TimeOnly Hora { get; set; }

    public string? Mandrel { get; set; }

    public string? Ndpiezas { get; set; }

    public string? Turno { get; set; }

    public string? NuMesa { get; set; }

    public string? Tm { get; set; }

    public int Id { get; set; }
}
