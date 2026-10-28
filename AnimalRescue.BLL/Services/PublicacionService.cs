using AnimalRescue.BLL.DTOs.Comun;
using AnimalRescue.BLL.DTOs.Publicaciones;
using AnimalRescue.BLL.Exceptions;
using AnimalRescue.BLL.Interfaces;
using AnimalRescue.BLL.Mapping;
using AnimalRescue.BLL.Validators;
using AnimalRescue.DAL.Entities;
using AnimalRescue.DAL.External.Storage;
using AnimalRescue.DAL.UnitOfWork;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AnimalRescue.BLL.Services;

public class PublicacionService(
    IUnitOfWork uow,
    ICurrentUser actual,
    IGeocodificacionService geocodificacion,
    ProcesadorImagenes procesador,
    IAlmacenamientoArchivos almacenamiento,
    IValidator<CrearPublicacionRequest> validarCrear,
    IValidator<EditarPublicacionRequest> validarEditar) : IPublicacionService
{
    private const int MaxImagenes = 5;
    private const string CarpetaImagenes = "publicaciones";

    public async Task<PaginaDto<PublicacionCardDto>> FeedAsync(int pagina, int tamano, string? busqueda, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamano = Math.Clamp(tamano, 1, 50);

        var consulta = uow.Publicaciones.QuerySinSeguimiento().Where(p => p.Estado == EstadoPublicacion.ACTIVA);
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var patron = $"%{busqueda.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            consulta = consulta.Where(p => EF.Functions.ILike(p.NombreMascota, patron) || EF.Functions.ILike(p.UbicacionTexto, patron));
        }

        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderByDescending(p => p.FechaCreacion)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .Select(PublicacionMapeo.Card)
            .ToListAsync(ct);

        return new PaginaDto<PublicacionCardDto>(items, pagina, tamano, total);
    }

    public async Task<PublicacionDetalleDto> DetalleAsync(Guid id, CancellationToken ct = default)
    {
        var p = await uow.Publicaciones.QuerySinSeguimiento()
            .Include(x => x.Imagenes)
            .Include(x => x.Usuario)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (p is null || (p.Estado == EstadoPublicacion.ELIMINADA && !actual.EsAdmin))
            throw new NotFoundException("La publicación no existe.");

        var yaReportada = false;
        if (actual.Id is { } usuarioId)
            yaReportada = await uow.Reportes.QuerySinSeguimiento()
                .AnyAsync(r => r.PublicacionId == id && r.ReportadoPorId == usuarioId, ct);

        return PublicacionMapeo.Detalle(p, p.UsuarioId == actual.Id, yaReportada);
    }

    public async Task<PublicacionDetalleDto> CrearAsync(
        CrearPublicacionRequest request, IReadOnlyList<ArchivoSubido> imagenes, CancellationToken ct = default)
    {
        var usuarioId = actual.IdRequerido;
        await validarCrear.ValidarYLanzarAsync(request, ct);

        if (imagenes.Count == 0)
            throw new ValidacionException(new Dictionary<string, string[]> { ["imagenes"] = ["Sube al menos una imagen de la mascota."] });
        if (imagenes.Count > MaxImagenes)
            throw new ValidacionException(new Dictionary<string, string[]> { ["imagenes"] = [$"Máximo {MaxImagenes} imágenes."] });

        var (latitud, longitud) = await ResolverUbicacionAsync(request.UbicacionTexto, request.Latitud, request.Longitud, ct);

        var publicacion = new Publicacion
        {
            UsuarioId = usuarioId,
            NombreMascota = request.NombreMascota.Trim(),
            Color = request.Color.Trim(),
            Tamano = request.Tamano,
            Edad = LimpiarOpcional(request.Edad),
            Castrado = request.Castrado,
            TieneCollar = request.TieneCollar,
            TieneChapitaQr = request.TieneChapitaQr,
            Descripcion = LimpiarOpcional(request.Descripcion),
            FechaPerdida = request.FechaPerdida,
            Recompensa = request.Recompensa,
            UbicacionTexto = request.UbicacionTexto.Trim(),
            Latitud = latitud,
            Longitud = longitud
        };

        var guardadas = new List<ImagenGuardada>();
        try
        {
            short orden = 0;
            foreach (var archivo in imagenes)
            {
                var guardada = await procesador.GuardarAsync(archivo, CarpetaImagenes, ct);
                guardadas.Add(guardada);
                publicacion.Imagenes.Add(new ImagenPublicacion { Url = guardada.Url, UrlMiniatura = guardada.UrlMiniatura, Orden = orden++ });
            }

            uow.Publicaciones.Agregar(publicacion);
            await uow.SaveChangesAsync(ct);
        }
        catch
        {
            await BorrarArchivosAsync(guardadas.SelectMany(g => new[] { g.Url, g.UrlMiniatura }));
            throw;
        }

        return await DetalleAsync(publicacion.Id, ct);
    }

    public async Task<PublicacionDetalleDto> EditarAsync(
        Guid id, EditarPublicacionRequest request, IReadOnlyList<ArchivoSubido> nuevasImagenes, CancellationToken ct = default)
    {
        var usuarioId = actual.IdRequerido;
        var p = await uow.Publicaciones.Query()
            .Include(x => x.Imagenes)
            .FirstOrDefaultAsync(x => x.Id == id && x.Estado != EstadoPublicacion.ELIMINADA, ct)
            ?? throw new NotFoundException("La publicación no existe.");

        if (p.UsuarioId != usuarioId)
            throw new ForbiddenException("Sólo el autor puede editar la publicación.");

        await validarEditar.ValidarYLanzarAsync(request, ct);

        var idsEliminar = request.EliminarImagenIds ?? [];
        var aQuitar = p.Imagenes.Where(i => idsEliminar.Contains(i.Id)).ToList();
        var restantes = p.Imagenes.Except(aQuitar).OrderBy(i => i.Orden).ToList();
        var total = restantes.Count + nuevasImagenes.Count;
        if (total is < 1 or > MaxImagenes)
            throw new ValidacionException(new Dictionary<string, string[]>
            {
                ["imagenes"] = [$"La publicación debe tener entre 1 y {MaxImagenes} imágenes (quedarían {total})."]
            });

        var ubicacionTexto = request.UbicacionTexto.Trim();
        if (request.Latitud.HasValue)
        {
            p.Latitud = request.Latitud.Value;
            p.Longitud = request.Longitud!.Value;
        }
        else if (!string.Equals(ubicacionTexto, p.UbicacionTexto, StringComparison.Ordinal))
        {
            (p.Latitud, p.Longitud) = await ResolverUbicacionAsync(ubicacionTexto, null, null, ct);
        }

        p.NombreMascota = request.NombreMascota.Trim();
        p.Color = request.Color.Trim();
        p.Tamano = request.Tamano;
        p.Edad = LimpiarOpcional(request.Edad);
        p.Castrado = request.Castrado;
        p.TieneCollar = request.TieneCollar;
        p.TieneChapitaQr = request.TieneChapitaQr;
        p.Descripcion = LimpiarOpcional(request.Descripcion);
        p.FechaPerdida = request.FechaPerdida;
        p.Recompensa = request.Recompensa;
        p.UbicacionTexto = ubicacionTexto;
        p.Estado = request.Estado;

        // Se renumeran las imágenes restantes (la restricción UNIQUE(publicacion, orden) es diferida).
        short orden = 0;
        foreach (var imagen in restantes) imagen.Orden = orden++;
        uow.Imagenes.EliminarVarios(aQuitar);

        var guardadas = new List<ImagenGuardada>();
        try
        {
            foreach (var archivo in nuevasImagenes)
            {
                var guardada = await procesador.GuardarAsync(archivo, CarpetaImagenes, ct);
                guardadas.Add(guardada);
                // Agregar vía repositorio: con el Id ya asignado, EF la tomaría por existente si sólo entra por la colección.
                uow.Imagenes.Agregar(new ImagenPublicacion
                {
                    PublicacionId = p.Id, Url = guardada.Url, UrlMiniatura = guardada.UrlMiniatura, Orden = orden++
                });
            }

            await uow.SaveChangesAsync(ct);
        }
        catch
        {
            await BorrarArchivosAsync(guardadas.SelectMany(g => new[] { g.Url, g.UrlMiniatura }));
            throw;
        }

        await BorrarArchivosAsync(aQuitar.SelectMany(i => new[] { i.Url, i.UrlMiniatura }));
        return await DetalleAsync(p.Id, ct);
    }

    public async Task EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var usuarioId = actual.IdRequerido;
        var p = await uow.Publicaciones.Query()
            .FirstOrDefaultAsync(x => x.Id == id && x.Estado != EstadoPublicacion.ELIMINADA, ct)
            ?? throw new NotFoundException("La publicación no existe.");

        if (p.UsuarioId != usuarioId)
            throw new ForbiddenException("Sólo el autor puede eliminar la publicación.");

        p.Estado = EstadoPublicacion.ELIMINADA;
        p.FechaEliminacion = DateTime.UtcNow;
        p.EliminadaPorId = usuarioId;
        await uow.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PublicacionCardDto>> MiasAsync(CancellationToken ct = default)
    {
        var usuarioId = actual.IdRequerido;
        return await uow.Publicaciones.QuerySinSeguimiento()
            .Where(p => p.UsuarioId == usuarioId && p.Estado != EstadoPublicacion.ELIMINADA)
            .OrderByDescending(p => p.FechaCreacion)
            .Select(PublicacionMapeo.Card)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MarcadorMapaDto>> MapaAsync(
        double? norte, double? sur, double? este, double? oeste, CancellationToken ct = default)
    {
        var consulta = uow.Publicaciones.QuerySinSeguimiento().Where(p => p.Estado == EstadoPublicacion.ACTIVA);

        if (norte.HasValue && sur.HasValue)
            consulta = consulta.Where(p => p.Latitud <= norte.Value && p.Latitud >= sur.Value);

        if (este.HasValue && oeste.HasValue)
        {
            var (e, o) = (este.Value, oeste.Value);
            // Si el rectángulo cruza el antimeridiano, oeste es mayor que este.
            consulta = o <= e
                ? consulta.Where(p => p.Longitud >= o && p.Longitud <= e)
                : consulta.Where(p => p.Longitud >= o || p.Longitud <= e);
        }

        return await consulta
            .OrderByDescending(p => p.FechaCreacion)
            .Take(500)
            .Select(p => new MarcadorMapaDto(
                p.Id,
                p.NombreMascota,
                p.Imagenes.OrderBy(i => i.Orden).Select(i => i.UrlMiniatura).FirstOrDefault(),
                p.Latitud,
                p.Longitud))
            .ToListAsync(ct);
    }

    private async Task<(double Latitud, double Longitud)> ResolverUbicacionAsync(
        string texto, double? latitud, double? longitud, CancellationToken ct)
    {
        if (latitud.HasValue && longitud.HasValue) return (latitud.Value, longitud.Value);

        var encontrada = await geocodificacion.PrimeraAsync(texto, ct)
            ?? throw new ReglaNegocioException("No se encontró la ubicación indicada. Prueba con una dirección más específica o marca el punto en el mapa.");
        return (encontrada.Latitud, encontrada.Longitud);
    }

    private async Task BorrarArchivosAsync(IEnumerable<string> urls)
    {
        foreach (var url in urls) await almacenamiento.EliminarAsync(url);
    }

    private static string? LimpiarOpcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
