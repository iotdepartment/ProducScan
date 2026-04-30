using System;
using System.Collections.Generic;

namespace ProducScan.Models;

public partial class TiemposMuerto
{
    public int Id { get; set; }

    public string NombreTiempoMuerto { get; set; } = null!;

    public string CodigoTiempoMuerto { get; set; } = null!;
}
