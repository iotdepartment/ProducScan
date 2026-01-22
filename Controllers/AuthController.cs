// Controllers/AuthController.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProducScan.Models;
using ProducScan.Services;
using System.Security.Claims;



namespace ProducScan.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogService _log;


        public AuthController(AppDbContext context, ILogService log)
        {
            _context = context;
            _log = log;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string usuario, string password)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Ingresa usuario y contraseña.";
                return View();
            }

            var user = _context.Usuarios.FirstOrDefault(u => u.NombreUsuario == usuario);
            if (user == null)
            {
                _log.Registrar("Login", $"Usuario {usuario} inició sesión", categoria: "Sistema");
                TempData["Error"] = "Usuario o contraseña incorrectos.";
                return View();
            }

            // Validar contraseña
            var inputHash = HashPassword(password);
            if (!string.Equals(user.PasswordHash, inputHash, StringComparison.Ordinal))
            {
                _log.Registrar("Login fallido", $"Intento fallido de {usuario}", "Warning", categoria: "Sistema");
                TempData["Error"] = "Usuario o contraseña incorrectos.";
                return View();
            }

            // Claims (Name y Role)
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.NombreUsuario),
        new Claim(ClaimTypes.Role, user.Rol),
        new Claim("SecurityStamp", user.SecurityStamp)
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // 🔥 Recuperar la última vista visitada
            var ultimaVista = HttpContext.Session.GetString("UltimaVista");

            if (!string.IsNullOrWhiteSpace(ultimaVista))
                return Redirect(ultimaVista);

            // Si no hay última vista, ir al dashboard por defecto
            return RedirectToAction("Dashboard", "PiezasEscaneadas");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }


        [AllowAnonymous]
        public IActionResult Denegado()
        {
            return View();
        }

        // Hash local (igual a Program.cs)
        private static string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}