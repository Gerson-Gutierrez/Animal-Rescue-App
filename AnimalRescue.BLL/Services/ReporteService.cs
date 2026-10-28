using AnimalRescue.BLL.DTOs.Reportes;
using AnimalRescue.BLL.Exceptions;
using AnimalRescue.BLL.Interfaces;
using AnimalRescue.BLL.Validators;
using AnimalRescue.DAL.Entities;
using AnimalRescue.DAL.UnitOfWork;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AnimalRescue.BLL.Services;

public class ReporteService(
    IUnitOfWork uow,
    ICurrentUser actual,
    IValidator<CrearReporteRequest> validar) : IReporteService
{
    public async Task CrearAsync(Guid publicacionId, CrearReporteRequest request, CancellationToken ct = default)
    {
        var yo = actual.IdRequerido;
        await validar.ValidarYLanzarAsync(request, ct);

        var publicacion = await uow.Publicaciones.QuerySinSeguimiento()
            .Where(p => p.Id == publicacionId && p.Estado != EstadoPublicacion.ELIMINADA)
            .Select(p => new { p.UsuarioId })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("La publicación no existe.");

        if (publicacion.UsuarioId == yo)
            throw new ReglaNegocioException("No puedes reportar tu propia publicación.");

        if (await uow.Reportes.QuerySinSeguimiento().AnyAsync(r => r.PublicacionId == publicacionId && r.ReportadoPorId == yo, ct))
            throw new ConflictException("Ya reportaste esta publicación.");

        uow.Reportes.Agregar(new Reporte
        {
            PublicacionId = publicacionId,
            ReportadoPorId = yo,
            Motivo = request.Motivo,
            Comentario = string.IsNullOrWhiteSpace(request.Comentario) ? null : request.Comentario.Trim()
        });

        try
        {
            await uow.SaveChangesAsync(ct);
        }
        catch (ConflictoDatosException)
        {
            throw new ConflictException("Ya reportaste esta publicación.");
        }
    }
}
