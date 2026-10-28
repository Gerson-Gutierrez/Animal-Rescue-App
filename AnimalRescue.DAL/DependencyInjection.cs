using AnimalRescue.DAL.Context;
using AnimalRescue.DAL.External.Geocoding;
using AnimalRescue.DAL.External.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnimalRescue.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddDataLayer(this IServiceCollection services, IConfiguration config)
    {
        var conexion = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Default en la configuración.");

        services.AddDbContext<AnimalRescueDbContext>(o => o
            .UseNpgsql(conexion)
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
}
