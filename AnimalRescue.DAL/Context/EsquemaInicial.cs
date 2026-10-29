using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AnimalRescue.DAL.Context;

/// <summary>
/// Crea la base (si el servidor no la tiene) y el esquema (Scripts/01_esquema.sql) cuando está vacía,
/// para servidores donde no se puede correr psql a mano (p. ej. Render).
/// </summary>
public static class EsquemaInicial
{
    private const string Recurso = "AnimalRescue.DAL.Scripts.01_esquema.sql";
    private const string BaseInexistente = "3D000";

    public static async Task CrearSiFaltaAsync(IServiceProvider servicios)
    {
        await using var scope = servicios.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AnimalRescueDbContext>();

        await CrearBaseSiFaltaAsync(db.Database.GetConnectionString()!);

        var existe = await db.Database
            .SqlQueryRaw<bool>("SELECT to_regclass('public.usuarios') IS NOT NULL AS \"Value\"")
            .SingleAsync();
        if (existe) return;

        await using var script = typeof(EsquemaInicial).Assembly.GetManifestResourceStream(Recurso)
            ?? throw new InvalidOperationException($"No se encontró el recurso {Recurso}.");
        using var lector = new StreamReader(script);
        await db.Database.ExecuteSqlRawAsync(await lector.ReadToEndAsync());
    }

    private static async Task CrearBaseSiFaltaAsync(string conexion)
    {
        try
        {
            await using var prueba = new NpgsqlConnection(conexion);
            await prueba.OpenAsync();
            return;
        }
        catch (PostgresException ex) when (ex.SqlState == BaseInexistente)
        {
        }

        // La base no existe: se crea desde la base de mantenimiento "postgres" del mismo servidor.
        var datos = new NpgsqlConnectionStringBuilder(conexion);
        var nombre = datos.Database!;
        datos.Database = "postgres";
        datos.Pooling = false;

        await using var cn = new NpgsqlConnection(datos.ConnectionString);
        await cn.OpenAsync();
        await using var crear = new NpgsqlCommand($"CREATE DATABASE {new NpgsqlCommandBuilder().QuoteIdentifier(nombre)}", cn);
        await crear.ExecuteNonQueryAsync();
    }
}
