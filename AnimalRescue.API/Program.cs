using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AnimalRescue.API.Auth;
using AnimalRescue.API.Hubs;
using AnimalRescue.API.Middleware;
using AnimalRescue.BLL;
using AnimalRescue.BLL.Interfaces;
using AnimalRescue.BLL.Options;
using FluentValidation;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

// Cultura invariante: los formularios multipart envían decimales como "1500.50".
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es");

var builder = WebApplication.CreateBuilder(args);

// Los archivos subidos viven fuera de wwwroot; se resuelve la ruta respecto a la carpeta del proyecto.
var carpetaArchivos = Path.GetFullPath(
    builder.Configuration["Storage:RootPath"] ?? "uploads", builder.Environment.ContentRootPath);
builder.Configuration["Storage:RootPath"] = carpetaArchivos;
var prefijoArchivos = builder.Configuration["Storage:RequestPath"] ?? "/uploads";
Directory.CreateDirectory(carpetaArchivos);

builder.Services.AddBusinessLayer(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<INotificadorMensajes, SignalRNotificador>();
builder.Services.AddSingleton<IUserIdProvider, SubUserIdProvider>();

builder.Services
    .AddControllers(o => o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSignalR()
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 40_000_000);

// ----- Autenticación JWT -----
var jwt = builder.Configuration.GetSection(JwtOptions.Seccion).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Falta la sección Jwt en la configuración.");

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret ?? string.Empty)),
            NameClaimType = "name",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // Los WebSockets no pueden enviar cabeceras: SignalR manda el token en la query.
        o.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments(ChatHub.Ruta))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.EsAdmin, p => p.RequireRole("ADMIN", "SUPERADMIN"))
    .AddPolicy(Policies.EsSuperAdmin, p => p.RequireRole("SUPERADMIN"));

// ----- CORS -----
var origenes = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(origenes)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// ----- Límite de peticiones -----
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy("geocodificacion", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.User.FindFirst("sub")?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(carpetaArchivos),
    RequestPath = prefijoArchivos
});

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { estado = "ok" }));
app.MapControllers();
app.MapHub<ChatHub>(ChatHub.Ruta);

app.Run();

public partial class Program;
