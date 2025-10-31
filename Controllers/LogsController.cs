using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProducScan.Models;

namespace ProducScan.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LogsController : Controller
    {
        private readonly AppDbContext _context;

        public LogsController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string? usuario, string? nivel, DateTime? desde, DateTime? hasta)
        {
            var query = _context.Logs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(usuario))
                query = query.Where(l => l.Usuario.Contains(usuario));

            if (!string.IsNullOrWhiteSpace(nivel))
                query = query.Where(l => l.Nivel == nivel);

            if (desde.HasValue)
                query = query.Where(l => l.Fecha >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(l => l.Fecha <= hasta.Value);

            var logs = query.OrderByDescending(l => l.Fecha).Take(1000).ToList();

            return View(logs);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult TablaLogs(string? nivel = "all", string? categoria = "all", DateTime? fecha = null, int page = 1, int pageSize = 10)
            {
                // Base query
                var query = _context.Logs.AsQueryable();

                // Filtros
                if (!string.IsNullOrWhiteSpace(nivel) && nivel != "all")
                    query = query.Where(l => l.Nivel == nivel);

                if (!string.IsNullOrWhiteSpace(categoria) && categoria != "all")
                    query = query.Where(l => l.Categoria == categoria);

                if (fecha.HasValue)
                {
                    var day = fecha.Value.Date;
                    var nextDay = day.AddDays(1);
                    query = query.Where(l => l.Fecha >= day && l.Fecha < nextDay);
                }

                // Total después de filtros
                var totalFiltrados = query.Count();

                // Paginación sobre el conjunto filtrado
                var logs = query
                    .OrderByDescending(l => l.Fecha)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Calcular total de páginas
                var totalPaginas = (int)Math.Ceiling(totalFiltrados / (double)pageSize);

                // Pasar datos a la vista parcial
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPaginas = totalPaginas;
                ViewBag.TotalFiltrados = totalFiltrados;

                // Echo de filtros para que el JS los restaure
                ViewBag.Nivel = nivel ?? "all";
                ViewBag.Categoria = categoria ?? "all";
                ViewBag.Fecha = fecha?.ToString("yyyy-MM-dd") ?? "";

                return PartialView("_TablaLogs", logs);
            }

    }
}
