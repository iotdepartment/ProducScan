using System;
using System.Collections.Generic;

namespace ProducScan.Models;

public partial class Kanban
{
    public int Id { get; set; }

    public string? Modelo { get; set; }

    public virtual ICollection<SlDefecto> SlDefectos { get; set; } = new List<SlDefecto>();
}
