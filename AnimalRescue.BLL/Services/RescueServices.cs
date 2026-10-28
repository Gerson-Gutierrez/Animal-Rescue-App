using AnimalRescue.BLL.DTOs.Rescue;
using AnimalRescue.BLL.Exceptions;
using AnimalRescue.BLL.Interfaces;
using AnimalRescue.BLL.Validators;
using AnimalRescue.DAL.Entities;
using AnimalRescue.DAL.UnitOfWork;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AnimalRescue.BLL.Services;

public class HogarTransitoService(
    IUnitOfWork uow,
    ICurrentUser actual,
    IValidator<CrearHogarRequest> validar) : IHogarTransitoService
{
    public async Task<IReadOnlyList<HogarTransitoDto>> ListarAsync(CancellationToken ct = default)
    {
        var yo = actual.IdRequerido;
        return await uow.HogaresTransito.QuerySinSeguimiento()
            .OrderByDescending(h => h.FechaCreacion)
            .Select(h => new HogarTransitoDto(h.Id, h.Nombre, h.Telefono, h.Email, h.Ciudad, h.Pais, h.UsuarioId == yo, h.FechaCreacion))
            .ToListAsync(ct);
    }

    public async Task<HogarTransitoDto?> MioAsync(CancellationToken ct = default)
    {
        var yo = actual.IdRequerido;
        var h = await uow.HogaresTransito.QuerySinSeguimiento().FirstOrDefaultAsync(x => x.UsuarioId == yo, ct);
        return h is null ? null : Mapear(h, true);
    }

    public async Task<HogarTransitoDto> CrearAsync(CrearHogarRequest request, CancellationToken ct = default)
    {
        var yo = actual.IdRequerido;
        await validar.ValidarYLanzarAsync(request, ct);

        if (await uow.HogaresTransito.QuerySinSeguimiento().AnyAsync(h => h.UsuarioId == yo, ct))
            throw new ConflictException("Ya te postulaste como hogar de tránsito. Elimina tu postulación para crear otra.");

        var hogar = new HogarTransito
        {
            UsuarioId = yo,
            Nombre = request.Nombre.Trim(),
            Telefono = request.Telefono.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Ciudad = request.Ciudad.Trim(),
            Pais = request.Pais.Trim()
        };
        uow.HogaresTransito.Agregar(hogar);

        try
        {
            await uow.SaveChangesAsync(ct);
        }
        catch (ConflictoDatosException)
        {
            throw new ConflictException("Ya te postulaste como hogar de tránsito.");
        }

        return Mapear(hogar, true);
    }

    public async Task EliminarMioAsync(CancellationToken ct = default)
    {
        var yo = actual.IdRequerido;
        var hogar = await uow.HogaresTransito.Query().FirstOrDefaultAsync(h => h.UsuarioId == yo, ct)
            ?? throw new NotFoundException("No tienes una postulación como hogar de tránsito.");

        uow.HogaresTransito.Eliminar(hogar);
        await uow.SaveChangesAsync(ct);
    }

    private static HogarTransitoDto Mapear(HogarTransito h, bool esMio) =>
        new(h.Id, h.Nombre, h.Telefono, h.Email, h.Ciudad, h.Pais, esMio, h.FechaCreacion);
}

public class RefugioService(
    IUnitOfWork uow,
    ICurrentUser actual,
    IGeocodificacionService geocodificacion,
    IValidator<CrearRefugioRequest> validar) : IRefugioService
{
    public async Task<IReadOnlyList<RefugioDto>> ListarAsync(CancellationToken ct = default)
    {
        var yo = actual.Id;
        var admin = actual.EsAdmin;
        return await uow.Refugios.QuerySinSeguimiento()
            .OrderByDescending(r => r.FechaCreacion)
            .Select(r => new RefugioDto(r.Id, r.Nombre, r.Direccion, r.Latitud, r.Longitud,
                admin || (yo != null && r.CreadoPorId == yo)))
            .ToListAsync(ct);
    }

    public async Task<RefugioDto> CrearAsync(CrearRefugioRequest request, CancellationToken ct = default)
    {
        var yo = actual.IdRequerido;
        await validar.ValidarYLanzarAsync(request, ct);

        var direccion = request.Direccion.Trim();
        var ubicacion = await geocodificacion.PrimeraAsync(direccion, ct)
            ?? throw new ReglaNegocioException("No se encontró esa dirección. Prueba con calle, número, ciudad y país.");

        var refugio = new Refugio
        {
            Nombre = string.IsNullOrWhiteSpace(request.Nombre) ? null : request.Nombre.Trim(),
            Direccion = direccion,
            Latitud = ubicacion.Latitud,
            Longitud = ubicacion.Longitud,
            CreadoPorId = yo
        };
        uow.Refugios.Agregar(refugio);
        await uow.SaveChangesAsync(ct);

        return new RefugioDto(refugio.Id, refugio.Nombre, refugio.Direccion, refugio.Latitud, refugio.Longitud, true);
    }

    public async Task EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var yo = actual.IdRequerido;
        var refugio = await uow.Refugios.Query().FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("El refugio no existe.");

        if (refugio.CreadoPorId != yo && !actual.EsAdmin)
            throw new ForbiddenException("Sólo quien agregó el refugio (o un administrador) puede eliminarlo.");

        uow.Refugios.Eliminar(refugio);
        if (refugio.CreadoPorId != yo)
        {
            uow.Auditoria.Agregar(new AuditoriaAdmin
            {
                AdminId = yo,
                Accion = AccionAdmin.ELIMINAR_REFUGIO,
                Entidad = "refugio",
                EntidadId = refugio.Id,
                Detalle = JsonSerializer.Serialize(new { refugio.Direccion, refugio.Nombre })
            });
        }

        await uow.SaveChangesAsync(ct);
    }
}
