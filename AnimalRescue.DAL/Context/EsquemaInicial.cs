using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AnimalRescue.DAL.Context;

/// <summary>
/// Crea el esquema (Scripts/01_esquema.sql) cuando la base está vacía,
/// para servidores donde no se puede correr psql a mano (p. ej. Render).
/// </summary>
public static class EsquemaInicial
{
    private const string Recurso = "AnimalRescue.DAL.Scripts.01_esquema.sql";

    public static async Task CrearSiFaltaAsync(IServiceProvider servicios)
    {
        await using var scope = servicios.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AnimalRescueDbContext>();

        var existe = await db.Database
            .SqlQueryRaw<bool>("SELECT to_regclass('public.usuarios') IS NOT NULL AS \"Value\"")
            .SingleAsync();
        if (existe) return;

        await using var script = typeof(EsquemaInicial).Assembly.GetManifestResourceStream(Recurso)
            ?? throw new InvalidOperationException($"No se encontró el recurso {Recurso}.");
        using var lector = new StreamReader(script);
        await db.Database.ExecuteSqlRawAsync(await lector.ReadToEndAsync());
    }
}
