using Azure;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProducScan.Helpers;
using ProducScan.Models;
using ProducScan.Services;
using ProducScan.ViewModels;
using ProducScan.ViewModels.Dashboard;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq.Dynamic.Core;

[Authorize]
public class PiezasEscaneadasController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogService _log;
    private readonly IHubContext<ProduccionHub> _hubContext;


    public PiezasEscaneadasController(AppDbContext context, ILogService log, IHubContext<ProduccionHub> hubContext)
    {
        _context = context;
        _log = log;
        _hubContext = hubContext;

    }

    public async Task<IActionResult> RegistrarPieza(RegistrodePiezasEscaneada registro)
    {
        _context.RegistrodePiezasEscaneadas.Add(registro);
        await _context.SaveChangesAsync();

        // Construyes el ViewModel actualizado
        var vm = new InspeccionTMViewModel
        {
            TM = registro.Tm,
            Mesa = registro.NuMesa,
            TotalPiezas = int.Parse(registro.Ndpiezas),
            Mandril = registro.Mandrel,
            Meta = 1800
        };

        // 🔥 Notificas a todos los clientes conectados
        await _hubContext.Clients.All.SendAsync("RecibirActualizacion", vm);

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        int pageSize = 10;

        var Escaneadas = _context.RegistrodePiezasEscaneadas.AsQueryable();

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

        // --- Fecha laboral (usando ProduccionHelper) ---
        var fechaLaboral = DateOnly.FromDateTime(ProduccionHelper.GetFechaProduccion(ahora));

        // ✅ Pasar turno y fecha laboral a la vista
        ViewBag.TurnoSeleccionado = turnoSeleccionado;
        ViewBag.FechaSeleccionada = fechaLaboral.ToString("yyyy-MM-dd");

        var paginated = await PaginatedList<RegistrodePiezasEscaneada>.CreateAsync(Escaneadas, page, pageSize);

        return View(paginated);
    }

    [Authorize(Roles = "Admin,Editor,Visual")]
    [HttpGet]
    public IActionResult InspeccionTM(DateTime? fecha, string turno)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
        var ahora = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);

        var fechaSeleccionada = fecha ?? ProduccionHelper.GetFechaProduccion(ahora);
        var fechaFiltro = DateOnly.FromDateTime(fechaSeleccionada);

        string turnoSeleccionado = turno;
        if (string.IsNullOrEmpty(turnoSeleccionado))
        {
            var horaActual = ahora.TimeOfDay;
            if (horaActual >= new TimeSpan(7, 0, 0) && horaActual <= new TimeSpan(15, 44, 59))
                turnoSeleccionado = "1";
            else if (horaActual >= new TimeSpan(15, 45, 0) && horaActual <= new TimeSpan(23, 49, 59))
                turnoSeleccionado = "2";
            else
                turnoSeleccionado = "3";
        }

        var usuarios = _context.Users.ToList();
        var mesas = _context.Mesas.ToList();

        // --- Producciones ---
        var produccionesRaw = _context.RegistrodePiezasEscaneadas
            .Where(r => r.Fecha >= fechaFiltro.AddDays(-1) && r.Fecha <= fechaFiltro.AddDays(1))
            .ToList()
            .Where(r => ProduccionHelper.GetFechaProduccion(r.Fecha.ToDateTime(r.Hora)).Date
                        == fechaFiltro.ToDateTime(TimeOnly.MinValue).Date
                        && r.Turno == turnoSeleccionado)
            .ToList();

        var producciones = produccionesRaw
            .GroupBy(r => new { r.NuMesa, r.Tm })
            .Select(g => new
            {
                Mesa = g.Key.NuMesa,
                TM = g.Key.Tm,
                PiezasBuenas = g.Sum(x => int.TryParse(x.Ndpiezas, out var n) ? n : 0)
            })
            .ToList();

        // --- Defectos ---
        var defectosRaw = _context.RegistrodeDefectos
            .Where(d => d.Fecha >= fechaFiltro.AddDays(-1) && d.Fecha <= fechaFiltro.AddDays(1))
            .ToList()
            .Where(d => ProduccionHelper.GetFechaProduccion(d.Fecha.ToDateTime(d.Hora)).Date
                        == fechaFiltro.ToDateTime(TimeOnly.MinValue).Date
                        && d.Turno == turnoSeleccionado)
            .ToList();

        var defectos = defectosRaw
            .GroupBy(d => new { d.NuMesa, d.Tm })
            .Select(g => new
            {
                Mesa = g.Key.NuMesa,
                TM = g.Key.Tm,
                PiezasMalas = g.Count()
            })
            .ToList();

        // --- Unión de claves (Mesa + TM) ---
        var union = producciones.Select(p => new { p.Mesa, p.TM })
            .Union(defectos.Select(d => new { d.Mesa, d.TM }))
            .ToList();

        var lista = union.Select(u =>
        {
            var prod = producciones.FirstOrDefault(p => p.Mesa == u.Mesa && p.TM == u.TM);
            var def = defectos.FirstOrDefault(d => d.Mesa == u.Mesa && d.TM == u.TM);

            int piezasBuenas = prod?.PiezasBuenas ?? 0;
            int piezasMalas = def?.PiezasMalas ?? 0;
            int total = piezasBuenas + piezasMalas;

            // 👇 Extraer número de "MESA#3" → 3
            string digits = new string(u.Mesa.Where(char.IsDigit).ToArray());
            int numeroMesa = int.TryParse(digits, out var num) ? num : 0;

            // 👇 Buscar la meta usando IdMesa o NumerodeMesa
            var mesaInfo = mesas.FirstOrDefault(m => m.IdMesa == numeroMesa
                                                  || m.NumerodeMesa == numeroMesa.ToString());

            int metaMesa = mesaInfo?.Meta ?? 1800;

            // --- Calcular meta proporcional ---
            int duracionTurnoMin = 8 * 60;
            TimeSpan inicioTurno = turnoSeleccionado == "1" ? new TimeSpan(7, 0, 0) :
                                   turnoSeleccionado == "2" ? new TimeSpan(15, 45, 0) :
                                   new TimeSpan(23, 50, 0);

            var minutosTranscurridos = (ahora.TimeOfDay - inicioTurno).TotalMinutes;
            if (minutosTranscurridos < 0) minutosTranscurridos = 0;
            if (minutosTranscurridos > duracionTurnoMin) minutosTranscurridos = duracionTurnoMin;

            int metaEsperada = (int)((metaMesa / (double)duracionTurnoMin) * minutosTranscurridos);

            // --- Estado y color ---
            string estado;
            string colorClass;
            if (total >= metaEsperada + 100)
            {
                estado = "Sobreproducción";
                colorClass = "bg-danger text-white";
            }
            else if (total >= metaEsperada)
            {
                estado = "En meta";
                colorClass = "bg-green-500 text-white";
            }
            else if (total >= metaEsperada - 100)
            {
                estado = "Cerca de la meta";
                colorClass = "bg-yellow-400 text-black";
            }
            else
            {
                estado = "Fuera de meta";
                colorClass = "bg-red-300 text-black";
            }

            var usuario = usuarios.FirstOrDefault(x => x.Nombre == u.TM);
            string numeroEmpleadoBD = usuario?.NumerodeEmpleado ?? "0000";
            string numeroEmpleadoFoto = int.Parse(numeroEmpleadoBD).ToString();

            string fotoPathJPG = Path.Combine("wwwroot/images/tm", $"{numeroEmpleadoFoto}.JPG");
            string fotoPathjpg = Path.Combine("wwwroot/images/tm", $"{numeroEmpleadoFoto}.jpg");

            string fotoUrl;
            if (System.IO.File.Exists(fotoPathJPG))
                fotoUrl = $"/images/tm/{numeroEmpleadoFoto}.JPG";
            else if (System.IO.File.Exists(fotoPathjpg))
                fotoUrl = $"/images/tm/{numeroEmpleadoFoto}.jpg";
            else
                fotoUrl = "/images/tm/thumbnail.png";

            // 👇 Obtener el último mandril registrado para esa mesa/TM
            var ultimoMandril = produccionesRaw
                .Where(r => r.NuMesa == u.Mesa && r.Tm == u.TM)
                .OrderByDescending(r => r.Fecha.ToDateTime(r.Hora))
                .Select(r => r.Mandrel) // asegúrate que tu entidad tiene este campo
                .FirstOrDefault() ?? "N/A";

            return new InspeccionTMViewModel
            {
                TM = u.TM,
                Mesa = u.Mesa,
                NumeroEmpleado = numeroEmpleadoBD,
                FotoUrl = fotoUrl,
                PiezasBuenas = piezasBuenas,
                PiezasMalas = piezasMalas,
                TotalPiezas = total,
                Meta = metaMesa,
                MetaEsperada = metaEsperada,
                Estado = estado,
                ColorClass = colorClass,
                Mandril = ultimoMandril
            };
        })
        .OrderBy(m =>
        {
            var digits = new string(m.Mesa.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out int num) ? num : int.MaxValue;
        })
        .ToList();

        ViewBag.Año = fechaFiltro.Year;
        ViewBag.Mes = fechaFiltro.Month;
        ViewBag.Dia = fechaFiltro.Day;
        ViewBag.Turno = turnoSeleccionado;

        ViewBag.FechaSeleccionada = fechaFiltro.ToString("yyyy-MM-dd");
        ViewBag.TurnoSeleccionado = turnoSeleccionado;

        return View(lista);
    }

    [HttpGet]
    public IActionResult InspeccionTMTV(DateTime? fecha, string turno)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
        var ahora = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);

        var fechaSeleccionada = fecha ?? ProduccionHelper.GetFechaProduccion(ahora);
        var fechaFiltro = DateOnly.FromDateTime(fechaSeleccionada);

        string turnoSeleccionado = turno;
        if (string.IsNullOrEmpty(turnoSeleccionado))
        {
            var horaActual = ahora.TimeOfDay;
            if (horaActual >= new TimeSpan(7, 0, 0) && horaActual <= new TimeSpan(15, 44, 59))
                turnoSeleccionado = "1";
            else if (horaActual >= new TimeSpan(15, 45, 0) && horaActual <= new TimeSpan(23, 49, 59))
                turnoSeleccionado = "2";
            else
                turnoSeleccionado = "3";
        }

        var usuarios = _context.Users.ToList();
        var mesas = _context.Mesas.ToList();

        // --- Producciones ---
        var produccionesRaw = _context.RegistrodePiezasEscaneadas
            .Where(r => r.Fecha >= fechaFiltro.AddDays(-1) && r.Fecha <= fechaFiltro.AddDays(1))
            .ToList()
            .Where(r => ProduccionHelper.GetFechaProduccion(r.Fecha.ToDateTime(r.Hora)).Date
                        == fechaFiltro.ToDateTime(TimeOnly.MinValue).Date
                        && r.Turno == turnoSeleccionado)
            .ToList();

        var producciones = produccionesRaw
            .GroupBy(r => new { r.NuMesa, r.Tm })
            .Select(g => new
            {
                Mesa = g.Key.NuMesa,
                TM = g.Key.Tm,
                PiezasBuenas = g.Sum(x => int.TryParse(x.Ndpiezas, out var n) ? n : 0)
            })
            .ToList();

        // --- Defectos ---
        var defectosRaw = _context.RegistrodeDefectos
            .Where(d => d.Fecha >= fechaFiltro.AddDays(-1) && d.Fecha <= fechaFiltro.AddDays(1))
            .ToList()
            .Where(d => ProduccionHelper.GetFechaProduccion(d.Fecha.ToDateTime(d.Hora)).Date
                        == fechaFiltro.ToDateTime(TimeOnly.MinValue).Date
                        && d.Turno == turnoSeleccionado)
            .ToList();

        var defectos = defectosRaw
            .GroupBy(d => new { d.NuMesa, d.Tm })
            .Select(g => new
            {
                Mesa = g.Key.NuMesa,
                TM = g.Key.Tm,
                PiezasMalas = g.Count()
            })
            .ToList();

        // --- Unión de claves (Mesa + TM) ---
        var union = producciones.Select(p => new { p.Mesa, p.TM })
            .Union(defectos.Select(d => new { d.Mesa, d.TM }))
            .ToList();

        var lista = union.Select(u =>
        {
            var prod = producciones.FirstOrDefault(p => p.Mesa == u.Mesa && p.TM == u.TM);
            var def = defectos.FirstOrDefault(d => d.Mesa == u.Mesa && d.TM == u.TM);

            int piezasBuenas = prod?.PiezasBuenas ?? 0;
            int piezasMalas = def?.PiezasMalas ?? 0;
            int total = piezasBuenas + piezasMalas;

            // 👇 Extraer número de "MESA#3" → 3
            string digits = new string(u.Mesa.Where(char.IsDigit).ToArray());
            int numeroMesa = int.TryParse(digits, out var num) ? num : 0;

            // 👇 Buscar la meta usando IdMesa o NumerodeMesa
            var mesaInfo = mesas.FirstOrDefault(m => m.IdMesa == numeroMesa
                                                  || m.NumerodeMesa == numeroMesa.ToString());

            int metaMesa = mesaInfo?.Meta ?? 1800;

            // --- Calcular meta proporcional ---
            int duracionTurnoMin = 8 * 60;
            TimeSpan inicioTurno = turnoSeleccionado == "1" ? new TimeSpan(7, 0, 0) :
                                   turnoSeleccionado == "2" ? new TimeSpan(15, 45, 0) :
                                   new TimeSpan(23, 50, 0);

            var minutosTranscurridos = (ahora.TimeOfDay - inicioTurno).TotalMinutes;
            if (minutosTranscurridos < 0) minutosTranscurridos = 0;
            if (minutosTranscurridos > duracionTurnoMin) minutosTranscurridos = duracionTurnoMin;

            int metaEsperada = (int)((metaMesa / (double)duracionTurnoMin) * minutosTranscurridos);

            // --- Estado y color ---
            string estado;
            string colorClass;
            if (total >= metaEsperada + 100)
            {
                estado = "Sobreproducción";
                colorClass = "bg-danger text-white";
            }
            else if (total >= metaEsperada)
            {
                estado = "En meta";
                colorClass = "bg-green-500 text-white";
            }
            else if (total >= metaEsperada - 100)
            {
                estado = "Cerca de la meta";
                colorClass = "bg-yellow-400 text-black";
            }
            else
            {
                estado = "Fuera de meta";
                colorClass = "bg-red-300 text-black";
            }

            var usuario = usuarios.FirstOrDefault(x => x.Nombre == u.TM);
            string numeroEmpleadoBD = usuario?.NumerodeEmpleado ?? "0000";
            string numeroEmpleadoFoto = int.Parse(numeroEmpleadoBD).ToString();

            string fotoPathJPG = Path.Combine("wwwroot/images/tm", $"{numeroEmpleadoFoto}.JPG");
            string fotoPathjpg = Path.Combine("wwwroot/images/tm", $"{numeroEmpleadoFoto}.jpg");

            string fotoUrl;
            if (System.IO.File.Exists(fotoPathJPG))
                fotoUrl = $"/images/tm/{numeroEmpleadoFoto}.JPG";
            else if (System.IO.File.Exists(fotoPathjpg))
                fotoUrl = $"/images/tm/{numeroEmpleadoFoto}.jpg";
            else
                fotoUrl = "/images/tm/thumbnail.png";

            // 👇 Obtener el último mandril registrado para esa mesa/TM
            var ultimoMandril = produccionesRaw
                .Where(r => r.NuMesa == u.Mesa && r.Tm == u.TM)
                .OrderByDescending(r => r.Fecha.ToDateTime(r.Hora))
                .Select(r => r.Mandrel) // asegúrate que tu entidad tiene este campo
                .FirstOrDefault() ?? "N/A";

            return new InspeccionTMViewModel
            {
                TM = u.TM,
                Mesa = u.Mesa,
                NumeroEmpleado = numeroEmpleadoBD,
                FotoUrl = fotoUrl,
                PiezasBuenas = piezasBuenas,
                PiezasMalas = piezasMalas,
                TotalPiezas = total,
                Meta = metaMesa,
                MetaEsperada = metaEsperada,
                Estado = estado,
                ColorClass = colorClass,
                Mandril = ultimoMandril
            };
        })
        .OrderBy(m =>
        {
            var digits = new string(m.Mesa.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out int num) ? num : int.MaxValue;
        })
        .ToList();

        ViewBag.Año = fechaFiltro.Year;
        ViewBag.Mes = fechaFiltro.Month;
        ViewBag.Dia = fechaFiltro.Day;
        ViewBag.Turno = turnoSeleccionado;

        ViewBag.FechaSeleccionada = fechaFiltro.ToString("yyyy-MM-dd");
        ViewBag.TurnoSeleccionado = turnoSeleccionado;

        return View(lista);
    }
    public IActionResult ExportProduccionDiaLaboral(DateTime? fecha)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
        var ahora = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);

        var fechaSeleccionada = fecha ?? ahora;
        var fechaFiltro = DateOnly.FromDateTime(fechaSeleccionada);

        // --- Producciones agrupadas por Mesa + Mandril + Turno ---
        var producciones = _context.RegistrodePiezasEscaneadas
            .ToList()
            .Where(r => ProduccionHelper.GetFechaProduccion(r.Fecha.ToDateTime(r.Hora))
                        == fechaFiltro.ToDateTime(TimeOnly.MinValue))
            .GroupBy(r => new { r.NuMesa, r.Mandrel, r.Turno })
            .Select(g => new
            {
                Mesa = g.Key.NuMesa,
                Mandril = g.Key.Mandrel,
                Turno = g.Key.Turno,
                PiezasBuenas = g.Sum(x => int.TryParse(x.Ndpiezas, out var n) ? n : 0)
            })
            .ToList();

        // --- Defectos agrupados por Mesa + Mandril + Turno ---
        var defectos = _context.RegistrodeDefectos
            .ToList()
            .Where(d => ProduccionHelper.GetFechaProduccion(d.Fecha.ToDateTime(d.Hora))
                        == fechaFiltro.ToDateTime(TimeOnly.MinValue))
            .GroupBy(d => new { d.NuMesa, d.Mandrel, d.Turno })
            .Select(g => new
            {
                Mesa = g.Key.NuMesa,
                Mandril = g.Key.Mandrel,
                Turno = g.Key.Turno,
                PiezasMalas = g.Count()
            })
            .ToList();

        // --- Unión de claves (Mesa + Mandril + Turno) ---
        var union = producciones.Select(p => new { p.Mesa, p.Mandril, p.Turno })
            .Union(defectos.Select(d => new { d.Mesa, d.Mandril, d.Turno }))
            .ToList();

        // --- Pivot: filas = Mesa+Mandril, columnas = Turnos ---
        var lista = union
            .GroupBy(u => new { u.Mesa, u.Mandril })
            .Select(g =>
            {
                int turno1Buenas = producciones.FirstOrDefault(p => p.Mesa == g.Key.Mesa && p.Mandril == g.Key.Mandril && p.Turno == "1")?.PiezasBuenas ?? 0;
                int turno1Malas = defectos.FirstOrDefault(d => d.Mesa == g.Key.Mesa && d.Mandril == g.Key.Mandril && d.Turno == "1")?.PiezasMalas ?? 0;
                int turno1Total = turno1Buenas + turno1Malas;

                int turno2Buenas = producciones.FirstOrDefault(p => p.Mesa == g.Key.Mesa && p.Mandril == g.Key.Mandril && p.Turno == "2")?.PiezasBuenas ?? 0;
                int turno2Malas = defectos.FirstOrDefault(d => d.Mesa == g.Key.Mesa && d.Mandril == g.Key.Mandril && d.Turno == "2")?.PiezasMalas ?? 0;
                int turno2Total = turno2Buenas + turno2Malas;

                int turno3Buenas = producciones.FirstOrDefault(p => p.Mesa == g.Key.Mesa && p.Mandril == g.Key.Mandril && p.Turno == "3")?.PiezasBuenas ?? 0;
                int turno3Malas = defectos.FirstOrDefault(d => d.Mesa == g.Key.Mesa && d.Mandril == g.Key.Mandril && d.Turno == "3")?.PiezasMalas ?? 0;
                int turno3Total = turno3Buenas + turno3Malas;

                int totalProduccion = turno1Buenas + turno2Buenas + turno3Buenas;
                int totalDefectos = turno1Malas + turno2Malas + turno3Malas;
                int totalDia = totalProduccion + totalDefectos;

                return new
                {
                    Mesa = g.Key.Mesa,
                    Mandril = g.Key.Mandril,
                    Turno1 = turno1Total,
                    Turno2 = turno2Total,
                    Turno3 = turno3Total,
                    TotalProduccion = totalProduccion,
                    TotalDefectos = totalDefectos,
                    TotalDia = totalDia,
                    Turno1Produccion = turno1Buenas,
                    Turno1Defectos = turno1Malas,
                    Turno2Produccion = turno2Buenas,
                    Turno2Defectos = turno2Malas,
                    Turno3Produccion = turno3Buenas,
                    Turno3Defectos = turno3Malas
                };
            })
            .OrderBy(x =>
            {
                var digits = new string(x.Mesa.Where(char.IsDigit).ToArray());
                return int.TryParse(digits, out int num) ? num : int.MaxValue;
            })
            .ThenBy(x => x.Mandril)
            .ToList();

        // --- Generar Excel con bloques por mesa lado a lado ---
        using (var workbook = new XLWorkbook())
        {
            var ws = workbook.Worksheets.Add("Producción Día Laboral");

            // Agrupar por mesa para conocer tamaños de bloque
            var mesas = lista.GroupBy(x => x.Mesa)
                             .OrderBy(g =>
                             {
                                 var digits = new string(g.Key.Where(char.IsDigit).ToArray());
                                 return int.TryParse(digits, out int num) ? num : int.MaxValue;
                             })
                             .ToList();

            // Configuración de bloques
            int blocksPerRow = 4;       // hasta 4 mesas por fila
            int blockWidth = 8;         // columnas usadas: Mesa..Total Día (8 columnas)
            int blockGapCols = 2;       // columnas de separación entre bloques
            int blockHeaderHeight = 1;  // altura del encabezado
            int blockTotalsHeight = 1;  // fila TOTAL MESA
            int blockRowPadding = 2;    // filas de espacio entre filas de bloques (tu requerimiento)
            int startRow = 1;
            int startCol = 1;

            // Totales generales por turno (acumulados de todo el día)
            int totalTurno1Produccion = 0, totalTurno1Defectos = 0;
            int totalTurno2Produccion = 0, totalTurno2Defectos = 0;
            int totalTurno3Produccion = 0, totalTurno3Defectos = 0;

            int blockIndexInRow = 0;
            int currentRowMaxBlockHeight = 0;

            foreach (var mesaGroup in mesas)
            {
                // Colocar encabezado del bloque (siempre, incluyendo la primera mesa)
                int headerRow = startRow;
                int headerCol = startCol;

                // Encabezado por mesa
                ws.Cell(headerRow, headerCol + 0).Value = "Mesa";
                ws.Cell(headerRow, headerCol + 1).Value = "Mandril";
                ws.Cell(headerRow, headerCol + 2).Value = "Turno 1";
                ws.Cell(headerRow, headerCol + 3).Value = "Turno 2";
                ws.Cell(headerRow, headerCol + 4).Value = "Turno 3";
                ws.Cell(headerRow, headerCol + 5).Value = "Total Producción";
                ws.Cell(headerRow, headerCol + 6).Value = "Total Defectos";
                ws.Cell(headerRow, headerCol + 7).Value = "Total Día";



                var headerRange = ws.Range(headerRow, headerCol, headerRow, headerCol + 7);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.Underline = XLFontUnderlineValues.Single;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;


                // Escribir filas de mandriles
                int dataStartRow = headerRow + blockHeaderHeight;
                int r = dataStartRow;

                int sumaTurno1 = 0, sumaTurno2 = 0, sumaTurno3 = 0;
                int sumaProduccion = 0, sumaDefectos = 0, sumaTotalDia = 0;

                foreach (var item in mesaGroup.OrderBy(x => x.Mandril))
                {
                    // Filas de mandriles
                    ws.Cell(r, headerCol + 0).Value = item.Mesa;
                    ws.Cell(r, headerCol + 1).Value = item.Mandril;
                    ws.Cell(r, headerCol + 2).Value = item.Turno1;
                    ws.Cell(r, headerCol + 3).Value = item.Turno2;
                    ws.Cell(r, headerCol + 4).Value = item.Turno3;
                    ws.Cell(r, headerCol + 5).Value = item.TotalProduccion;
                    ws.Cell(r, headerCol + 6).Value = item.TotalDefectos;
                    ws.Cell(r, headerCol + 7).Value = item.TotalDia;

                    // aplicar bordes a la fila
                    var dataRange = ws.Range(r, headerCol, r, headerCol + 7);
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    // Acumulados por mesa
                    sumaTurno1 += item.Turno1;
                    sumaTurno2 += item.Turno2;
                    sumaTurno3 += item.Turno3;
                    sumaProduccion += item.TotalProduccion;
                    sumaDefectos += item.TotalDefectos;
                    sumaTotalDia += item.TotalDia;

                    // Acumulados generales por turno (producción/defectos)
                    totalTurno1Produccion += item.Turno1Produccion;
                    totalTurno1Defectos += item.Turno1Defectos;
                    totalTurno2Produccion += item.Turno2Produccion;
                    totalTurno2Defectos += item.Turno2Defectos;
                    totalTurno3Produccion += item.Turno3Produccion;
                    totalTurno3Defectos += item.Turno3Defectos;

                    r++;
                }

                // Fila TOTAL MESA
                ws.Cell(r, headerCol + 0).Value = mesaGroup.Key;
                ws.Cell(r, headerCol + 1).Value = "TOTAL MESA";
                ws.Cell(r, headerCol + 2).Value = sumaTurno1;
                ws.Cell(r, headerCol + 3).Value = sumaTurno2;
                ws.Cell(r, headerCol + 4).Value = sumaTurno3;
                ws.Cell(r, headerCol + 5).Value = sumaProduccion;
                ws.Cell(r, headerCol + 6).Value = sumaDefectos;
                ws.Cell(r, headerCol + 7).Value = sumaTotalDia;

                var totalRange = ws.Range(r, headerCol, r, headerCol + 7);
                totalRange.Style.Font.Bold = true;
                totalRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                totalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                totalRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;


                int blockHeight = (r - headerRow + 1); // header + data + total
                currentRowMaxBlockHeight = Math.Max(currentRowMaxBlockHeight, blockHeight);

                // Preparar siguiente bloque en esta fila (lado derecho)
                blockIndexInRow++;
                if (blockIndexInRow < blocksPerRow)
                {
                    startCol += (blockWidth + blockGapCols);
                }
                else
                {
                    // Pasar a la "siguiente fila de bloques"
                    startCol = 1;
                    startRow += currentRowMaxBlockHeight + blockRowPadding; // deja 2 líneas entre filas
                    blockIndexInRow = 0;
                    currentRowMaxBlockHeight = 0;
                }
            }

            // Bloque de Totales Generales por Turno (al final)
            // Lo colocamos en el siguiente espacio disponible de bloques.
            int tgHeaderRow = startRow;
            int tgHeaderCol = startCol;

            ws.Cell(tgHeaderRow, tgHeaderCol + 0).Value = "Totales generales por turno";
            ws.Range(tgHeaderRow, tgHeaderCol, tgHeaderRow, tgHeaderCol + 7).Merge();
            var tgHeaderRange = ws.Range(tgHeaderRow, tgHeaderCol, tgHeaderRow, tgHeaderCol + 7);
            tgHeaderRange.Style.Font.Bold = true;
            tgHeaderRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            tgHeaderRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            tgHeaderRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            int tgRow = tgHeaderRow + 1;

            ws.Cell(tgRow, tgHeaderCol + 0).Value = "Turno 1 Producción";
            ws.Cell(tgRow, tgHeaderCol + 1).Value = totalTurno1Produccion;
            ws.Cell(tgRow, tgHeaderCol + 2).Value = "Turno 1 Defectos";
            ws.Cell(tgRow, tgHeaderCol + 3).Value = totalTurno1Defectos;
            ws.Cell(tgRow, tgHeaderCol + 4).Value = "Turno 1 Total";
            ws.Cell(tgRow, tgHeaderCol + 5).Value = totalTurno1Produccion + totalTurno1Defectos;
            tgRow++;

            ws.Cell(tgRow, tgHeaderCol + 0).Value = "Turno 2 Producción";
            ws.Cell(tgRow, tgHeaderCol + 1).Value = totalTurno2Produccion;
            ws.Cell(tgRow, tgHeaderCol + 2).Value = "Turno 2 Defectos";
            ws.Cell(tgRow, tgHeaderCol + 3).Value = totalTurno2Defectos;
            ws.Cell(tgRow, tgHeaderCol + 4).Value = "Turno 2 Total";
            ws.Cell(tgRow, tgHeaderCol + 5).Value = totalTurno2Produccion + totalTurno2Defectos;
            tgRow++;

            ws.Cell(tgRow, tgHeaderCol + 0).Value = "Turno 3 Producción";
            ws.Cell(tgRow, tgHeaderCol + 1).Value = totalTurno3Produccion;
            ws.Cell(tgRow, tgHeaderCol + 2).Value = "Turno 3 Defectos";
            ws.Cell(tgRow, tgHeaderCol + 3).Value = totalTurno3Defectos;
            ws.Cell(tgRow, tgHeaderCol + 4).Value = "Turno 3 Total";
            ws.Cell(tgRow, tgHeaderCol + 5).Value = totalTurno3Produccion + totalTurno3Defectos;

            // Ajuste de columnas y estilos generales
            ws.Columns().AdjustToContents();

            // Configuración de impresión para 11x17 horizontal ocupando la página
            ws.PageSetup.PaperSize = XLPaperSize.TabloidPaper;           // 11x17
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;  // horizontal
            ws.PageSetup.Margins.Top = 0.25;
            ws.PageSetup.Margins.Bottom = 0.25;
            ws.PageSetup.Margins.Left = 0.25;
            ws.PageSetup.Margins.Right = 0.25;

            // Ajustar a una sola página (si el contenido excede, ClosedXML escala para caber)
            ws.PageSetup.PagesWide = 1;
            ws.PageSetup.PagesTall = 1;

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(content,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            $"ProduccionDiaLaboral_{fechaFiltro:yyyyMMdd}.xlsx");
            }
        }

    }


    //METODOS PARA CREAR PIEZAS BUENAS
    [Authorize(Roles = "Admin,Editor")]
    [HttpGet]
    public IActionResult Create()
    {
        // Obtener la zona horaria de Matamoros (Central Standard Time con DST local)
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        var nowInMatamoros = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        var model = new RegistrodePiezasEscaneada
        {
            Fecha = DateOnly.FromDateTime(nowInMatamoros),
            Hora = TimeOnly.FromDateTime(nowInMatamoros)
        };

        var mesas = _context.Mesas
     .Where(m => m.IdMesa >= 3 && m.IdMesa <= 24)   // 👈 ahora usamos IdMesa directamente
     .OrderBy(m => m.IdMesa)                        // 👈 ordenamos por IdMesa
     .Select(m => m.Mesas.ToUpper())                // 👈 convertimos el nombre a mayúsculas
     .ToList();

        ViewBag.Mesas = mesas;
        ViewBag.Turnos = new List<string> { "1", "2", "3" };

        return PartialView("_CreateModal", model);
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost]
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
                return Json(new
                {
                    success = false,
                    message = "Error al guardar registro",
                    errors = new List<string> { ex.Message, ex.InnerException?.Message ?? "" }
                });
            }
        }

        // 👇 aquí devolvemos los errores de validación
        var validationErrors = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        return Json(new { success = false, message = "Modelo inválido.", errors = validationErrors });
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(RegistrodePiezasEscaneada model)
    {
        try
        {
            var registro = _context.RegistrodePiezasEscaneadas.Find(model.Id);
            if (registro == null)
                return Json(new { success = false, error = "Registro no encontrado" });

            // Guardamos valores anteriores
            var piezasAntes = registro.Ndpiezas;
            var mandrelAntes = registro.Mandrel;
            var mesaAntes = registro.NuMesa;
            var turnoAntes = registro.Turno;
            var horaAntes = registro.Hora;
            var tmAntes = registro.Tm;

            // Validar mandrel
            bool existeMandrel = _context.Mandriles.Any(m => m.MandrilNombre == model.Mandrel);
            if (!existeMandrel)
                return Json(new { success = false, error = "El mandrel ingresado no existe en el catálogo." });

            // Actualizamos campos
            registro.Mandrel = model.Mandrel;
            registro.Ndpiezas = model.Ndpiezas;
            registro.Turno = model.Turno;
            registro.NuMesa = model.NuMesa;
            registro.Tm = model.Tm;
            registro.Hora = model.Hora; // 👈 directo, sin parseo

            _context.SaveChanges();

            // Log de cambios
            var cambios = new List<string>();
            if (piezasAntes != model.Ndpiezas) cambios.Add($"Piezas: {piezasAntes} → {model.Ndpiezas}");
            if (mandrelAntes != model.Mandrel) cambios.Add($"Mandrel: {mandrelAntes} → {model.Mandrel}");
            if (mesaAntes != model.NuMesa) cambios.Add($"Mesa: {mesaAntes} → {model.NuMesa}");
            if (turnoAntes != model.Turno) cambios.Add($"Turno: {turnoAntes} → {model.Turno}");
            if (horaAntes != registro.Hora) cambios.Add($"Hora: {horaAntes:HH:mm} → {registro.Hora:HH:mm}");
            if (tmAntes != model.Tm) cambios.Add($"TM: {tmAntes} → {model.Tm}");

            if (cambios.Any())
            {
                string mensaje = $"Registro [Id={registro.Id} | TM={tmAntes}] . " + string.Join(" | ", cambios);
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
        var mandriles = _context.Mandriles
            .Where(m => m.Area == "INSPECCION" &&  m.MandrilNombre.Contains(term))
            .Select(m => m.MandrilNombre)
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

        var mandriles = _context.Mandriles
            .Where(m => m.Area == "INSPECCION" && m.MandrilNombre.Contains(term))
            .Select(m => m.MandrilNombre)
            .Take(10) // limitar resultados
            .ToList();

        return Json(mandriles);
    }

    //REPORTE DE PRODUCCION GENERAL
    public IActionResult ReporteProduccion(DateOnly? fecha, string? turno, string? mesa)
    {
        // --- Fecha laboral ---
        if (!fecha.HasValue)
        {
            var zona = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
            var ahora = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);
            fecha = DateOnly.FromDateTime(ProduccionHelper.GetFechaProduccion(ahora));
        }

        var fechaFiltro = fecha.Value;

        // --- Determinar turno actual si no se seleccionó ---
        string turnoSeleccionado = turno;

        // Si es null => calcular turno actual
        if (turnoSeleccionado == null)
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

        // --- Producciones ---
        var registros = _context.RegistrodePiezasEscaneadas
            .Where(r => r.Fecha >= fechaFiltro.AddDays(-1) && r.Fecha <= fechaFiltro.AddDays(1))
            .ToList()
            .Where(r => ProduccionHelper.GetFechaProduccion(r.Fecha.ToDateTime(r.Hora)).Date
                        == fechaFiltro.ToDateTime(TimeOnly.MinValue).Date);

        // ✅ aplicar filtro de turno solo si NO es "Todos"
        if (!string.IsNullOrWhiteSpace(turnoSeleccionado) && !turnoSeleccionado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            registros = registros.Where(r => r.Turno.Trim().Equals(turnoSeleccionado, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(mesa))
        {
            registros = registros.Where(r => r.NuMesa.Trim().Equals(mesa.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        // --- Defectos ---
        var defectos = _context.RegistrodeDefectos
            .Where(d => d.Fecha >= fechaFiltro.AddDays(-1) && d.Fecha <= fechaFiltro.AddDays(1))
            .ToList()
            .Where(d => ProduccionHelper.GetFechaProduccion(d.Fecha.ToDateTime(d.Hora)).Date
                        == fechaFiltro.ToDateTime(TimeOnly.MinValue).Date);

        // ✅ aplicar filtro de turno solo si NO es "Todos"
        if (!string.IsNullOrWhiteSpace(turnoSeleccionado) && !turnoSeleccionado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            defectos = defectos.Where(d => d.Turno.Trim().Equals(turnoSeleccionado, StringComparison.OrdinalIgnoreCase));
        }

        defectos = defectos
            .Where(d => string.IsNullOrWhiteSpace(mesa) || d.NuMesa.Trim().Equals(mesa.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(d => !string.IsNullOrEmpty(d.Defecto) && !string.IsNullOrEmpty(d.Mandrel))
            .ToList();

        // --- ViewBag ---
        ViewBag.FechaSeleccionada = fechaFiltro.ToString("yyyy-MM-dd");
        ViewBag.TurnoSeleccionado = turnoSeleccionado;


        // --- Mesas disponibles ---
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
            Turno = turnoSeleccionado, // ✅ aquí debe ir el turno calculado
            Mesa = mesa,
            Reporte = datos
        };

        return View(filtro);
    }

    // PRODUCCION POR USUARIO - FECHA
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

        var registros = _context.RegistrodePiezasEscaneadas
            .Where(r => r.Fecha >= fechaFiltro.AddDays(-1) && r.Fecha <= fechaFiltro.AddDays(1))
            .ToList()
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
        ViewBag.TotalPiezas = registros.Sum(d => int.TryParse(d.NumeroDePiezas, out int cantidad) ? cantidad : 0);
        ViewBag.Usuario = usuario;
        ViewBag.Fecha = fechaFiltro.ToString("yyyy-MM-dd");
        ViewBag.Turnos = registros.Select(r => r.Turno).Distinct().ToList();

        // --- Foto del usuario ---
        var usuarioBD = _context.Users.FirstOrDefault(u => u.Nombre == usuario);
        string numeroEmpleadoBD = usuarioBD?.NumerodeEmpleado ?? "0000";
        string numeroEmpleadoFoto = int.Parse(numeroEmpleadoBD).ToString();

        string fotoPathJPG = Path.Combine("wwwroot/images/tm", $"{numeroEmpleadoFoto}.JPG");
        string fotoPathjpg = Path.Combine("wwwroot/images/tm", $"{numeroEmpleadoFoto}.jpg");

        string fotoUrl;
        if (System.IO.File.Exists(fotoPathJPG))
            fotoUrl = $"/images/tm/{numeroEmpleadoFoto}.JPG";
        else if (System.IO.File.Exists(fotoPathjpg))
            fotoUrl = $"/images/tm/{numeroEmpleadoFoto}.jpg";
        else
            fotoUrl = "/images/tm/thumbnail.png";

        ViewBag.FotoUrl = fotoUrl;

        // --- Datos para la gráfica ---
        var datosAgrupados = registros
            .GroupBy(r => new { r.Turno, r.Mandrel })
            .Select(g => new {
                Turno = g.Key.Turno,
                Mandrel = g.Key.Mandrel,
                Total = g.Sum(x => int.TryParse(x.NumeroDePiezas, out var n) ? n : 0)
            }).ToList();

        ViewBag.Registros = registros.Select(r => new {
            Hora = r.Hora.ToString(@"hh\:mm"),
            Mandrel = r.Mandrel,
            Turno = r.Turno,
            Piezas = int.TryParse(r.NumeroDePiezas, out var n) ? n : 0
        }).ToList();

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

        // Si no se seleccionó fecha, usar la fecha laboral local de Matamoros
        var ahora = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);
        var fechaSeleccionada = fecha ?? ProduccionHelper.GetFechaProduccion(ahora);
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

        // --- Producción por Mandrel y turno ---
        var produccionPorMandrelYTurno = producciones
            .Where(r => !string.IsNullOrWhiteSpace(r.Mandrel) && !string.IsNullOrWhiteSpace(r.Turno))
            .GroupBy(r => r.Mandrel.Trim())
            .OrderBy(g => g.Key) // orden alfabético de mandrel
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(r => r.Turno.Trim())
                      .ToDictionary(
                          t => t.Key,
                          t => t.Sum(r => int.TryParse(r.Ndpiezas, out var n) ? n : 0)
                      )
            );

        // --- Preparar datos para Chart.js ---
        ViewBag.MandrelLabels = produccionPorMandrelYTurno.Keys.ToList();
        ViewBag.Turno1PorMandrel = produccionPorMandrelYTurno.Values.Select(m => m.ContainsKey("1") ? m["1"] : 0).ToList();
        ViewBag.Turno2PorMandrel = produccionPorMandrelYTurno.Values.Select(m => m.ContainsKey("2") ? m["2"] : 0).ToList();
        ViewBag.Turno3PorMandrel = produccionPorMandrelYTurno.Values.Select(m => m.ContainsKey("3") ? m["3"] : 0).ToList();

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
            var fechaInicio = fechaFiltro.AddDays(-1);
            var fechaFin = fechaFiltro.AddDays(1);

            registros = await baseQuery
                .Where(x => x.Fecha >= fechaInicio && x.Fecha <= fechaFin)
                .ToListAsync();

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

        var query = registros.AsQueryable();

        // Filtro por turno (solo si no es "Todos")
        if (!string.IsNullOrEmpty(turnoSeleccionado))
        {
            query = query.Where(x => x.Turno == turnoSeleccionado);
        }

        // Filtro global búsqueda
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

        var data = query
            .Skip(request.Start)
            .Take(request.Length)
            .ToList();

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

