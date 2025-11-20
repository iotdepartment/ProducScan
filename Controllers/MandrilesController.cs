using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProducScan.Models;

namespace ProducScan.Controllers
{
    public class MandrilesController : Controller
    {
        private readonly AppDbContext _context;

        public MandrilesController(AppDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "Admin,Editor")]
        public IActionResult Index()
        {
            //var mandriles = _context.Mandriles.ToList();
            return View();
        }
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> GetMandriles()
        {
            var mandriles = await _context.Mandriles
                .Where(m => m.Area == "INSPECCION")
                .OrderByDescending(m => m.Id)
                .Select(m => new
                {
                    id = m.Id,
                    mandrilNombre = m.MandrilNombre,
                    centrodeCostos = m.CentrodeCostos,
                    cantidaddeEmpaque = m.CantidaddeEmpaque,
                    barcode = m.Barcode,
                    area = m.Area,
                    kanban = m.Kanban,
                    estacion = m.Estacion
                })
                .ToListAsync();

            return Json(new { data = mandriles });
        }

        [Authorize(Roles = "Admin,Editor")]
        // POST: Mandriles/Create
        [HttpPost]
        public async Task<IActionResult> Create(Mandril mandril)
        {
            ModelState.Remove(nameof(Mandril.Id)); // evita error de Id vacío en Create

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Json(new { success = false, message = "Error de validación", errors });
            }

            try
            {
                mandril.Area = "INSPECCION"; // fuerza el área
                                             // 👇 Estación ya viene del formulario, puede ser null
                                             // mandril.Estacion = mandril.Estacion; // no es necesario asignar explícitamente

                _context.Add(mandril);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Mandril creado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al crear: {ex.Message}" });
            }
        }
        [Authorize(Roles = "Admin,Editor")]
        // POST: Mandriles/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(Mandril mandril)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage)
                                              .ToList();
                return Json(new { success = false, message = "Error de validación", errors });
            }

            try
            {
                var existing = await _context.Mandriles.FindAsync(mandril.Id);
                if (existing == null)
                    return Json(new { success = false, message = "Mandril no encontrado." });

                existing.MandrilNombre = mandril.MandrilNombre;
                existing.CentrodeCostos = mandril.CentrodeCostos;
                existing.CantidaddeEmpaque = mandril.CantidaddeEmpaque;
                existing.Barcode = mandril.Barcode;
                existing.Kanban = mandril.Kanban;
                existing.Estacion = mandril.Estacion; // 👈 ahora también se actualiza

                existing.Area = "INSPECCION"; // siempre INSPECCION

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Mandril actualizado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al editar: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Admin,Editor")]
        // POST: Mandriles/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var mandril = await _context.Mandriles.FindAsync(id);
                if (mandril == null)
                    return Json(new { success = false, message = "Mandril no encontrado." });

                _context.Mandriles.Remove(mandril);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Mandril eliminado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al eliminar: {ex.Message}" });
            }
        }



    }
}
