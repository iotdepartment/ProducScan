using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProducScan.DTOs;
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
                    kanban = m.Kanban,
                    estacion = m.Estacion,
                    familia = m.Familia,
                    proceso = m.Proceso,
                    Imagen = m.Imagen,
                    LTester = m.LTester,
                    pesomin = m.PesoMin,
                    pesomax = m.PesoMax
                })
                .ToListAsync();

            return Json(new { data = mandriles });
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        public async Task<IActionResult> Create(Mandril mandril, IFormFile Imagen)
        {
            ModelState.Remove(nameof(Mandril.Id));

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
                mandril.Area = "INSPECCION";

                // ============================
                //   SUBIDA DE IMAGEN (CREATE)
                // ============================
                if (Imagen != null && Imagen.Length > 0)
                {
                    var extension = Path.GetExtension(Imagen.FileName);
                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var savePath = Path.Combine("wwwroot/images/mandriles", fileName);

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await Imagen.CopyToAsync(stream);
                    }

                    mandril.Imagen = fileName;
                }

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
        [HttpPost]
        public async Task<IActionResult> Edit(Mandril mandril, IFormFile Imagen, bool RemoveImage)
        {
            // ============================
            // VALIDACIONES BACKEND
            // ============================

            if (string.IsNullOrWhiteSpace(mandril.MandrilNombre))
                return Json(new { success = false, message = "El campo Mandril es obligatorio." });

            if (!string.IsNullOrWhiteSpace(mandril.CentrodeCostos) &&
                !int.TryParse(mandril.CentrodeCostos, out _))
                return Json(new { success = false, message = "Centro de Costos debe ser numérico." });

            if (!string.IsNullOrWhiteSpace(mandril.CantidaddeEmpaque) &&
                !int.TryParse(mandril.CantidaddeEmpaque, out _))
                return Json(new { success = false, message = "Cantidad de Empaque debe ser numérica." });

            try
            {
                var existing = await _context.Mandriles.FindAsync(mandril.Id);
                if (existing == null)
                    return Json(new { success = false, message = "Mandril no encontrado." });

                // ============================
                // ACTUALIZAR CAMPOS NORMALES
                // ============================
                existing.MandrilNombre = mandril.MandrilNombre;
                existing.CentrodeCostos = mandril.CentrodeCostos;
                existing.CantidaddeEmpaque = mandril.CantidaddeEmpaque;
                existing.Barcode = mandril.Barcode;
                existing.Kanban = mandril.Kanban;
                existing.Estacion = mandril.Estacion;
                existing.Familia = mandril.Familia;
                existing.Proceso = mandril.Proceso;
                existing.PesoMin = mandril.PesoMin;
                existing.PesoMax = mandril.PesoMax;
                existing.LTester = mandril.LTester;
                existing.Area = "INSPECCION";

                // ============================
                // ELIMINAR IMAGEN (X)
                // ============================
                if (RemoveImage)
                {
                    if (!string.IsNullOrEmpty(existing.Imagen))
                    {
                        var oldPath = Path.Combine("wwwroot/images/mandriles", existing.Imagen);
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    existing.Imagen = null;
                }

                // ============================
                // SUBIR NUEVA IMAGEN
                // ============================
                if (Imagen != null && Imagen.Length > 0)
                {
                    // Borrar imagen anterior si existe
                    if (!string.IsNullOrEmpty(existing.Imagen))
                    {
                        var oldPath = Path.Combine("wwwroot/images/mandriles", existing.Imagen);
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    var extension = Path.GetExtension(Imagen.FileName);
                    var fileName = $"{existing.Id}_{DateTime.Now.Ticks}{extension}";
                    var savePath = Path.Combine("wwwroot/images/mandriles", fileName);

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await Imagen.CopyToAsync(stream);
                    }

                    existing.Imagen = fileName;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Mandril actualizado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al editar: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Admin,Editor")]
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

        [HttpPost]
        public IActionResult DeleteMultiple([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return BadRequest("No se recibieron IDs para eliminar.");
            }

            try
            {
                var registros = _context.Mandriles
                    .Where(m => ids.Contains(m.Id))
                    .ToList();

                if (!registros.Any())
                {
                    return NotFound("No se encontraron registros con los IDs proporcionados.");
                }

                _context.Mandriles.RemoveRange(registros);
                _context.SaveChanges();

                return Ok(new { message = $"{registros.Count} registros eliminados correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al eliminar registros: {ex.Message}");
            }
        }
        [HttpPost]
        public IActionResult EditMultiple([FromBody] EditMandrilDto dto)
        {
            if (dto == null || dto.Ids == null || !dto.Ids.Any())
                return BadRequest("No se recibieron IDs para editar.");

            try
            {
                var registros = _context.Mandriles
                    .Where(m => dto.Ids.Contains(m.Id))
                    .ToList();

                if (!registros.Any())
                    return NotFound("No se encontraron registros con los IDs proporcionados.");

                foreach (var registro in registros)
                {
                    // Inputs tipo texto
                    if (dto.MandrilNombre != null) registro.MandrilNombre = dto.MandrilNombre;
                    if (dto.CentrodeCostos != null) registro.CentrodeCostos = dto.CentrodeCostos;
                    if (dto.CantidaddeEmpaque != null) registro.CantidaddeEmpaque = dto.CantidaddeEmpaque;
                    if (dto.Barcode != null) registro.Barcode = dto.Barcode;
                    if (dto.Kanban != null) registro.Kanban = dto.Kanban;
                    if (dto.Estacion != null) registro.Estacion = dto.Estacion;

                    // Selects
                    if (dto.Familia != null) registro.Familia = dto.Familia;
                    if (dto.Proceso != null) registro.Proceso = dto.Proceso;

                    // Inputs numéricos
                    if (dto.PesoMax.HasValue) registro.PesoMax = dto.PesoMax;
                    if (dto.PesoMin.HasValue) registro.PesoMin = dto.PesoMin;
                }

                _context.SaveChanges();

                return Ok(new { message = $"{registros.Count} registros editados correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al editar registros: {ex.Message}");
            }
        }
    }
}