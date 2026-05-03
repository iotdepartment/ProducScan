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

        [HttpGet]
        public IActionResult Detalle(string oven)
        {
            if (string.IsNullOrWhiteSpace(oven))
                return RedirectToAction("Mandriles");

            ViewBag.Oven = oven;
            return View();
        }

        [HttpPost]
        public IActionResult GetDefectosPorOven(string oven)
        {
            // 1. Obtener mandriles asignados al OVEN
            var mandriles = _context.Mandriles
                .Where(m => m.Area == "OVEN" && m.Estacion == oven)
                .Select(m => m.MandrilNombre)
                .ToList();

            if (!mandriles.Any())
                return Json(new { mandriles = new List<object>() });

            // 2. Obtener defectos de esos mandriles
            var defectos = _context.RegistrodeDefectos
                .Where(d => mandriles.Contains(d.Mandrel))
                .ToList();

            // 3. Agrupar defectos por mandril
            var resultado = defectos
                .GroupBy(d => d.Mandrel)
                .Select(g => new
                {
                    Mandril = g.Key,
                    Defectos = g.GroupBy(x => new { x.CodigodeDefecto, x.Defecto })
                                .Select(dg => new
                                {
                                    Codigo = dg.Key.CodigodeDefecto,
                                    Nombre = dg.Key.Defecto,
                                    Total = dg.Count()
                                })
                                .OrderByDescending(x => x.Total)
                                .ToList()
                })
                .ToList();

            return Json(resultado);
        }

        
        [HttpGet]
        public IActionResult GetTop5MandrilesPorOvenTurno(string oven)
        {
            if (string.IsNullOrWhiteSpace(oven))
                return Json(new { data = new List<object>() });

            // Hora local Matamoros
            DateTime ahora = ProduccionHelper.GetMatamorosTime();

            // Turno actual
            string turno = ProduccionHelper.GetTurno(ahora);

            // Fecha laboral actual
            DateTime fechaLaboralDT = ProduccionHelper.GetFechaProduccion(ahora);
            DateOnly fechaLaboral = DateOnly.FromDateTime(fechaLaboralDT);

            // Rango de horas del turno
            TimeSpan inicioTurno, finTurno;

            if (turno == "1")
            {
                inicioTurno = new TimeSpan(7, 0, 0);
                finTurno = new TimeSpan(15, 44, 59);
            }
            else if (turno == "2")
            {
                inicioTurno = new TimeSpan(15, 45, 0);
                finTurno = new TimeSpan(23, 49, 59);
            }
            else // turno 3
            {
                // Turno 3 tiene dos segmentos
                // 23:50 → 23:59 (día actual)
                // 00:00 → 07:09 (día anterior)
                // PERO tu helper ya ajusta la fecha laboral
                inicioTurno = new TimeSpan(0, 0, 0);
                finTurno = new TimeSpan(23, 59, 59);
            }

            // 1. Mandriles del OVEN
            var mandrilesOven = _context.Mandriles
                .Where(m => m.Area == "OVEN" && m.Estacion == oven)
                .GroupBy(m => m.MandrilNombre.Trim())
                .Select(g => g.First())
                .ToDictionary(
                    m => m.MandrilNombre.Trim(),
                    m => m.Costo ?? 0
                );

            if (!mandrilesOven.Any())
                return Json(new { data = new List<object>() });

            // 2. Defectos del turno actual
            var defectos = _context.RegistrodeDefectos
                .AsEnumerable()
                .Where(d =>
                {
                    DateTime fechaEvento = d.Fecha.ToDateTime(d.Hora);
                    DateTime fechaLaboralEvento = ProduccionHelper.GetFechaProduccion(fechaEvento);

                    // Fecha laboral debe coincidir
                    if (DateOnly.FromDateTime(fechaLaboralEvento) != fechaLaboral)
                        return false;

                    // Mandril debe pertenecer al OVEN
                    string mandril = d.Mandrel.Trim();
                    if (!mandrilesOven.ContainsKey(mandril))
                        return false;

                    // Hora dentro del turno
                    TimeSpan hora = fechaEvento.TimeOfDay;

                    if (turno == "3")
                    {
                        // Turno 3 tiene dos segmentos
                        return (hora >= new TimeSpan(23, 50, 0) && hora <= new TimeSpan(23, 59, 59))
                            || (hora >= TimeSpan.Zero && hora <= new TimeSpan(7, 9, 59));
                    }

                    return hora >= inicioTurno && hora <= finTurno;
                })
                .ToList();

            // 3. Agrupar por mandril
            var resultado = defectos
                .GroupBy(d => d.Mandrel.Trim())
                .Select(g => new
                {
                    Mandril = g.Key,
                    CostoTotal = g.Count() * mandrilesOven[g.Key],
                    TopDefectos = g.GroupBy(x => new { x.CodigodeDefecto, x.Defecto })
                                   .Select(dg => new
                                   {
                                       Codigo = dg.Key.CodigodeDefecto,
                                       Nombre = dg.Key.Defecto,
                                       Total = dg.Count()
                                   })
                                   .OrderByDescending(x => x.Total)
                                   .Take(3)
                                   .ToList()
                })
                .OrderByDescending(x => x.CostoTotal)
                .Take(5)
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