using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProducScan.Models;

namespace ProducScan.Controllers
{
    public class SafeLaunchController : Controller
    {
        private readonly AppDbContext _context;

        public SafeLaunchController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ListaKanban()
        {
            var kanban = _context.Kanbans
                .Include(k => k.SlDefectos)
                .ToList();

            return View(kanban);
        }

        public IActionResult GetKanbanAjax()
        {
            var data = _context.Kanbans
                .Select(k => new {
                    id = k.Id,
                    modelo = k.Modelo,
                    defectos = k.SlDefectos.Count
                })
                .ToList();

            return Json(new { data });
        }

        [HttpPost]
        public IActionResult CrearKanbanAjax([FromBody] Kanban model)
        {
            if (string.IsNullOrWhiteSpace(model.Modelo))
                return BadRequest("El modelo es obligatorio.");

            _context.Kanbans.Add(model);
            _context.SaveChanges();

            return Ok(new { success = true });
        }
    }
}