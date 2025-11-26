using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ProducScan.Hubs;
using ProducScan.Models;
using ProducScan.Services;

var builder = WebApplication.CreateBuilder(args);

// DbContext de tu app (usa tu cadena de conexión)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Autenticación por cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Denegado";

        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var userName = context.Principal.Identity.Name;
                var securityStampClaim = context.Principal.FindFirst("SecurityStamp")?.Value;

                var user = await db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == userName);

                if (user == null || user.SecurityStamp != securityStampClaim)
                {
                    // 👇 Si el usuario fue eliminado o su SecurityStamp cambió → logout inmediato
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.MapHub<LogsHub>("/logsHub");
app.MapHub<ProduccionHub>("/produccionHub");


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logService = context.RequestServices.GetRequiredService<ILogService>();
        logService.Registrar("Excepción no controlada", ex.ToString(), "Critical");
        throw; // vuelve a lanzar para que no se oculte
    }
});

// Ruta por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=PiezasEscaneadas}/{action=Dashboard}/{id?}");

app.Run();

// Función de hashing simple (sin librerías externas)
// Recomendado: PBKDF2/BCrypt, pero aquí usamos SHA256 por simplicidad.
static string HashPassword(string password)
{
    using var sha = System.Security.Cryptography.SHA256.Create();
    var bytes = System.Text.Encoding.UTF8.GetBytes(password);
    var hash = sha.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}