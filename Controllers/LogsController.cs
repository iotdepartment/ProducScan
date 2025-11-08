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
        public IActionResult TablaLogs(string? nivel = "all", string? categoria = "all", DateTime? fecha = null)
        {
            var query = _context.Logs.AsQueryable();

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

            var logs = query
            .OrderByDescending(l => l.Fecha)
            .ToList();

            return PartialView("_TablaLogs", logs);
        }


    }
}
