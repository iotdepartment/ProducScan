using System;
using System.Collections.Generic;

namespace ProducScan.Modelos;

public partial class DownTime
{
    public DateOnly? Fecha { get; set; }

    public TimeOnly? Hora { get; set; }

    public string? Tm { get; set; }

    public string? Dt { get; set; }

    public string? Mesa { get; set; }

    public int Id { get; set; }
}
