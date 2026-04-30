using System;
using System.Collections.Generic;

namespace ProducScan.Models;

public partial class DefectosScadum
{
    public int Id { get; set; }

    public string? Defecto { get; set; }

    public string? CodigodeDefecto { get; set; }

    public virtual ICollection<SlDefecto> SlDefectos { get; set; } = new List<SlDefecto>();
}
