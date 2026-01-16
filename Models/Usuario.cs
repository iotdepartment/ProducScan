// Models/Usuario.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace ProducScan.Models
{

    [Table("Usuarios")]

    public class Usuario
    {
        public int Id { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Rol { get; set; } = "Visual";
        public DateTime FechaAlta { get; set; } = DateTime.Now;

        // 👇 Nuevo campo
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
    }
}