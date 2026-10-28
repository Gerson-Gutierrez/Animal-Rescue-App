using AnimalRescue.BLL.Interfaces;
using AnimalRescue.BLL.Options;
using AnimalRescue.BLL.Services;
using AnimalRescue.DAL;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnimalRescue.BLL;

public static class DependencyInjection
{
    /// <summary>Registra la lógica de negocio (y, a través de ella, la capa de datos).</summary>
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services, IConfiguration config)
    {
        services.AddDataLayer(config);

        services.AddOptions<JwtOptions>()
            .Bind(config.GetSection(JwtOptions.Seccion))
            .Validate(o => !string.IsNullOrEmpty(o.Secret) && o.Secret.Length >= 32, "Jwt:Secret debe tener al menos 32 caracteres.")
            .ValidateOnStart();
        services.Configure<BootstrapOptions>(config.GetSection(BootstrapOptions.Seccion));

        services.AddValidatorsFromAssemblyContaining<AuthService>(includeInternalTypes: true);

        services.AddSingleton<TokenService>();
        services.AddScoped<ProcesadorImagenes>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPublicacionService, PublicacionService>();
        services.AddScoped<IGeocodificacionService, GeocodificacionService>();
        services.AddScoped<IMensajeriaService, MensajeriaService>();
        services.AddScoped<IHogarTransitoService, HogarTransitoService>();
        services.AddScoped<IRefugioService, RefugioService>();
        services.AddScoped<IReporteService, ReporteService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}
