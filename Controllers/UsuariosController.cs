using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProducScan.Models;
using ProducScan.Models.ViewModels;
using ProducScan.Services;

namespace ProducScan.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsuariosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogService _log;

        public UsuariosController(AppDbContext context, ILogService log)
        {
            _context = context;
            _log = log;
        }

        public IActionResult Index()
        {
            var usuarios = _context.Usuarios.ToList();
            return View(usuarios); // 👈 ahora sí coincide con @model IEnumerable<Usuario>
        }

        // GET: Modal Crear
        public IActionResult Crear()
        {
            return PartialView("_CrearUsuario");
        }

        public IActionResult TablaUsuarios()
        {
            var usuarios = _context.Usuarios.ToList();
            return PartialView("_TablaUsuarios", usuarios);
        }

        [HttpPost]
        public IActionResult Crear(Usuario model, string password)
        {
            if (_context.Usuarios.Any(u => u.NombreUsuario == model.NombreUsuario))
            {
                _log.Registrar("Error crear usuario", $"Intento duplicado: {model.NombreUsuario}", "Error", categoria: "Sistema");
                return Json(new { success = false, message = "El nombre de usuario ya existe." });
            }

            _context.Usuarios.Add(model);
            _context.SaveChanges();

            _log.Registrar("Crear usuario", $"Usuario creado: {model.NombreUsuario}");
            return Json(new { success = true, message = "Usuario creado correctamente." });
        }

        // GET: Modal Editar
        public IActionResult Editar(int id)
        {
            var usuario = _context.Usuarios.Find(id);
            if (usuario == null) return NotFound();
            return PartialView("_EditarUsuario", usuario);
        }

        [HttpPost]
        public IActionResult Editar(int id, string nombreUsuario, string rol, string? password)
        {
            var usuario = _context.Usuarios.Find(id);
            if (usuario == null) return Json(new { success = false, message = "Usuario no encontrado." });

            if (_context.Usuarios.Any(u => u.NombreUsuario == nombreUsuario && u.Id != id))
            {
                _log.Registrar("Error editar usuario", $"Intento duplicado: {usuario.NombreUsuario}", "Error", categoria: "Sistema");
                return Json(new { success = false, message = "El nombre de usuario ya existe." });
            }

            usuario.NombreUsuario = nombreUsuario;
            usuario.Rol = rol;
            if (!string.IsNullOrWhiteSpace(password))
                usuario.PasswordHash = HashPassword(password);

            _context.Update(usuario);

            _context.SaveChanges();
            _log.Registrar("Usuario Actualizado", $"Usuario: {usuario.NombreUsuario} ha sido actualizado", "Info", categoria: "Sistema");
            return Json(new { success = true, message = "Usuario actualizado correctamente." });
        }

        // GET: Modal Eliminar
        public IActionResult Eliminar(int id)
        {
            var usuario = _context.Usuarios.Find(id);
            if (usuario == null) return NotFound();
            return PartialView("_EliminarUsuario", usuario);
        }

        [HttpPost]
        public IActionResult EliminarConfirmado(int id)
        {
            var usuario = _context.Usuarios.Find(id);
            if (usuario == null)
                return Json(new { success = false, message = "Usuario no encontrado." });

            var usuarioActual = User.Identity?.Name;
            if (usuario.NombreUsuario == usuarioActual)
            {
                return Json(new { success = false, message = "No puedes eliminar tu propio usuario mientras estás conectado." });
            }

            // 👇 Capturamos datos antes de eliminar
            var nombreEliminado = usuario.NombreUsuario;

            // Cambiamos SecurityStamp para invalidar sesiones activas
            usuario.SecurityStamp = Guid.NewGuid().ToString();

            // Registrar en logs con más detalle
            _log.Registrar(
                "Usuario Eliminado",
                $"Usuario '{nombreEliminado}' fue eliminado del sistema",
                "Warning"
                , categoria: "Sistema"
            );

            _context.Usuarios.Remove(usuario);
            _context.SaveChanges();

            return Json(new { success = true, message = "Usuario eliminado correctamente." });
        }

        private static string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}