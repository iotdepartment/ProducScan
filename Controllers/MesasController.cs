using Microsoft.AspNetCore.Mvc;
using ProducScan.Models;

public class MesasController : Controller
{
    private readonly AppDbContext _context;

    public MesasController(AppDbContext context)
    {
        _context = context;
    }

    // LISTADO
    public IActionResult Index()
    {
        return View();
    }

    // AJAX: Obtener mesas con formato MESA#
    [HttpGet]
    public IActionResult GetMesas()
    {
        var mesas = _context.Mesas
            .Where(m => m.Mesas != null && m.Mesas.StartsWith("Mesa#"))
            .Select(m => new
            {
                m.IdMesa,
                m.Id,
                m.Mesas,
                m.NumerodeMesa,
                m.Meta
            })
            .ToList();

        return Json(new { data = mesas });
    }

    // EDITAR META
    [HttpPost]
    public IActionResult EditMeta(int idMesa, int meta)
    {
        var mesa = _context.Mesas.FirstOrDefault(m => m.IdMesa == idMesa);

        if (mesa == null)
            return Json(new { success = false, message = "Mesa no encontrada" });

        mesa.Meta = meta;
        _context.SaveChanges();

        return Json(new { success = true, message = "Meta actualizada correctamente" });
    }
}