using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProducScan.Helpers;
using ProducScan.Models;
using ProducScan.Services;
using ProducScan.ViewModels;
using ProducScan.ViewModels.Dashboard;
using System.Globalization;
using System.Linq.Dynamic.Core;

[Authorize]
public class PiezasEscaneadasController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogService _log;


    public PiezasEscaneadasController(AppDbContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        int pageSize = 10;

        var Escaneadas = _context.RegistrodePiezasEscaneadas.AsQueryable();

        ViewBag.Mandrels = _context.Mandrels.ToList();
        ViewBag.Mesas = _context.Mesas.ToList();
        ViewBag.Usuarios = _context.Users.ToList();

        var paginated = await PaginatedList<RegistrodePiezasEscaneada>.CreateAsync(Escaneadas, page, pageSize);

        return View(paginated);
    }


    public IActionResult InspeccionTM(DateTime? fecha, string turno)
    {
        // Zona horaria de Matamoros
        var zona = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
        var fechaSeleccionada = fecha ?? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);
        var fechaFiltro = DateOnly.FromDateTime(fechaSeleccionada);

        // Determinar turno
        string turnoSeleccionado = turno;
        if (string.IsNullOrEmpty(turnoSeleccionado))
        {
            var horaActual = fechaSeleccionada.TimeOfDay;
            if (horaActual >= new TimeSpan(7, 0, 0) && horaActual <= new TimeSpan(15, 44, 59))
                turnoSeleccionado = "1";
            else if (horaActual >= new TimeSpan(15, 45, 0) && horaActual <= new TimeSpan(23, 49, 59))
                turnoSeleccionado = "2";
            else
                turnoSeleccionado = "3";
        }

        // Meta según turno
        int meta = turnoSeleccionado == "1" ? 1800 :
                   turnoSeleccionado == "2" ? 1800 : 1200;

        var usuarios = _context.Users.ToList();

        // --- Producciones ---
        var producciones = _context.RegistrodePiezasEscaneadas
            .Where(r => r.Fecha >= fechaFiltro.AddDays(-1) && r.Fecha <= fechaFiltro.AddDays(1))
            .ToList()
            .Where(r => ProduccionHelper.GetFechaProduccion(r.Fecha.ToDateTime(r.Hora)).Date
                        == fechaFiltro.ToDateTime(TimeOnly.MinValue).Date
                        && r.Turno == turnoSeleccionado)
            .GroupBy(r => r.Tm)
            .Select(g => new
            {
                TM = g.Key,
                PiezasBuenas = g.Sum(x => int.TryParse(x.Ndpiezas, out var n) ? n : 0)
            })
            .ToList();

        // --- Defectos ---
        var defectos = _context.RegistrodeDefectos
            .Where(d => d.Fecha >= fechaFiltro.AddDays(-1) && d.Fecha <= fechaFiltro.AddDays(1))
            .ToList()
            .Where(d => ProduccionHelper.GetFechaProduccion(d.Fecha.ToDateTime(d.Hora)).Date
                        == fechaFiltro.ToDateTime(TimeOnly.MinValue).Date
                        && d.Turno == turnoSeleccionado)
            .GroupBy(d => d.Tm)
            .Select(g => new { TM = g.Key, PiezasMalas = g.Count() })
            .ToList();

        // --- ViewModel ---
        var model = producciones.Select(p =>
        {
            var usuario = usuarios.FirstOrDefault(u => u.Nombre == p.TM);
            string numeroEmpleadoBD = usuario?.NumerodeEmpleado ?? "0000";

            string numeroEmpleadoFoto = int.Parse(numeroEmpleadoBD).ToString();

            // Rutas posibles
            string fotoPathJPG = Path.Combine("wwwroot/images/tm", $"{numeroEmpleadoFoto}.JPG");
            string fotoPathjpg = Path.Combine("wwwroot/images/tm", $"{numeroEmpleadoFoto}.jpg");

            string fotoUrl;
            if (System.IO.File.Exists(fotoPathJPG))
            {
                fotoUrl = $"/images/tm/{numeroEmpleadoFoto}.JPG";
            }
            else if (System.IO.File.Exists(fotoPathjpg))
            {
                fotoUrl = $"/images/tm/{numeroEmpleadoFoto}.jpg";
            }
            else
            {
                fotoUrl = "/images/tm/thumbnail.png";
            }

            int piezasMalas = defectos.FirstOrDefault(d => d.TM == p.TM)?.PiezasMalas ?? 0;
            int total = p.PiezasBuenas + piezasMalas;

            string color = total >= meta ? "bg-success-subtle text-success"
                         : total >= meta * 0.8 ? "bg-warning-subtle"
                         : "bg-danger-subtle text-white";

            return new InspeccionTMViewModel
            {
                TM = p.TM,
                NumeroEmpleado = numeroEmpleadoBD,
                FotoUrl = fotoUrl,
                PiezasBuenas = p.PiezasBuenas,
                PiezasMalas = piezasMalas,
                TotalPiezas = total,
                Meta = meta,
                ColorCard = color
            };
        }).ToList();

        ViewBag.FechaSeleccionada = fechaFiltro.ToString("yyyy-MM-dd");
        ViewBag.TurnoSeleccionado = turnoSeleccionado;

        return View(model);
    }


    //METODOS PARA CREAR, ACTUALIZAR Y ELIMINAR PRODUCCION
    [Authorize(Roles = "Admin,Editor")]
    [HttpGet]
    public IActionResult Create()
    {
        var model = new RegistrodePiezasEscaneada
        {
            Fecha = DateOnly.FromDateTime(DateTime.Now),
            Hora = TimeOnly.FromDateTime(DateTime.Now)
        };

        var mesas = _context.Mesas
            .AsEnumerable()
            .Where(m => int.TryParse(m.Id, out var id) && id >= 3 && id <= 24)
            .OrderBy(m => int.Parse(m.Id))
            .Select(m => m.Mesas.ToUpper())
            .ToList();

        ViewBag.Mesas = mesas;
        ViewBag.Turnos = new List<string> { "1", "2", "3" };

        return PartialView("_CreateModal", model);
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegistrodePiezasEscaneada model)
    {

        if (ModelState.IsValid)
        {
            try
            {
                _context.RegistrodePiezasEscaneadas.Add(model);
                await _context.SaveChangesAsync();

                _log.Registrar("Alta Pieza", $"{model.Ndpiezas} piezas | {model.Mandrel} | {model.NuMesa}", "Success", categoria: "Producción");
                return Json(new { success = true, message = "Registro de producción guardado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al guardar producción: " + ex.Message });
            }
        }

        return Json(new { success = false, message = "Modelo inválido." });
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost]
    public IActionResult Update(int Id, string Mandrel, string Ndpiezas, string Turno, string NuMesa, string Hora, RegistrodePiezasEscaneada model)
    {
        try
        {
            var registro = _context.RegistrodePiezasEscaneadas.Find(Id);
            if (registro == null)
                return Json(new { success = false, error = "Registro no encontrado" });

            // Guardamos valores anteriores
            var piezasAntes = registro.Ndpiezas;
            var mandrelAntes = registro.Mandrel;
            var mesaAntes = registro.NuMesa;
            var turnoAntes = registro.Turno;
            var horaAntes = registro.Hora;

            // Validar mandrel
            bool existeMandrel = _context.Mandrels.Any(m => m.Mandril == model.Mandrel);
            if (!existeMandrel)
            {
                return Json(new { success = false, error = "El mandrel ingresado no existe en el catálogo." });
            }

            // Actualizamos campos
            registro.Mandrel = Mandrel;
            registro.Ndpiezas = Ndpiezas;
            registro.Turno = Turno;
            registro.NuMesa = NuMesa;

            // 👇 Acepta "HH:mm" o "HH:mm:ss"
            string[] formatos = { "HH:mm", "HH:mm:ss" };
            if (TimeOnly.TryParseExact(Hora, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var horaParsed))
            {
                registro.Hora = horaParsed;
            }
            else
            {
                return Json(new { success = false, error = "Formato de hora inválido" });
            }

            _context.SaveChanges();

            // Usuario actual
            var usuarioActual = User.Identity?.Name ?? "Sistema";

            // Construimos lista de cambios
            var cambios = new List<string>();

            if (piezasAntes != Ndpiezas)
                cambios.Add($"Piezas: {piezasAntes} → {Ndpiezas}");

            if (mandrelAntes != Mandrel)
                cambios.Add($"Mandrel: {mandrelAntes} → {Mandrel}");

            if (mesaAntes != NuMesa)
                cambios.Add($"Mesa: {mesaAntes} → {NuMesa}");

            if (turnoAntes != Turno)
                cambios.Add($"Turno: {turnoAntes} → {Turno}");

            if (horaAntes != registro.Hora)
                cambios.Add($"Hora: {horaAntes:HH:mm} → {registro.Hora:HH:mm}");

            // Solo registrar si hubo cambios
            if (cambios.Any())
            {
                string mensaje = $"Registro [Id={registro.Id} | Mandrel={mandrelAntes}] . " + string.Join("| ", cambios);
                _log.Registrar("Actualizar Registro", mensaje, "Info", categoria: "Producción");
            }

            return Json(new { success = true, message = "Registro actualizado correctamente ✅" });
        }
        catch (Exception ex)
        {
            _log.Registrar("Error en Update", ex.ToString(), "Error");
            return Json(new { success = false, error = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost]
    public IActionResult Delete(int Id)
    {
        try
        {
            var registro = _context.RegistrodePiezasEscaneadas.Find(Id);
            if (registro == null)
                return Json(new { success = false, error = "Registro no encontrado" });

            // Guardamos datos antes de eliminar
            var piezas = registro.Ndpiezas;
            var mandrel = registro.Mandrel;
            var mesa = registro.NuMesa;
            var turno = registro.Turno;
            var hora = registro.Hora;

            _context.RegistrodePiezasEscaneadas.Remove(registro);
            _context.SaveChanges();

            // Usuario actual
            var usuarioActual = User.Identity?.Name ?? "Sistema";

            // Mensaje detallado
            string mensaje = $"Registro [Id={Id}]. " +
                             $"Piezas: {piezas}, " +
                             $"Mandrel: {mandrel}, " +
                             $"Mesa: {mesa}, " +
                             $"Turno: {turno}, " +
                             $"Hora: {hora:HH:mm}.";

            // Registrar en logs (categoría Producción)
            _log.Registrar("Eliminar Registro", mensaje, "Warning", categoria: "Producción");

            return Json(new { success = true, message = "Registro eliminado correctamente ✅" });
        }
        catch (Exception ex)
        {
            _log.Registrar("Error en Delete", ex.ToString(), "Error", categoria: "Producción");
            return Json(new { success = false, error = ex.Message });
        }
    }

    //EDITAR MODAL DE USUARIO
    [Authorize(Roles = "Admin,Editor")]
    [HttpGet]
    public IActionResult EditarModal(int id)
    {
        var registro = _context.RegistrodePiezasEscaneadas.FirstOrDefault(p => p.Id == id);
        if (registro == null) return NotFound();

        return PartialView("_EditarProduccionModal", registro);
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
        var mandriles = _context.Mandrels
            .Where(m => m.Mandril.Contains(term))
            .Select(m => m.Mandril)
            .Take(10)
            .ToList();

        return Json(mandriles);
    }

    //METODO PARA BUSCAR LOS MANDRILES EN LA TABLA DE MANDRILES DE LA COL MANDRIL
    [HttpGet]
    public IActionResult BuscarMandrels(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return Json(new List<string>());

        var mandriles = _context.Mandrels
            .Where(m => m.Mandril.Contains(term))
            .Select(m => m.Mandril)
            .Take(10) // limitar resultados
            .ToList();

        return Json(mandriles);
    }

    //REPORTE DE PRODUCCION GENERAL
    public IActionResult ReporteProduccion(DateOnly? fecha, string? turno, string? mesa)
    {
        if (!fecha.HasValue)
            fecha = DateOnly.FromDateTime(DateTime.Today);

        var fechaFiltro = fecha.Value;

        // Traer rango en SQL y luego aplicar helper en memoria
        var registros = _context.RegistrodePiezasEscaneadas
            .Where(r => r.Fecha >= fechaFiltro.AddDays(-1) && r.Fecha <= fechaFiltro.AddDays(1))
            .ToList() // materializamos
            .Where(r => ProduccionHelper.GetFechaProduccion(r.Fecha.ToDateTime(r.Hora)).Date
                        == fechaFiltro.ToDateTime(TimeOnly.MinValue).Date);

        if (!string.IsNullOrWhiteSpace(turno))
            registros = registros.Where(r => r.Turno.Trim().ToLower() == turno.Trim().ToLower());

        if (!string.IsNullOrWhiteSpace(mesa))
            registros = registros.Where(r => r.NuMesa.Trim().ToLower() == mesa.Trim().ToLower());

        var defectos = _context.RegistrodeDefectos
            .Where(d => d.Fecha >= fechaFiltro.AddDays(-1) && d.Fecha <= fechaFiltro.AddDays(1))
            .ToList()
            .Where(d => ProduccionHelper.GetFechaProduccion(d.Fecha.ToDateTime(d.Hora)).Date
                        == fechaFiltro.ToDateTime(TimeOnly.MinValue).Date)
            .Where(d => string.IsNullOrWhiteSpace(turno) || d.Turno.Trim().Equals(turno.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(d => string.IsNullOrWhiteSpace(mesa) || d.NuMesa.Trim().Equals(mesa.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(d => !string.IsNullOrEmpty(d.Defecto) && !string.IsNullOrEmpty(d.Mandrel))
            .ToList();

        // Mesas disponibles (solo las que tienen producción o defectos en la fecha/turno)
        var mesasDisponibles = registros
            .Select(r => r.NuMesa.Trim())
            .Union(defectos.Select(d => d.NuMesa.Trim()))
            .Distinct()
            .AsEnumerable()
            .OrderBy(m =>
            {
                var parts = m.Split('#');
                if (parts.Length > 1 && int.TryParse(parts[1], out var num))
                    return num;
                return int.MaxValue;
            })
            .ToList();


        ViewBag.Mesas = mesasDisponibles;


        // --- Producción agrupada por Mesa+Turno ---
        var datosProduccion = registros
            .ToList()
            .Where(r => !string.IsNullOrEmpty(r.Ndpiezas) && !string.IsNullOrEmpty(r.Mandrel))
            .Select(r => new
            {
                Mandrel = r.Mandrel.Trim(),
                Mesa = r.NuMesa.Trim(),
                Turno = r.Turno.Trim(),
                Tm = r.Tm?.Trim(),
                Cantidad = int.TryParse(r.Ndpiezas, out var c) ? c : 0
            })
            .GroupBy(r => new { r.Mesa, r.Turno })
            .Select(g => new ProduccionPorMesaViewModel
            {
                NuMesa = g.Key.Mesa,
                Turno = g.Key.Turno,
                TotalPiezas = g.Sum(x => x.Cantidad),

                Mandriles = g.GroupBy(x => x.Mandrel)
                    .Select(mg => new ProduccionPorMandrel
                    {
                        Mandrel = mg.Key,
                        TotalPiezas = mg.Sum(x => x.Cantidad)
                    }).ToList(),

                TeamMembers = g
                    .Where(x => !string.IsNullOrWhiteSpace(x.Tm))
                    .Select(x => x.Tm!)
                    .Union(
                        defectos
                            .Where(d => d.NuMesa.Trim().Equals(g.Key.Mesa, StringComparison.OrdinalIgnoreCase)
                                     && d.Turno.Trim().Equals(g.Key.Turno, StringComparison.OrdinalIgnoreCase)
                                     && !string.IsNullOrEmpty(d.Tm))
                            .Select(d => d.Tm.Trim()),
                        StringComparer.OrdinalIgnoreCase
                    )
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),

                TeamMembersProduccion = g
                    .Where(x => !string.IsNullOrWhiteSpace(x.Tm))
                    .GroupBy(x => x.Tm!)
                    .Select(tmGroup => new ProduccionPorTMViewModel
                    {
                        TM = tmGroup.Key,
                        TotalPiezas = tmGroup.Sum(x => x.Cantidad),
                        Mandriles = tmGroup.GroupBy(x => x.Mandrel)
                            .Select(mg => new ProduccionPorMandrel
                            {
                                Mandrel = mg.Key,
                                TotalPiezas = mg.Sum(x => x.Cantidad)
                            }).ToList()
                    }).ToList(),

                DefectosPorTM = defectos
                    .Where(d => d.NuMesa.Trim() == g.Key.Mesa && d.Turno.Trim() == g.Key.Turno && !string.IsNullOrEmpty(d.Tm))
                    .GroupBy(d => d.Tm.Trim())
                    .Select(grp => new DefectoPorTMViewModel
                    {
                        TM = grp.Key,
                        TotalDefectos = grp.Count()
                    }).ToList(),

                DefectosPorMandrel = defectos
                    .Where(d => d.NuMesa.Trim() == g.Key.Mesa && d.Turno.Trim() == g.Key.Turno && !string.IsNullOrEmpty(d.Mandrel))
                    .GroupBy(d => d.Mandrel.Trim())
                    .Select(grp => new DefectoPorMandrelViewModel
                    {
                        Mandrel = grp.Key,
                        TotalDefectos = grp.Count()
                    }).ToList(),

                DefectosPorDefecto = defectos
                    .Where(d => d.NuMesa.Trim() == g.Key.Mesa && d.Turno.Trim() == g.Key.Turno)
                    .GroupBy(d => d.Defecto.Trim())
                    .Select(grp => (Defecto: grp.Key, Total: grp.Count()))
                    .ToList(),

                TotalDefectos = defectos
                    .Count(d => d.NuMesa.Trim().ToLower() == g.Key.Mesa.Trim().ToLower()
                             && d.Turno.Trim().ToLower() == g.Key.Turno.Trim().ToLower())
            })
            .ToList();

        // --- Agregar mesas+turnos que solo tengan defectos ---
        var mesasTurnosDefectos = defectos
            .Select(d => new { Mesa = d.NuMesa.Trim(), Turno = d.Turno.Trim() })
            .Distinct()
            .ToList();

        foreach (var mt in mesasTurnosDefectos)
        {
            bool yaExiste = datosProduccion.Any(x =>
                x.NuMesa.Equals(mt.Mesa, StringComparison.OrdinalIgnoreCase) &&
                x.Turno.Equals(mt.Turno, StringComparison.OrdinalIgnoreCase));

            if (!yaExiste)
            {
                var defectosMesa = defectos
                    .Where(d => d.NuMesa.Trim() == mt.Mesa && d.Turno.Trim() == mt.Turno)
                    .ToList();

                datosProduccion.Add(new ProduccionPorMesaViewModel
                {
                    NuMesa = mt.Mesa,
                    Turno = mt.Turno,
                    TotalPiezas = 0,
                    Mandriles = new List<ProduccionPorMandrel>(),
                    TeamMembers = defectosMesa
                        .Where(d => !string.IsNullOrEmpty(d.Tm))
                        .Select(d => d.Tm.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList() ?? new List<string>(),
                    TeamMembersProduccion = new List<ProduccionPorTMViewModel>(),
                    DefectosPorTM = defectosMesa
                        .Where(d => !string.IsNullOrEmpty(d.Tm))
                        .GroupBy(d => d.Tm.Trim())
                        .Select(grp => new DefectoPorTMViewModel
                        {
                            TM = grp.Key,
                            TotalDefectos = grp.Count()
                        }).ToList(),
                    DefectosPorMandrel = defectosMesa
                        .Where(d => !string.IsNullOrEmpty(d.Mandrel))
                        .GroupBy(d => d.Mandrel.Trim())
                        .Select(grp => new DefectoPorMandrelViewModel
                        {
                            Mandrel = grp.Key,
                            TotalDefectos = grp.Count()
                        }).ToList(),
                    DefectosPorDefecto = defectosMesa
                        .GroupBy(d => d.Defecto.Trim())
                        .Select(grp => (Defecto: grp.Key, Total: grp.Count()))
                        .ToList(),
                    TotalDefectos = defectosMesa.Count
                });
            }
        }

        var datos = datosProduccion
            .OrderBy(d =>
            {
                var digits = new string(d.NuMesa.Where(char.IsDigit).ToArray());
                return int.TryParse(digits, out int num) ? num : int.MaxValue;
            })
            .ThenBy(d => int.TryParse(d.Turno, out var t) ? t : int.MaxValue)
            .ToList();

        var filtro = new ProduccionFiltroViewModel
        {
            Dia = fecha?.Day,
            Mes = fecha?.Month,
            Año = fecha?.Year,
            Turno = turno,
            Mesa = mesa,
            Reporte = datos
        };

        return View(filtro);
    }

    //PRODUCCION POR USUARIO - FECHA
    public IActionResult DetallePorUsuario(string fecha, string usuario)
    {
        if (string.IsNullOrWhiteSpace(fecha) || string.IsNullOrWhiteSpace(usuario))
        {
            TempData["Error"] = "Datos inválidos";
            return RedirectToAction("Dashboard");
        }

        if (!DateTime.TryParse(fecha, out DateTime parsedDateTime))
        {
            TempData["Error"] = "Fecha inválida";
            return RedirectToAction("Dashboard");
        }

        var fechaFiltro = DateOnly.FromDateTime(parsedDateTime);


        // Traemos un rango de +/-1 día en SQL
        var registros = _context.RegistrodePiezasEscaneadas
            .Where(r => r.Fecha >= fechaFiltro.AddDays(-1) && r.Fecha <= fechaFiltro.AddDays(1))
            .ToList() // 👈 materializamos para poder usar el helper
            .Where(r => ProduccionHelper.GetFechaProduccion(r.Fecha.ToDateTime(r.Hora)).Date
                        == fechaFiltro.ToDateTime(TimeOnly.MinValue).Date
                     && r.Tm != null
                     && r.Tm.Trim().Equals(usuario.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.Fecha)
            .ThenBy(r => r.Hora)
           .Select(r => new ProduccionDetalleViewModel
           {
               Id = r.Id,
               TM = r.Tm,
               FechaReal = r.Fecha,
               Hora = r.Hora,
               Mandrel = r.Mandrel,
               NumeroDePiezas = r.Ndpiezas,
               NuMesa = r.NuMesa,
               Turno = r.Turno,
               FechaLaboral = ProduccionHelper.GetFechaProduccion(r.Fecha.ToDateTime(r.Hora))
           })
            .ToList(); 

        ViewBag.FechaLaboral = fechaFiltro.ToString("yyyy-MM-dd");
      
        int totalPiezas = registros.Sum(d =>
        int.TryParse(d.NumeroDePiezas, out int cantidad) ? cantidad : 0);

        ViewBag.TotalPiezas = totalPiezas;
        ViewBag.Usuario = usuario;
        ViewBag.Fecha = fechaFiltro.ToString("yyyy-MM-dd");
        ViewBag.Turno = registros.FirstOrDefault()?.Turno ?? "Desconocido";

        return View("DetallePorUsuario", registros);
    }

    //PRODUCCION POR MESA - TURNO - FECHA
    public IActionResult DetalleProduccionMesa(string fecha, string mesa, string? turno)
    {
        if (!DateOnly.TryParse(fecha, out var fechaParseada) || string.IsNullOrWhiteSpace(mesa))
        {
            return BadRequest("Parámetros inválidos.");
        }

        // Traemos un rango de +/-1 día en SQL para no cargar toda la tabla
        var registros = _context.RegistrodePiezasEscaneadas
            .Where(r => r.Fecha >= fechaParseada.AddDays(-1) && r.Fecha <= fechaParseada.AddDays(1)
                     && r.NuMesa != null && r.NuMesa.Trim() == mesa.Trim())
            .ToList() // 👈 materializamos para poder usar el helper
            .Where(r => ProduccionHelper.GetFechaProduccion(r.Fecha.ToDateTime(r.Hora)).Date
                        == fechaParseada.ToDateTime(TimeOnly.MinValue).Date);

        // Filtro por turno (si se especifica)
        if (!string.IsNullOrWhiteSpace(turno))
        {
            registros = registros
                .Where(r => r.Turno != null && r.Turno.Trim().Equals(turno.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        var detalles = registros
            .OrderBy(r => r.Fecha) // primero por fecha real
            .ThenBy(r => r.Hora)   // luego por hora
            .Select(r => new ProduccionDetalleViewModel
            {
                Fecha = r.Fecha, // fecha real del registro
                Hora = r.Hora,
                Mandrel = r.Mandrel,
                NumeroDePiezas = r.Ndpiezas,
                NuMesa = r.NuMesa,
                Turno = r.Turno,
                TM = r.Tm,
                // 👇 puedes agregar una propiedad en tu ViewModel si quieres mostrar la fecha laboral
                FechaLaboral = ProduccionHelper.GetFechaProduccion(r.Fecha.ToDateTime(r.Hora))
            })
            .ToList();

        int totalPiezas = detalles.Sum(d =>
            int.TryParse(d.NumeroDePiezas, out int cantidad) ? cantidad : 0);

        ViewBag.Mesa = mesa;
        ViewBag.FechaLaboral = fechaParseada.ToString("yyyy-MM-dd");
        ViewBag.FechaRealMin = detalles.Any() ? detalles.Min(d => d.Fecha).ToString("yyyy-MM-dd") : fechaParseada.ToString("yyyy-MM-dd");
        ViewBag.FechaRealMax = detalles.Any() ? detalles.Max(d => d.Fecha).ToString("yyyy-MM-dd") : fechaParseada.ToString("yyyy-MM-dd");
        ViewBag.Turno = turno ?? "Todos";
        ViewBag.TotalPiezas = totalPiezas;

        return View("DetallePorMesa", detalles);
    }

    //DASHBOARD DINAMICO
    public IActionResult Dashboard(DateTime? fecha)
    {
        // Zona horaria de Matamoros
        var zona = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
        // Si no se seleccionó fecha, usar la fecha local de Matamoros
        var fechaSeleccionada = fecha ?? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);
        var fechaFiltro = DateOnly.FromDateTime(fechaSeleccionada);

        // --- Producciones ---
        var producciones = _context.RegistrodePiezasEscaneadas
            .Where(r => r.Fecha >= fechaFiltro.AddDays(-1) && r.Fecha <= fechaFiltro.AddDays(1))
            .ToList()
            .Where(r => ProduccionHelper.GetFechaProduccion(r.Fecha.ToDateTime(r.Hora)).Date
                        == fechaFiltro.ToDateTime(TimeOnly.MinValue).Date)
            .ToList();

        // --- Defectos ---
        var defectos = _context.RegistrodeDefectos
            .Where(d => d.Fecha >= fechaFiltro.AddDays(-1) && d.Fecha <= fechaFiltro.AddDays(1))
            .ToList()
            .Where(d => ProduccionHelper.GetFechaProduccion(d.Fecha.ToDateTime(d.Hora)).Date
                        == fechaFiltro.ToDateTime(TimeOnly.MinValue).Date)
            .ToList();

        // --- Totales ---
        var totalBuenas = producciones.Sum(r => int.TryParse(r.Ndpiezas, out var n) ? n : 0);
        var totalDefectos = defectos.Count;
        var totalPiezas = totalBuenas + totalDefectos;

        // --- FPY y Scrap ---
        double fpy = totalPiezas > 0 ? (double)totalBuenas / totalPiezas * 100 : 0;
        double scrap = totalPiezas > 0 ? (double)totalDefectos / totalPiezas * 100 : 0;

        // --- Defectos por categoría (excluyendo los códigos indicados) ---
        int defectosPrintIllegible = defectos.Count(d => !new[] { "17a", "17b", "21" }.Contains(d.CodigodeDefecto));
        int defectosMaterialLub = defectos.Count(d => !new[] { "17a", "17b", "21", "54" }.Contains(d.CodigodeDefecto));
        int defectosVulcanization = defectos.Count(d => !new[] { "17a", "17b", "21", "54", "59", "46", "24" }.Contains(d.CodigodeDefecto));
        int defectosUncured = defectos.Count(d => !new[] { "17a", "17b", "21", "54", "59", "46", "24", "23" }.Contains(d.CodigodeDefecto));

        // --- Porcentajes (respecto al total de defectos) ---
        double porcPrintIllegible = totalDefectos > 0 ? (double)defectosPrintIllegible / totalPiezas * 100 : 0;
        double porcMaterialLub = totalDefectos > 0 ? (double)defectosMaterialLub / totalPiezas * 100 : 0;
        double porcVulcanization = totalDefectos > 0 ? (double)defectosVulcanization / totalPiezas * 100 : 0;
        double porcUncured = totalDefectos > 0 ? (double)defectosUncured / totalPiezas * 100 : 0;




        var viewModel = new DashboardResumenViewModel
        {
            TotalPiezas = totalPiezas,
            TotalDefectos = totalDefectos,
            FPY = fpy,
            Scrap = scrap,
            PorcentajePrintIllegible = porcPrintIllegible,
            PorcentajeMaterialLub = porcMaterialLub,
            PorcentajeVulcanization = porcVulcanization,
            PorcentajeUncured = porcUncured
        };


        // --- Producción por turno (para Chart.js) ---
        var produccionPorTurno = producciones
            .GroupBy(r => r.Turno?.Trim())
            .Select(g => new
            {
                Turno = g.Key,
                Total = g.Sum(r => int.TryParse(r.Ndpiezas, out var n) ? n : 0)
            })
            .OrderBy(g => int.TryParse(g.Turno, out var turnoNum) ? turnoNum : int.MaxValue)
            .ToList();

        // --- Producción por mesa y turno ---
        var produccionPorMesaYTurno = producciones
            .Where(r => !string.IsNullOrWhiteSpace(r.NuMesa) && !string.IsNullOrWhiteSpace(r.Turno))
            .GroupBy(r => r.NuMesa.Trim())
            .OrderBy(g =>
            {
                var digits = new string(g.Key.Where(char.IsDigit).ToArray());
                return int.TryParse(digits, out int n) ? n : int.MaxValue;
            })
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(r => r.Turno.Trim())
                      .ToDictionary(
                          t => t.Key,
                          t => t.Sum(r => int.TryParse(r.Ndpiezas, out var n) ? n : 0)
                      )
            );

        // --- Producción por TM y turno ---
        var produccionPorTMYTurno = producciones
            .Where(r => !string.IsNullOrWhiteSpace(r.Tm) && !string.IsNullOrWhiteSpace(r.Turno))
            .GroupBy(r => r.Tm.Trim())
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(r => r.Turno.Trim())
                      .ToDictionary(
                          t => t.Key,
                          t => t.Sum(r => int.TryParse(r.Ndpiezas, out var n) ? n : 0)
                      )
            );

        // --- Preparar datos para Chart.js ---
        ViewBag.MesaLabels = produccionPorMesaYTurno.Keys.ToList();
        ViewBag.Turno1PorMesa = produccionPorMesaYTurno.Values.Select(m => m.ContainsKey("1") ? m["1"] : 0).ToList();
        ViewBag.Turno2PorMesa = produccionPorMesaYTurno.Values.Select(m => m.ContainsKey("2") ? m["2"] : 0).ToList();
        ViewBag.Turno3PorMesa = produccionPorMesaYTurno.Values.Select(m => m.ContainsKey("3") ? m["3"] : 0).ToList();

        ViewBag.TMLabels = produccionPorTMYTurno.Keys.ToList();
        ViewBag.Turno1PorTM = produccionPorTMYTurno.Values.Select(m => m.ContainsKey("1") ? m["1"] : 0).ToList();
        ViewBag.Turno2PorTM = produccionPorTMYTurno.Values.Select(m => m.ContainsKey("2") ? m["2"] : 0).ToList();
        ViewBag.Turno3PorTM = produccionPorTMYTurno.Values.Select(m => m.ContainsKey("3") ? m["3"] : 0).ToList();

        ViewBag.ProduccionTurnoLabels = produccionPorTurno.Select(x => $"Turno {x.Turno}").ToList();
        ViewBag.ProduccionTurnoData = produccionPorTurno.Select(x => x.Total).ToList();

        ViewBag.FechaSeleccionada = fechaSeleccionada.ToString("yyyy-MM-dd");
        //ViewBag.FechaTitulo = fechaSeleccionada.ToString("dd MMMM yyyy");

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> GetProduccion([FromForm] DataTablesRequest request, string fecha, string turno)
    {
        var baseQuery = _context.RegistrodePiezasEscaneadas.AsQueryable();

        List<RegistrodePiezasEscaneada> registros;

        if (!string.IsNullOrEmpty(fecha) && DateOnly.TryParse(fecha, out var fechaFiltro))
        {
            // Traemos un rango de +/-1 día en SQL (solo con DateOnly)
            var fechaInicio = fechaFiltro.AddDays(-1);
            var fechaFin = fechaFiltro.AddDays(1);

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

        // Convertimos a IQueryable para que DataTables pueda seguir aplicando filtros
        var query = registros.AsQueryable();

        // Filtro por turno
        if (!string.IsNullOrEmpty(turno))
        {
            query = query.Where(x => x.Turno == turno);
        }

        // Filtro global busqueda
        if (!string.IsNullOrEmpty(request.Search?.Value))
        {
            var search = request.Search.Value.ToLower();
            query = query.Where(x =>
                x.Mandrel.ToLower().Contains(search) ||
                x.Tm.ToLower().Contains(search) ||
                x.NuMesa.ToLower().Contains(search));
        }

        var totalRecords = await _context.RegistrodePiezasEscaneadas.CountAsync();
        var filteredRecords = query.Count();

        // Orden dinámico
        if (request.Order != null && request.Order.Any())
        {
            var order = request.Order.First();
            var columnName = request.Columns[order.Column].Data;
            var direction = order.Dir;
            query = query.OrderBy($"{columnName} {direction}");
        }

        // Paginación
        var data = query
            .Skip(request.Start)
            .Take(request.Length)
            .ToList();

        return Json(new
        {
            draw = request.Draw,
            recordsTotal = totalRecords,
            recordsFiltered = filteredRecords,
            data = data
        });
    }

}

