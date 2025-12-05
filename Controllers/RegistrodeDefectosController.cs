using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProducScan.Helpers;
using ProducScan.Models;
using ProducScan.Services;
using ProducScan.ViewModels;
using System.Globalization;


[Authorize]
public class RegistrodeDefectosController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogService _log;


    public RegistrodeDefectosController(AppDbContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    [HttpGet]
    public IActionResult Index(DateTime? fecha, string turno)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
        var ahora = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);

        // ✅ Fecha laboral actual
        var fechaSeleccionada = fecha ?? ProduccionHelper.GetFechaProduccion(ahora);
        var fechaFiltro = DateOnly.FromDateTime(fechaSeleccionada);

        // ✅ Turno actual
        string turnoSeleccionado = turno;
        var horaActual = ahora.TimeOfDay;

        if (string.IsNullOrEmpty(turnoSeleccionado))
        {
            turnoSeleccionado =
                horaActual >= new TimeSpan(7, 10, 0) && horaActual <= new TimeSpan(15, 44, 59) ? "1" :
                horaActual >= new TimeSpan(15, 45, 0) && horaActual <= new TimeSpan(23, 49, 59) ? "2" :
                "3";
        }

        // ✅ 1. Reducir dataset desde SQL (solo columnas necesarias)
        var defectosQuery = _context.RegistrodeDefectos
            .Where(d => d.Fecha >= fechaFiltro.AddDays(-1) && d.Fecha <= fechaFiltro.AddDays(1))
            .Select(d => new
            {
                d.Fecha,
                d.Hora,
                d.Mandrel,
                d.CodigodeDefecto,
                d.Defecto,
                d.Turno
            })
            .ToList(); // ✅ Solo 1 ToList()

        // ✅ 2. Filtrar por día laboral real (en memoria)
        var defectosRaw = defectosQuery
            .Where(d =>
                ProduccionHelper.GetFechaProduccion(d.Fecha.ToDateTime(d.Hora)).Date
                == fechaFiltro.ToDateTime(TimeOnly.MinValue).Date
            )
            .ToList();

        // ✅ Mandriles únicos
        ViewBag.Mandriles = defectosRaw
            .Select(d => d.Mandrel?.Trim())
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        // ✅ Códigos de defecto únicos
        ViewBag.CodigosDefecto = defectosRaw
            .Select(d => d.CodigodeDefecto?.Trim())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        ViewBag.FechaSeleccionada = fechaFiltro.ToString("yyyy-MM-dd");
        ViewBag.TurnoSeleccionado = turnoSeleccionado;

        return View();
    }

    [HttpGet]
    public IActionResult List()
    {
        var defectos = _context.Defectos
       .Select(d => new {
           value = d.CodigodeDefecto,   // 👈 este será el value del <option>
           text = d.CodigodeDefecto + " - " + d.Defecto1
       })
       .ToList();

        return Json(defectos);

    }

    // METODOS PARA CREAR, EDITAR, ELIMINAR DEFECTOS 
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Create()
    {
        // Obtener la zona horaria de Matamoros (Central Standard Time)
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        var nowInMatamoros = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        var model = new RegistrodeDefecto
        {
            Fecha = DateOnly.FromDateTime(nowInMatamoros),
            Hora = TimeOnly.FromDateTime(nowInMatamoros)
        };

        // Traer mesas de la tabla Mesas, IDs 3 a 24, ordenadas numéricamente
        var mesas = _context.Mesas
          .Where(m => m.IdMesa >= 3 && m.IdMesa <= 24)   // 👈 filtro directo por IdMesa
          .OrderBy(m => m.IdMesa)                        // 👈 orden numérico natural
          .Select(m => m.Mesas.ToUpper())                // 👈 convertir a mayúsculas
          .ToList();

        ViewBag.Mesas = mesas;
        ViewBag.Turnos = new List<string> { "1", "2", "3" };

        return PartialView("_CreateModalDefecto", model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(RegistrodeDefecto model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var defecto = _context.Defectos.FirstOrDefault(d => d.CodigodeDefecto == model.CodigodeDefecto);

                if (defecto == null)
                {
                    return Json(new { success = false, message = "Código de defecto no válido." });
                }

                model.Defecto = defecto.Defecto1;


                _context.RegistrodeDefectos.Add(model);
                await _context.SaveChangesAsync();

                _log.Registrar("Alta Defecto", $"{model.CodigodeDefecto} - {model.Defecto} | {model.Mandrel} | {model.NuMesa}", "Success", categoria: "Defecto");
                return Json(new { success = true, message = "Registro de defecto guardado correctamente." });
            }
            catch (Exception ex)
            {
                // 👇 devolvemos el error exacto
                return Json(new { success = false, message = "Excepción: " + ex.Message, stack = ex.StackTrace });
            }
        }


        // 👇 devolvemos todos los errores de validación
        var errores = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        return Json(new { success = false, message = "Modelo inválido", errors = errores });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult EditarDefectoModal(int id)
    {
        var registro = _context.RegistrodeDefectos
            .FirstOrDefault(r => r.Id == id);

        if (registro == null)
            return NotFound();

        return PartialView("_EditarDefectoModal", registro);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public IActionResult EditarDefecto(RegistrodeDefecto model, string CodigodeDefecto)
    {
        try
        {
            if (string.IsNullOrEmpty(CodigodeDefecto))
                return Json(new { success = false, message = "Debes seleccionar un defecto válido." });

            var partes = CodigodeDefecto.Split('|');
            var codigo = partes[0].Trim();
            var defecto = partes[1].Trim();

            var registro = _context.RegistrodeDefectos.FirstOrDefault(d => d.Id == model.Id);
            if (registro == null)
                return Json(new { success = false, message = "El registro no existe." });

            // 🔎 Comparar campo por campo
            var cambios = new List<string>();

            if (registro.NuMesa != model.NuMesa)
            {
                cambios.Add($"NuMesa: '{registro.NuMesa}' → '{model.NuMesa}'");
                registro.NuMesa = model.NuMesa;
            }
            if (registro.Turno != model.Turno)
            {
                cambios.Add($"Turno: '{registro.Turno}' → '{model.Turno}'");
                registro.Turno = model.Turno;
            }
            if (registro.Mandrel != model.Mandrel)
            {
                cambios.Add($"Mandrel: '{registro.Mandrel}' → '{model.Mandrel}'");
                registro.Mandrel = model.Mandrel;
            }
            if (registro.CodigodeDefecto != codigo)
            {
                cambios.Add($"Código: '{registro.CodigodeDefecto}' → '{codigo}'");
                registro.CodigodeDefecto = codigo;
            }
            if (registro.Defecto != defecto)
            {
                cambios.Add($"Defecto: '{registro.Defecto}' → '{defecto}'");
                registro.Defecto = defecto;
            }
            if (registro.Hora != model.Hora)
            {
                cambios.Add($"Hora: '{registro.Hora}' → '{model.Hora}'");
                registro.Hora = model.Hora;
            }
            if (registro.Tm != model.Tm) // 👈 nuevo bloque para TM
            {
                cambios.Add($"TM: '{registro.Tm}' → '{model.Tm}'");
                registro.Tm = model.Tm;
            }

            // Guardar cambios en la BD
            _context.SaveChanges();

            // Solo registrar log si hubo cambios
            if (cambios.Any())
            {
                var log = new Log
                {
                    Fecha = DateTime.Now,
                    Usuario = User.Identity?.Name ?? "Sistema",
                    Accion = "Actualizar Defecto",
                    Nivel = "Info",
                    Categoria = "Producción",
                    Detalles = $"Defecto ID {registro.Id} actualizado. Cambios: {string.Join("; ", cambios)}"
                };
                _context.Logs.Add(log);
                _context.SaveChanges();
            }

            return Json(new { success = true, message = "El defecto se actualizó correctamente" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error al guardar: " + ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost]
    public IActionResult BorrarDefecto(int id)
    {
        try
        {
            var defecto = _context.RegistrodeDefectos.FirstOrDefault(d => d.Id == id);
            if (defecto == null)
                return Json(new { success = false, message = "El registro no existe." });

            // Guardar datos para el log antes de borrar
            var detalles = $"Defecto ID {defecto.Id} | Mesa={defecto.NuMesa} | Turno={defecto.Turno} | Mandrel={defecto.Mandrel} | " +
                           $"Código={defecto.CodigodeDefecto} | Defecto={defecto.Defecto} | Hora={defecto.Hora}";

            _context.RegistrodeDefectos.Remove(defecto);
            _context.SaveChanges();

            // Registrar log
            var log = new Log
            {
                Fecha = DateTime.Now,
                Usuario = User.Identity?.Name ?? "Sistema",
                Accion = "Eliminar Defecto",
                Nivel = "Warning",
                Categoria = "Producción",
                Detalles = $"Se eliminó: {detalles}"
            };
            _context.Logs.Add(log);
            _context.SaveChanges();

            return Json(new { success = true, message = "El defecto se eliminó correctamente" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error al borrar: " + ex.Message });
        }
    }

    [HttpGet]
    public JsonResult BuscarTM(string term)
    {
        var tms = _context.Users
            .Where(u => u.Nombre.Contains(term))
            .Select(u => u.Nombre)
            .Take(10)
            .ToList();

        return Json(tms);
    }

    [HttpGet]
    public JsonResult BuscarMandrel(string term)
    {
        var mandriles = _context.Mandriles
            .Where(m => m.Area == "INSPECCION" && m.MandrilNombre.Contains(term))
            .Select(m => m.MandrilNombre)
            .Take(10)
            .ToList();

        return Json(mandriles);
    }

    [HttpGet]
    public IActionResult ReporteDefectosPorTM(DateOnly? fecha)
    {
        if (!fecha.HasValue)
            fecha = DateOnly.FromDateTime(DateTime.Today);

        var defectos = _context.RegistrodeDefectos
            .Where(d => d.Fecha == fecha.Value && !string.IsNullOrEmpty(d.Tm))
            .ToList();

        var reporte = defectos
            .GroupBy(d => new { d.Tm, d.NuMesa, d.Turno, d.Fecha })
            .Select(g => new DefectoPorTMViewModel
            {
                TM = g.Key.Tm.Trim(),
                NuMesa = g.Key.NuMesa.Trim(),
                Turno = g.Key.Turno.Trim(),
                Fecha = g.Key.Fecha,
                TotalDefectos = g.Count(),
                Defectos = g.GroupBy(x => new { x.Defecto, x.Mandrel })
                    .Select(dg => new DefectoDetalleViewModel
                    {
                        Defecto = dg.Key.Defecto,
                        Mandrel = dg.Key.Mandrel,
                        Cantidad = dg.Count()
                    }).ToList()
            })
            .OrderBy(r => r.TM)
            .ThenBy(r => r.NuMesa)
            .ThenBy(r => r.Turno)
            .ToList();

        return View(reporte);
    }

    [HttpGet]
    public IActionResult DetalleDefectosPorUsuarios(DateOnly fecha, string usuario, string turno)
    {
        ViewBag.DefectosCatalogo = _context.Defectos
            .Select(c => new
            {
                Value = c.CodigodeDefecto + "|" + c.Defecto1,
                Text = c.CodigodeDefecto + " - " + c.Defecto1
            })
            .ToList();

        // Traemos un rango de +/-1 día en SQL
        var registros = _context.RegistrodeDefectos
            .Where(r => r.Fecha >= fecha.AddDays(-1) && r.Fecha <= fecha.AddDays(1)
                     && r.Tm != null && r.Tm.Trim() == usuario.Trim())
            .ToList() // 👈 materializamos
            .Where(r => ProduccionHelper.GetFechaProduccion(r.Fecha.ToDateTime(r.Hora)).Date
                        == fecha.ToDateTime(TimeOnly.MinValue).Date
                     && (string.IsNullOrWhiteSpace(turno) || r.Turno.Trim().Equals(turno.Trim(), StringComparison.OrdinalIgnoreCase)))
            .OrderBy(r => r.Fecha)
            .ThenBy(r => r.Hora)
            .Select(r => new RegistroDefectoViewModel
            {
                Id = r.Id,
                Fecha = r.Fecha, // fecha real
                Tm = r.Tm,
                NuMesa = r.NuMesa,
                Turno = r.Turno,
                Mandrel = r.Mandrel,
                CodigodeDefecto = r.CodigodeDefecto,
                Defecto = r.Defecto,
                Hora = r.Hora,
                FechaLaboral = ProduccionHelper.GetFechaProduccion(r.Fecha.ToDateTime(r.Hora))
            })
            .ToList();

        var vm = new DetalleDefectosPorUsuarioViewModel
        {
            Usuario = usuario,
            Fecha = fecha.ToString("yyyy-MM-dd"), // Fecha laboral
            Defectos = registros
        };

        return View(vm);
    }


    private List<RegistrodeDefecto> ObtenerDefectosFiltrados(DateOnly fechaFiltro, string turno)
    {
        // ✅ 1. Reducir dataset desde SQL (solo columnas necesarias)
        var query = _context.RegistrodeDefectos
            .Where(d => d.Fecha >= fechaFiltro.AddDays(-1) && d.Fecha <= fechaFiltro.AddDays(1))
            .Select(d => new RegistrodeDefecto
            {
                Fecha = d.Fecha,
                Hora = d.Hora,
                Mandrel = d.Mandrel,
                CodigodeDefecto = d.CodigodeDefecto,
                Defecto = d.Defecto,
                Turno = d.Turno
            })
            .ToList(); // ✅ Solo 1 ToList()

        // ✅ 2. Filtrar por día laboral real (en memoria)
        query = query
            .Where(d =>
                ProduccionHelper.GetFechaProduccion(d.Fecha.ToDateTime(d.Hora)).Date
                == fechaFiltro.ToDateTime(TimeOnly.MinValue).Date
            )
            .ToList();

        // ✅ 3. Filtrar por turno
        if (!string.IsNullOrEmpty(turno))
            query = query.Where(d => d.Turno == turno).ToList();

        return query;
    }


    [HttpPost]
    public IActionResult GetDefectosAgrupados(string fecha, string turno, List<string> mandriles, List<string> codigos)
    {
        DateOnly fechaFiltro = DateOnly.Parse(fecha);

        var defectos = ObtenerDefectosFiltrados(fechaFiltro, turno);

        if (mandriles != null && mandriles.Any())
            defectos = defectos.Where(d => mandriles.Contains(d.Mandrel)).ToList();

        if (codigos != null && codigos.Any())
            defectos = defectos.Where(d => codigos.Contains(d.CodigodeDefecto)).ToList();

        var data = defectos
            .GroupBy(d => new { d.Turno, d.Mandrel, d.CodigodeDefecto, d.Defecto })
            .Select(g => new
            {
                turno = g.Key.Turno,
                mandril = g.Key.Mandrel,
                totalPiezas = g.Count(),
                defectoCompleto = $"{g.Key.CodigodeDefecto} - {g.Key.Defecto}"
            })
            .OrderBy(x => x.turno)
            .ThenBy(x => x.mandril)
            .ToList();

        return Json(new { data });
    }

    private string ObtenerDescripcionDefecto(string codigo)
    {
        var defecto = _context.RegistrodeDefectos
            .FirstOrDefault(x => x.CodigodeDefecto == codigo);

        return defecto?.Defecto ?? "Sin descripción";
    }

    [HttpPost]
    public IActionResult GetCodigos(string fecha, string turno)
    {
        DateOnly fechaFiltro = DateOnly.Parse(fecha);

        var defectos = ObtenerDefectosFiltrados(fechaFiltro, turno);

        var lista = defectos
            .Where(d => !string.IsNullOrWhiteSpace(d.CodigodeDefecto))
            .Select(d => new
            {
                value = d.CodigodeDefecto!.Trim(),
                text = $"{d.CodigodeDefecto!.Trim()} - {d.Defecto?.Trim()}"
            })
            .Distinct()
            .OrderBy(x => x.value)
            .ToList();

        return Json(lista);
    }


    [HttpPost]
    public IActionResult GetMandriles(string fecha, string turno)
    {
        DateOnly fechaFiltro = DateOnly.Parse(fecha);

        var defectos = ObtenerDefectosFiltrados(fechaFiltro, turno);

        var mandriles = defectos
            .Select(d => d.Mandrel?.Trim())
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        return Json(mandriles);
    }


    [HttpPost]
    public IActionResult ExportarExcel(
     DateTime fechaInicio,
     DateTime fechaFin,
     string turno,
     List<string> mandriles,
     List<string> codigos)
    {
        DateOnly inicio = DateOnly.FromDateTime(fechaInicio);
        DateOnly fin = DateOnly.FromDateTime(fechaFin);

        // ✅ 1. Reducir dataset desde SQL
        var query = _context.RegistrodeDefectos
            .Where(d => d.Fecha >= inicio.AddDays(-1) && d.Fecha <= fin.AddDays(1))
            .Select(d => new
            {
                d.Fecha,
                d.Hora,
                d.Mandrel,
                d.CodigodeDefecto,
                d.Defecto,
                d.Turno
            })
            .ToList();

        // ✅ 2. Filtrar por fecha laboral real
        var defectos = query
            .Select(d => new
            {
                FechaLaboral = ProduccionHelper.GetFechaProduccion(d.Fecha.ToDateTime(d.Hora)).Date,
                d.Turno,
                d.Mandrel,
                d.CodigodeDefecto,
                d.Defecto
            })
            .Where(d => d.FechaLaboral >= inicio.ToDateTime(TimeOnly.MinValue).Date &&
                        d.FechaLaboral <= fin.ToDateTime(TimeOnly.MinValue).Date)
            .ToList();

        // ✅ 3. Filtrar por turno
        if (!string.IsNullOrEmpty(turno))
            defectos = defectos.Where(d => d.Turno == turno).ToList();

        // ✅ 4. Filtrar por mandriles
        if (mandriles != null && mandriles.Any())
            defectos = defectos.Where(d => mandriles.Contains(d.Mandrel)).ToList();

        // ✅ 5. Filtrar por códigos
        if (codigos != null && codigos.Any())
            defectos = defectos.Where(d => codigos.Contains(d.CodigodeDefecto)).ToList();

        // ✅ 6. Agrupar para hoja principal
        var data = defectos
            .GroupBy(d => new
            {
                d.FechaLaboral,
                d.Turno,
                d.Mandrel,
                d.CodigodeDefecto,
                d.Defecto
            })
            .Select(g => new
            {
                Fecha = g.Key.FechaLaboral,
                Turno = g.Key.Turno,
                Mandril = g.Key.Mandrel,
                Codigo = g.Key.CodigodeDefecto,
                Defecto = g.Key.Defecto,
                TotalPiezas = g.Count()
            })
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Turno)
            .ThenBy(x => x.Mandril)
            .ToList();

        // ✅ 7. Totales por día
        var totalesPorDia = data
            .GroupBy(x => x.Fecha)
            .Select(g => new
            {
                Fecha = g.Key,
                Total = g.Sum(x => x.TotalPiezas)
            })
            .OrderBy(x => x.Fecha)
            .ToList();

        // ✅ 8. Totales por turno
        var totalesPorTurno = data
            .GroupBy(x => x.Turno)
            .Select(g => new
            {
                Turno = g.Key,
                Total = g.Sum(x => x.TotalPiezas)
            })
            .OrderBy(x => x.Turno)
            .ToList();


        // ✅ 9. Totales por turno por día (pivot)
        var totalesPorDiaTurno = data
            .GroupBy(x => new { x.Fecha, x.Turno })
            .Select(g => new
            {
                Fecha = g.Key.Fecha,
                Turno = g.Key.Turno,
                Total = g.Sum(x => x.TotalPiezas)
            })
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Turno)
            .ToList();

        // ✅ Convertir a estructura pivot
        var fechasUnicas = totalesPorDiaTurno.Select(x => x.Fecha).Distinct().OrderBy(x => x).ToList();

        // ✅ 9. Crear Excel
        using var workbook = new ClosedXML.Excel.XLWorkbook();

        /* ---------------------------------------------------------
           ✅ HOJA 1: DETALLES
        --------------------------------------------------------- */
        var ws = workbook.Worksheets.Add("Defectos");

        ws.Cell(1, 1).Value = "Fecha";
        ws.Cell(1, 2).Value = "Turno";
        ws.Cell(1, 3).Value = "Mandril";
        ws.Cell(1, 4).Value = "Código";
        ws.Cell(1, 5).Value = "Defecto";
        ws.Cell(1, 6).Value = "Total Piezas";

        ws.Range("A1:F1").Style.Font.Bold = true;
        ws.Range("A1:F1").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

        // ✅ Encabezados con columna extra
        ws.Cell(1, 1).Value = "Fecha";
        ws.Cell(1, 2).Value = "Turno";
        ws.Cell(1, 3).Value = "Etiqueta Turno";
        ws.Cell(1, 4).Value = "Mandril";
        ws.Cell(1, 5).Value = "Código";
        ws.Cell(1, 6).Value = "Defecto";
        ws.Cell(1, 7).Value = "Total Piezas";

        ws.Range("A1:G1").Style.Font.Bold = true;
        ws.Range("A1:G1").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

        int row = 2;

        foreach (var item in data)
        {
            // ✅ Escribir datos
            ws.Cell(row, 1).Value = item.Fecha.ToString("yyyy-MM-dd");
            ws.Cell(row, 2).Value = item.Turno;

            // ✅ Etiqueta visible
            string etiqueta =
                item.Turno == "1" ? "TURNO 1" :
                item.Turno == "2" ? "TURNO 2" :
                "TURNO 3";

            ws.Cell(row, 3).Value = etiqueta;
            ws.Cell(row, 4).Value = item.Mandril;
            ws.Cell(row, 5).Value = item.Codigo;
            ws.Cell(row, 6).Value = item.Defecto;
            ws.Cell(row, 7).Value = item.TotalPiezas;

            // ✅ Colorear fila según turno
            var fila = ws.Range($"A{row}:G{row}");

            if (item.Turno == "1")
                fila.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightBlue;

            else if (item.Turno == "2")
                fila.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightYellow;

            else if (item.Turno == "3")
                fila.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGreen;

            row++;
        }

        ws.Columns().AdjustToContents();

        /* ---------------------------------------------------------
           ✅ HOJA 2: RESUMEN
        --------------------------------------------------------- */
        /* ---------------------------------------------------------
    ✅ HOJA 2: RESUMEN
 --------------------------------------------------------- */
        var resumen = workbook.Worksheets.Add("Resumen");

        /* -------------------------------
           ✅ TOTALES POR DÍA
        -------------------------------- */
        resumen.Cell(1, 1).Value = "Totales por día";
        resumen.Cell(1, 1).Style.Font.Bold = true;

        resumen.Cell(2, 1).Value = "Fecha";
        resumen.Cell(2, 2).Value = "Total piezas";
        resumen.Range("A2:B2").Style.Font.Bold = true;

        int r = 3;
        foreach (var item in totalesPorDia)
        {
            resumen.Cell(r, 1).Value = item.Fecha.ToString("yyyy-MM-dd");
            resumen.Cell(r, 2).Value = item.Total;
            r++;
        }

        r += 2;

        /* -------------------------------
           ✅ TOTALES POR TURNO POR DÍA (PIVOT)
        -------------------------------- */
        resumen.Cell(r, 1).Value = "Producción por turno por día";
        resumen.Cell(r, 1).Style.Font.Bold = true;

        r++;

        resumen.Cell(r, 1).Value = "Fecha";
        resumen.Cell(r, 2).Value = "Turno 1";
        resumen.Cell(r, 3).Value = "Turno 2";
        resumen.Cell(r, 4).Value = "Turno 3";
        resumen.Cell(r, 5).Value = "Total Día";

        resumen.Range($"A{r}:E{r}").Style.Font.Bold = true;

        r++;

        foreach (var fecha in fechasUnicas)
        {
            int totalDia = 0;

            resumen.Cell(r, 1).Value = fecha.ToString("yyyy-MM-dd");

            // Turno 1
            var t1 = totalesPorDiaTurno.FirstOrDefault(x => x.Fecha == fecha && x.Turno == "1")?.Total ?? 0;
            resumen.Cell(r, 2).Value = t1;
            totalDia += t1;

            // Turno 2
            var t2 = totalesPorDiaTurno.FirstOrDefault(x => x.Fecha == fecha && x.Turno == "2")?.Total ?? 0;
            resumen.Cell(r, 3).Value = t2;
            totalDia += t2;

            // Turno 3
            var t3 = totalesPorDiaTurno.FirstOrDefault(x => x.Fecha == fecha && x.Turno == "3")?.Total ?? 0;
            resumen.Cell(r, 4).Value = t3;
            totalDia += t3;

            // Total del día
            resumen.Cell(r, 5).Value = totalDia;

            r++;
        }

        resumen.Columns().AdjustToContents();

        /* ---------------------------------------------------------
           ✅ DESCARGA
        --------------------------------------------------------- */
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Defectos_{inicio}_{fin}.xlsx"
        );
    }

}

