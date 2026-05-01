using System;
using System.Collections.Generic;

namespace ProducScan.Models;

public partial class Mandrile
{
    public string? Mandril { get; set; }

    public string? CentrodeCostos { get; set; }

    public string? CantidaddeEmpaque { get; set; }

    public string? Barcode { get; set; }

    public string? Area { get; set; }

    public int Id { get; set; }

    public string? Kanban { get; set; }

    public string? Estacion { get; set; }

    public double? Costo { get; set; }

    public double? PesoMax { get; set; }

    public double? PesoMin { get; set; }

    public string? Familia { get; set; }

    public string? Proceso { get; set; }

    public string? Imagen { get; set; }

    public bool? Ltester { get; set; }
}
