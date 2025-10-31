using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProducScan.Models;

namespace ProducScan.Controllers
{
    [Authorize]
    public class RegistrodeUsersController : Controller
    {
        private readonly AppDbContext _context;

        public RegistrodeUsersController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin,Editor")]
        // 👉 Esta acción solo devuelve la vista Index
        public IActionResult Index()
        {
            return View();
        }


        [Authorize(Roles = "Admin,Editor")]
        // Endpoint para DataTables
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                                      .OrderByDescending(u => u.Id)
                                      .ToListAsync();
            return Json(new { data = users });
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        public async Task<IActionResult> Create(User user)
        {
            if (ModelState.IsValid)
            {
                // Normalizar: asegurar que empiece con "00"
                if (!user.NumerodeEmpleado.StartsWith("00"))
                {
                    user.NumerodeEmpleado = "00" + user.NumerodeEmpleado;
                }

                // Validar que sea numérico
                if (!long.TryParse(user.NumerodeEmpleado, out _))
                {
                    return Json(new { success = false, message = "El número de empleado debe ser numérico" });
                }

                // Validar duplicado
                bool exists = await _context.Users
                    .AnyAsync(u => u.NumerodeEmpleado == user.NumerodeEmpleado);

                if (exists)
                {
                    return Json(new { success = false, message = "El número de empleado ya existe" });
                }

                // 👇 Convertir nombre a mayúsculas
                user.Nombre = user.Nombre?.ToUpperInvariant();

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Usuario creado correctamente" });
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Error en validación", errors });
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        public async Task<IActionResult> Edit(User user)
        {
            if (ModelState.IsValid)
            {
                // Normalizar: asegurar que empiece con "00"
                if (!user.NumerodeEmpleado.StartsWith("00"))
                {
                    user.NumerodeEmpleado = "00" + user.NumerodeEmpleado;
                }

                // Validar que sea numérico
                if (!long.TryParse(user.NumerodeEmpleado, out _))
                {
                    return Json(new { success = false, message = "El número de empleado debe ser numérico" });
                }

                // Validar duplicado (excluyendo el mismo Id)
                bool exists = await _context.Users
                    .AnyAsync(u => u.NumerodeEmpleado == user.NumerodeEmpleado && u.Id != user.Id);

                if (exists)
                {
                    return Json(new { success = false, message = "El número de empleado ya existe" });
                }

                // 👇 Convertir nombre a mayúsculas
                user.Nombre = user.Nombre?.ToUpperInvariant();

                _context.Update(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Usuario actualizado correctamente" });
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Error en validación", errors });
        }
        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Usuario eliminado correctamente" });
            }
            return Json(new { success = false, message = "Usuario no encontrado" });
        }
    }
}
