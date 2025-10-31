using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ProducScan.Models;

public partial class RegistrodeDefecto
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }

    public TimeOnly Hora { get; set; }

    public string? Mandrel { get; set; }

    public string? CodigodeDefecto { get; set; }

    public string? Defecto { get; set; }

    public string? NuMesa { get; set; }

    public string? Turno { get; set; }

    public string? Tm { get; set; }
}


