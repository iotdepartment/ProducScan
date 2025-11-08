using System;
using System.Collections.Generic;

namespace ProducScan.Modelos;

public partial class Usuario
{
    public string NombreUsuario { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Rol { get; set; } = null!;

    public DateTime FechaAlta { get; set; }

    public string SecurityStamp { get; set; } = null!;

    public int Id { get; set; }
}
