using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ProducScan.Hubs;
using ProducScan.Models;
using ProducScan.Services;

var builder = WebApplication.CreateBuilder(args);

// DbContext
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

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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

// 🔥 USAR SESSION
app.UseSession();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;

    if (context.Request.Method == "GET" &&
        !string.IsNullOrWhiteSpace(path) &&
        !path.StartsWith("/Auth") &&
        !path.StartsWith("/css") &&
        !path.StartsWith("/js") &&
        !path.StartsWith("/lib") &&
        !path.StartsWith("/images") &&
        !path.StartsWith("/logsHub") &&
        !path.StartsWith("/produccionHub") &&
        !path.Contains("/Get") &&
        !path.Contains("/Tabla") &&
        !path.Contains("/Load") &&
        !path.Contains("/Export") &&
        !path.Contains("/Json") &&
        !path.Contains("?") &&            // 👈 evita rutas con parámetros
        !path.Contains("Detalle")         // 👈 evita vistas dependientes de parámetros
        )
    {
        context.Session.SetString("UltimaVista", path);
    }

    await next();
});

// Middleware de logging de excepciones
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
        throw;
    }
});

// Ruta por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=PiezasEscaneadas}/{action=Dashboard}/{id?}");

app.Run();

// Hash simple
static string HashPassword(string password)
{
    using var sha = System.Security.Cryptography.SHA256.Create();
    var bytes = System.Text.Encoding.UTF8.GetBytes(password);
    var hash = sha.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}