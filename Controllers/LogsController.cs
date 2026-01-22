using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProducScan.Models;

namespace ProducScan.Controllers
{
    [Authorize(Roles = "Admin,Editor")]
    public class LogsController : Controller
    {
        private readonly AppDbContext _context;

        public LogsController(AppDbContext context)
        {
            _context = context;
        }

        // Vista principal
        public IActionResult Index()
        {
            // Se carga inicialmente con los últimos registros
            var logs = _context.Logs
                .OrderByDescending(l => l.Fecha)
                .Take(100)
                .ToList();

            return View(logs);
        }

        // Acción que devuelve el partial filtrado
        [HttpGet]
        public IActionResult TablaLogs(string? nivel, string? categoria, DateTime? fecha)
        {
            var query = _context.Logs.AsQueryable();

            // Filtros
            if (!string.IsNullOrWhiteSpace(nivel) && nivel != "all")
                query = query.Where(l => l.Nivel == nivel);

            if (!string.IsNullOrWhiteSpace(categoria) && categoria != "all")
                query = query.Where(l => l.Categoria == categoria);

            if (fecha.HasValue)
                query = query.Where(l => l.Fecha.Date == fecha.Value.Date);

            // Restricción por rol
            if (User.IsInRole("Editor"))
            {
                query = query.Where(l => l.Categoria == "Producción");
            }

            var logs = query.OrderByDescending(l => l.Fecha).Take(1000).ToList();

            return PartialView("_TablaLogs", logs);
        }
    }
}
