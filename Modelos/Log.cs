using System;
using System.Collections.Generic;

namespace ProducScan.Modelos;

public partial class Log
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; }

    public string Usuario { get; set; } = null!;

    public string Accion { get; set; } = null!;

    public string Detalles { get; set; } = null!;

    public string Nivel { get; set; } = null!;

    public string Ip { get; set; } = null!;

    public string Categoria { get; set; } = null!;
}
