using System;
using System.Collections.Generic;

namespace ProducScan.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Rol { get; set; } = null!;

    public DateTime FechaAlta { get; set; }

    public string SecurityStamp { get; set; } = null!;
}
