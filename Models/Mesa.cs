using System;
using System.Collections.Generic;

namespace ProducScan.Models;

public partial class Mesa
{
    // 👈 Llave primaria real
    public int IdMesa { get; set; }

    public string? ID { get; set; }              // sigue existiendo en la tabla, pero ya no es PK
    public string? Mesas { get; set; }
    public string? NumerodeMesa { get; set; }

    // Meta puede ser null
    public int? Meta { get; set; }
}
