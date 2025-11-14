using Microsoft.AspNetCore.SignalR;
using ProducScan.Hubs;
using ProducScan.Models;

namespace ProducScan.Services
{
    public class LogService : ILogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<LogsHub> _hub;

        public LogService(AppDbContext context, IHttpContextAccessor accessor, IHubContext<LogsHub> hub)
        {
            _context = context;
            _httpContextAccessor = accessor;
            _hub = hub;
        }

        public void Registrar(string accion, string detalles, string nivel = "Info", string usuario = null, string ip = null, string categoria = "Sistema")
        {
            // 👇 Convertir siempre a hora local de Matamoros
            var zona = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
            var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);

            var log = new Log
            {
                Accion = accion,
                Detalles = detalles,
                Nivel = nivel,
                Usuario = usuario ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Sistema",
                Ip = ip ?? _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Desconocida",
                Fecha = ahoraLocal,
                Categoria = categoria
            };

            _context.Logs.Add(log);
            _context.SaveChanges();

            // Notificar en tiempo real
            _hub.Clients.All.SendAsync("NuevoLog", new
            {
                log.Id,
                log.Fecha,
                log.Usuario,
                log.Accion,
                log.Detalles,
                log.Nivel,
                log.Ip,
                log.Categoria
            });
        }
    }
}