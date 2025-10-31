// Controllers/HomeController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProducScan.Controllers
{
    [Authorize] // requiere estar autenticado
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
        public IActionResult Privacy() => View();
    }
}