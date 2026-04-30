using System;
using System.Collections.Generic;

namespace ProducScan.Models;

public partial class RegistrodePiezasEscaneadasExt
{
    public DateOnly Fecha { get; set; }

    public TimeOnly Hora { get; set; }

    public string Mandrel { get; set; } = null!;

    public int? Ndpiezas { get; set; }

    public string Turno { get; set; } = null!;

    public string NuMesa { get; set; } = null!;

    public string Tm { get; set; } = null!;

    public int Id { get; set; }
}
