// Models/Usuario.cs
namespace ProducScan.Models
{
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