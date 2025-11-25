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

    public async Task<IActionResult> Index(int page = 1)
    {
        int pageSize = 10;

        var registros = _context.RegistrodeDefectos.AsQueryable();

        ViewBag.Mandrels = _context.Mandriles.ToList();
        ViewBag.Mesas = _context.Mesas.ToList();
        ViewBag.Usuarios = _context.Users.ToList();

        // --- Determinar turno actual ---
        var zona = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
        var ahora = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);
        var horaActual = ahora.TimeOfDay;

        string turnoSeleccionado;
        if (horaActual >= new TimeSpan(7, 0, 0) && horaActual <= new TimeSpan(15, 44, 59))
            turnoSeleccionado = "1";
        else if (horaActual >= new TimeSpan(15, 45, 0) && horaActual <= new TimeSpan(23, 49, 59))
            turnoSeleccionado = "2";
        else
            turnoSeleccionado = "3";

        // Normalizar con ProduccionHelper
        var fechaLaboral = DateOnly.FromDateTime(ProduccionHelper.GetFechaProduccion(ahora));


        // ✅ Pasar turno y fecha laboral a la vista
        ViewBag.FechaSeleccionada = fechaLaboral.ToString("yyyy-MM-dd");
        ViewBag.TurnoSeleccionado = turnoSeleccionado;



        var paginated = await PaginatedList<RegistrodeDefecto>.CreateAsync(registros, page, pageSize);

        return View(paginated);
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
            .AsEnumerable() // 👈 pasamos a memoria para poder usar int.Parse
            .Where(m => int.TryParse(m.Id, out var id) && id >= 3 && id <= 24)
            .OrderBy(m => int.Parse(m.Id)) // 👈 orden numérico
            .Select(m => m.Mesas.ToUpper()) // 👈 convertir a mayúsculas
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
                    Categoria = "Defecto",
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
                Categoria = "Defecto",
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
            .Where(m => m.MandrilNombre.Contains(term))
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

    [HttpPost]
    public async Task<IActionResult> GetDefectos([FromForm] DataTablesRequest request, string fecha, string turno)
    {
        var baseQuery = _context.RegistrodeDefectos.AsQueryable();
        List<RegistrodeDefecto> registros;

        if (!string.IsNullOrEmpty(fecha) && DateOnly.TryParse(fecha, out var fechaFiltro))
        {
            // Rango de +/-1 día para no traer toda la tabla
            var fechaInicio = fechaFiltro.AddDays(-1);
            var fechaFin = fechaFiltro.AddDays(1);

            // Traemos en SQL solo por rango de fechas naturales
            registros = await baseQuery
                .Where(x => x.Fecha >= fechaInicio && x.Fecha <= fechaFin)
                .ToListAsync();

            // Ahora aplicamos el helper en memoria
            registros = registros
                .Where(x => ProduccionHelper
                    .GetFechaProduccion(x.Fecha.ToDateTime(x.Hora)).Date
                    == fechaFiltro.ToDateTime(TimeOnly.MinValue).Date)
                .ToList();
        }
        else
        {
            registros = await baseQuery.ToListAsync();
        }

        // --- Determinar turno actual si no se seleccionó ---
        string turnoSeleccionado = turno;
        if (string.IsNullOrWhiteSpace(turnoSeleccionado))
        {
            var zona = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
            var ahora = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);
            var horaActual = ahora.TimeOfDay;

            if (horaActual >= new TimeSpan(7, 0, 0) && horaActual <= new TimeSpan(15, 44, 59))
                turnoSeleccionado = "1";
            else if (horaActual >= new TimeSpan(15, 45, 0) && horaActual <= new TimeSpan(23, 49, 59))
                turnoSeleccionado = "2";
            else
                turnoSeleccionado = "3";
        }

        // Convertimos a IQueryable para seguir aplicando filtros
        var query = registros.AsQueryable();

        // ✅ Filtro por turno (solo si no es "Todos")
        if (!string.IsNullOrEmpty(turnoSeleccionado))
        {
            query = query.Where(x => x.Turno == turnoSeleccionado);
        }

        // Filtro global (búsqueda)
        if (!string.IsNullOrEmpty(request.Search?.Value))
        {
            var search = request.Search.Value.ToLower();
            query = query.Where(x =>
                x.Mandrel.ToLower().Contains(search) ||
                x.CodigodeDefecto.ToLower().Contains(search) ||
                x.NuMesa.ToLower().Contains(search) ||
                x.Tm.ToLower().Contains(search)
            );
        }

        var totalRecords = await _context.RegistrodeDefectos.CountAsync();
        var filteredRecords = query.Count();

        // Orden dinámico
        if (request.Order != null && request.Order.Any())
        {
            var order = request.Order.First();
            var columnName = request.Columns[order.Column].Data;
            var direction = order.Dir; // "asc" o "desc"

            switch (columnName.ToLower())
            {
                case "fecha":
                    query = direction == "asc" ? query.OrderBy(x => x.Fecha) : query.OrderByDescending(x => x.Fecha);
                    break;
                case "hora":
                    query = direction == "asc" ? query.OrderBy(x => x.Hora) : query.OrderByDescending(x => x.Hora);
                    break;
                case "mandrel":
                    query = direction == "asc" ? query.OrderBy(x => x.Mandrel) : query.OrderByDescending(x => x.Mandrel);
                    break;
                case "codigodedefecto":
                    query = direction == "asc" ? query.OrderBy(x => x.CodigodeDefecto) : query.OrderByDescending(x => x.CodigodeDefecto);
                    break;
                case "defecto":
                    query = direction == "asc" ? query.OrderBy(x => x.Defecto) : query.OrderByDescending(x => x.Defecto);
                    break;
                case "numesa":
                    query = direction == "asc" ? query.OrderBy(x => x.NuMesa) : query.OrderByDescending(x => x.NuMesa);
                    break;
                case "turno":
                    query = direction == "asc" ? query.OrderBy(x => x.Turno) : query.OrderByDescending(x => x.Turno);
                    break;
                case "tm":
                    query = direction == "asc" ? query.OrderBy(x => x.Tm) : query.OrderByDescending(x => x.Tm);
                    break;
                default:
                    query = query.OrderBy(x => x.Id); // orden por defecto
                    break;
            }
        }

        // 📄 Paginación
        var data = query
            .Skip(request.Start)
            .Take(request.Length)
            .ToList();

        // 📤 Respuesta en formato que DataTables espera
        return Json(new
        {
            draw = request.Draw,
            recordsTotal = totalRecords,
            recordsFiltered = filteredRecords,
            data = data,
            turnoSeleccionado // ✅ lo devolvemos para que la vista pueda marcar el select
        });
    }
}

