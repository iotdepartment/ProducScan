using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProducScan.Models;


public class RegistrosController : Controller
    {
        private readonly AppDbContext _context;

        public RegistrosController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var registros = _context.Registros
                .FromSqlRaw("SELECT TOP (10) Fecha, Hora, NuMesa, Tm, Mandrel, Ndpiezas, Turno FROM RegistrodePiezasEscaneadas ORDER BY Fecha DESC")
                .ToList();

            return View(registros);
        }

        [HttpPost]
        public IActionResult Update(string Fecha, string Hora, string NuMesa, string Tm,
                             string Mandrel, string Ndpiezas, string Turno)
        {
            try
            {
                var sql = @"UPDATE RegistrodePiezasEscaneadas
                    SET Mandrel = @Mandrel,
                        Ndpiezas = @Ndpiezas,
                        Turno = @Turno
                    WHERE Fecha = @Fecha AND Hora = @Hora AND NuMesa = @NuMesa AND Tm = @Tm";

                _context.Database.ExecuteSqlRaw(sql,
                    new SqlParameter("@Fecha", DateOnly.ParseExact(Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    new SqlParameter("@Hora", TimeOnly.ParseExact(Hora, "HH:mm:ss", CultureInfo.InvariantCulture)),
                    new SqlParameter("@NuMesa", NuMesa),
                    new SqlParameter("@Tm", Tm),
                    new SqlParameter("@Mandrel", Mandrel ?? (object)DBNull.Value),
                    new SqlParameter("@Ndpiezas", Ndpiezas ?? (object)DBNull.Value),
                    new SqlParameter("@Turno", Turno ?? (object)DBNull.Value)
                );

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Delete(string Fecha, string Hora, string NuMesa, string Tm)
        {
            try
            {
                var sql = @"DELETE FROM RegistrodePiezasEscaneadas
                    WHERE Fecha = @Fecha AND Hora = @Hora AND NuMesa = @NuMesa AND Tm = @Tm";

                _context.Database.ExecuteSqlRaw(sql,
                    new SqlParameter("@Fecha", DateOnly.ParseExact(Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    new SqlParameter("@Hora", TimeOnly.ParseExact(Hora, "HH:mm:ss", CultureInfo.InvariantCulture)),
                    new SqlParameter("@NuMesa", NuMesa),
                    new SqlParameter("@Tm", Tm)
                );

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
