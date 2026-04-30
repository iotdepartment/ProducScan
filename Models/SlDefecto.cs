using System;
using System.Collections.Generic;

namespace ProducScan.Models;

public partial class SlDefecto
{
    public int Id { get; set; }

    public int KanbanId { get; set; }

    public int DefectoId { get; set; }

    public int UserId { get; set; }

    public DateTime FechaHora { get; set; }

    public int Qty { get; set; }

    public virtual DefectosScadum Defecto { get; set; } = null!;

    public virtual Kanban Kanban { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
