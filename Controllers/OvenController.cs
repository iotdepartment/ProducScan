using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProducScan.Helpers;
using ProducScan.Models;

namespace ProducScan.Controllers
{
    public class OvenController : Controller
    {
        private readonly AppDbContext _context;

        public OvenController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Mandriles()
        {
            return View();
        }

        // SOLO MANDRILES DONDE AREA = OVEN
        public IActionResult GetMandrilesOven()
        {
            var data = _context.Mandriles
                .Where(m => m.Area == "OVEN")
                .Select(m => new
                {
                    m.Id,
                    m.MandrilNombre,
                    m.Estacion
                })
                .ToList();

            return Json(new { data });
        }

        // ACTUALIZACIÓN EN LOTE
        [HttpPost]
        public IActionResult UpdateOven([FromBody] UpdateOvenRequest req)
        {
            if (req == null || req.Ids == null || req.Ids.Count == 0)
                return BadRequest("No se enviaron registros.");

            var mandriles = _context.Mandriles
                .Where(m => req.Ids.Contains(m.Id))
                .ToList();

            foreach (var m in mandriles)
            {
                m.Estacion = req.NewEstacion;
            }

            _context.SaveChanges();

            return Ok(new { success = true });
        }

        [Authorize(Roles = "Admin,Gerente")]
        [HttpPost]
        public IActionResult GetTopMandrilesPorOven(DateTime fechaInicio, DateTime fechaFin, string oven)
        {
            var inicio = DateOnly.FromDateTime(fechaInicio);
            var fin = DateOnly.FromDateTime(fechaFin);

            // Mandriles asignados a este OVEN
            var mandrilesOven = _context.Mandriles
                .Where(m => m.Area == "OVEN" && m.Estacion == oven)
                .Select(m => new { m.MandrilNombre, Costo = m.Costo ?? 0 })
                .ToDictionary(m => m.MandrilNombre, m => m.Costo);

            if (!mandrilesOven.Any())
                return Json(new List<object>());

            var defectosRaw = _context.RegistrodeDefectos
                .Where(d => d.Fecha >= inicio.AddDays(-1) && d.Fecha <= fin.AddDays(1))
                .Where(d => mandrilesOven.Keys.Contains(d.Mandrel))
                .ToList();

            var defectos = defectosRaw
                .Select(d => {
                    var fechaEvento = d.Fecha.ToDateTime(d.Hora);
                    var fechaLaboral = ProduccionHelper.GetFechaProduccion(fechaEvento).Date;

                    return new
                    {
                        FechaLaboral = fechaLaboral,
                        d.Mandrel,
                        d.CodigodeDefecto,
                        d.Defecto
                    };
                })
                .Where(x => x.FechaLaboral >= inicio.ToDateTime(TimeOnly.MinValue).Date &&
                            x.FechaLaboral <= fin.ToDateTime(TimeOnly.MinValue).Date)
                .ToList();

            var resultado = defectos
                .GroupBy(x => x.Mandrel)
                .Select(g => new {
                    Mandril = g.Key,
                    TotalCosto = g.Count() * mandrilesOven[g.Key],
                    Defectos = g.GroupBy(d => new { d.CodigodeDefecto, d.Defecto })
                                .Select(dg => new {
                                    Codigo = dg.Key.CodigodeDefecto,
                                    Nombre = dg.Key.Defecto,
                                    Total = dg.Count()
                                })
                                .OrderByDescending(d => d.Total)
                                .Take(3)
                                .ToList()
                })
                .OrderByDescending(x => x.TotalCosto)
                .Take(10)
                .ToList();

            return Json(resultado);
        }

        [HttpGet]
        public IActionResult GetOvens()
        {
            var ovens = _context.Mandriles
                .Where(m => m.Area == "OVEN" && m.Estacion != null && m.Estacion.Trim() != "")
                .Select(m => m.Estacion!.Trim())
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            return Json(ovens);
        }
    }

    public class UpdateOvenRequest
    {
        public List<int> Ids { get; set; }
        public string NewEstacion { get; set; }
    }
}