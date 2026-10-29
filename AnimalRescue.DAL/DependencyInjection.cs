using AnimalRescue.DAL.Context;
using AnimalRescue.DAL.External.Geocoding;
using AnimalRescue.DAL.External.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AnimalRescue.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddDataLayer(this IServiceCollection services, IConfiguration config)
    {
        var conexion = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Default en la configuración.");

        services.AddDbContext<AnimalRescueDbContext>(o => o
            .UseNpgsql(NormalizarConexion(conexion, config["Database:Nombre"]))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<UnitOfWork.IUnitOfWork, UnitOfWork.UnitOfWork>();

        services.Configure<StorageOptions>(config.GetSection(StorageOptions.Seccion));
        services.AddSingleton<IAlmacenamientoArchivos, AlmacenamientoLocal>();

        services.AddMemoryCache();
        services.AddHttpClient<IGeocodificador, NominatimGeocodificador>(c =>
        {
            c.BaseAddress = new Uri(config["Geocoding:BaseUrl"] ?? "https://nominatim.openstreetmap.org/");
            // Nominatim exige identificar la aplicación.
            c.DefaultRequestHeaders.UserAgent.ParseAdd("AnimalRescueApp/1.0");
            c.DefaultRequestHeaders.AcceptLanguage.ParseAdd("es");
            c.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }

    /// <summary>
    /// Render (y otros proveedores) entregan la conexión como URL: postgresql://usuario:clave@host:puerto/base.
    /// Npgsql solo entiende el formato clave=valor, así que se convierte.
    /// Database:Nombre reemplaza la base de la conexión (para usar una base propia en una instancia compartida).
    /// </summary>
    private static string NormalizarConexion(string conexion, string? baseDatos)
    {
        NpgsqlConnectionStringBuilder datos;
        if (Uri.TryCreate(conexion, UriKind.Absolute, out var url) && url.Scheme.StartsWith("postgres"))
        {
            var credenciales = url.UserInfo.Split(':', 2);
            datos = new NpgsqlConnectionStringBuilder
            {
                Host = url.Host,
                Port = url.Port > 0 ? url.Port : 5432,
                Database = url.AbsolutePath.TrimStart('/'),
                Username = Uri.UnescapeDataString(credenciales[0]),
                Password = credenciales.Length > 1 ? Uri.UnescapeDataString(credenciales[1]) : null
            };
        }
        else
        {
            datos = new NpgsqlConnectionStringBuilder(conexion);
        }

        if (!string.IsNullOrWhiteSpace(baseDatos))
            datos.Database = baseDatos;
        return datos.ConnectionString;
    }
}
