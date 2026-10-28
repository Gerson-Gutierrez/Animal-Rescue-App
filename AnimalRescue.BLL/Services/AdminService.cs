using System.Text.Json;
using AnimalRescue.BLL.DTOs.Admin;
using AnimalRescue.BLL.DTOs.Comun;
using AnimalRescue.BLL.DTOs.Reportes;
using AnimalRescue.BLL.Exceptions;
using AnimalRescue.BLL.Interfaces;
using AnimalRescue.BLL.Validators;
using AnimalRescue.DAL.Entities;
using AnimalRescue.DAL.UnitOfWork;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AnimalRescue.BLL.Services;

public class AdminService(
    IUnitOfWork uow,
    ICurrentUser actual,
    IValidator<ResolverReporteRequest> validarResolver,
    IValidator<CambiarRolRequest> validarRol) : IAdminService
{
    public async Task<PaginaDto<ReporteDto>> ReportesAsync(EstadoReporte? estado, int pagina, int tamano, CancellationToken ct = default)
    {
        ExigirAdmin();
        pagina = Math.Max(1, pagina);
        tamano = Math.Clamp(tamano, 1, 50);

        var consulta = uow.Reportes.QuerySinSeguimiento();
        if (estado.HasValue) consulta = consulta.Where(r => r.Estado == estado.Value);

        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderBy(r => r.Estado == EstadoReporte.PENDIENTE ? 0 : 1).ThenByDescending(r => r.FechaCreacion)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .Select(r => new ReporteDto(
                r.Id,
                r.PublicacionId,
                r.Publicacion.NombreMascota,
                r.Publicacion.Imagenes.OrderBy(i => i.Orden).Select(i => i.UrlMiniatura).FirstOrDefault(),
                r.Publicacion.Estado,
                r.Publicacion.Usuario.Nombre,
                r.ReportadoPor.Nombre,
                r.Motivo,
                r.Comentario,
                r.Estado,
                r.RevisadoPor == null ? null : r.RevisadoPor.Nombre,
                r.NotaRevision,
                r.FechaCreacion))
            .ToListAsync(ct);

        return new PaginaDto<ReporteDto>(items, pagina, tamano, total);
    }

    public async Task<ReporteDto> ResolverReporteAsync(Guid reporteId, ResolverReporteRequest request, CancellationToken ct = default)
    {
        var admin = ExigirAdmin();
        await validarResolver.ValidarYLanzarAsync(request, ct);

        var reporte = await uow.Reportes.Query()
            .Include(r => r.Publicacion)
            .FirstOrDefaultAsync(r => r.Id == reporteId, ct)
            ?? throw new NotFoundException("El reporte no existe.");

        if (reporte.Estado != EstadoReporte.PENDIENTE)
            throw new ConflictException("Este reporte ya fue revisado.");

        var ahora = DateTime.UtcNow;
        var nota = string.IsNullOrWhiteSpace(request.Nota) ? null : request.Nota.Trim();

        if (request.Accion == AccionReporte.ACEPTAR)
        {
            // Se elimina la publicación y se cierran todos los reportes pendientes sobre ella.
            var pendientes = await uow.Reportes.Query()
                .Where(r => r.PublicacionId == reporte.PublicacionId && r.Estado == EstadoReporte.PENDIENTE)
                .ToListAsync(ct);
            foreach (var r in pendientes) Cerrar(r, EstadoReporte.ACEPTADO, admin, ahora, nota);

            EliminarPublicacion(reporte.Publicacion, admin, ahora);
            Auditar(admin, AccionAdmin.ACEPTAR_REPORTE, "reporte", reporte.Id, new { reporte.PublicacionId, nota });
        }
        else
        {
            Cerrar(reporte, EstadoReporte.RECHAZADO, admin, ahora, nota);
            Auditar(admin, AccionAdmin.RECHAZAR_REPORTE, "reporte", reporte.Id, new { reporte.PublicacionId, nota });
        }

        await uow.SaveChangesAsync(ct);
        return (await ReportesPorIdAsync(reporteId, ct))!;
    }

    public async Task<PaginaDto<AdminPublicacionDto>> PublicacionesAsync(
        EstadoPublicacion? estado, string? busqueda, int pagina, int tamano, CancellationToken ct = default)
    {
        ExigirAdmin();
        pagina = Math.Max(1, pagina);
        tamano = Math.Clamp(tamano, 1, 50);

        var consulta = uow.Publicaciones.QuerySinSeguimiento();
        if (estado.HasValue) consulta = consulta.Where(p => p.Estado == estado.Value);
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var patron = $"%{busqueda.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            consulta = consulta.Where(p => EF.Functions.ILike(p.NombreMascota, patron)
                || EF.Functions.ILike(p.UbicacionTexto, patron)
                || EF.Functions.ILike(p.Usuario.Nombre, patron));
        }

        var total = await consulta.CountAsync(ct);
        var filas = await consulta
            .OrderByDescending(p => p.FechaCreacion)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .Select(p => new
            {
                p.Id,
                p.NombreMascota,
                p.UbicacionTexto,
                Imagen = p.Imagenes.OrderBy(i => i.Orden).Select(i => i.UrlMiniatura).FirstOrDefault(),
                p.Estado,
                AutorNombre = p.Usuario.Nombre,
                AutorEmail = p.Usuario.Email,
                p.FechaCreacion
            })
            .ToListAsync(ct);

        var ids = filas.Select(f => f.Id).ToList();
        var pendientes = await uow.Reportes.QuerySinSeguimiento()
            .Where(r => ids.Contains(r.PublicacionId) && r.Estado == EstadoReporte.PENDIENTE)
            .GroupBy(r => r.PublicacionId)
            .Select(g => new { Id = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Total, ct);

        var items = filas.Select(f => new AdminPublicacionDto(
            f.Id, f.NombreMascota, f.UbicacionTexto, f.Imagen, f.Estado, f.AutorNombre, f.AutorEmail,
            pendientes.GetValueOrDefault(f.Id), f.FechaCreacion)).ToList();

        return new PaginaDto<AdminPublicacionDto>(items, pagina, tamano, total);
    }

    public async Task EliminarPublicacionAsync(Guid id, CancellationToken ct = default)
    {
        var admin = ExigirAdmin();
        var publicacion = await uow.Publicaciones.Query()
            .FirstOrDefaultAsync(p => p.Id == id && p.Estado != EstadoPublicacion.ELIMINADA, ct)
            ?? throw new NotFoundException("La publicación no existe.");

        var ahora = DateTime.UtcNow;
        var pendientes = await uow.Reportes.Query()
            .Where(r => r.PublicacionId == id && r.Estado == EstadoReporte.PENDIENTE)
            .ToListAsync(ct);
        foreach (var r in pendientes) Cerrar(r, EstadoReporte.ACEPTADO, admin, ahora, "Publicación eliminada por un administrador.");

        EliminarPublicacion(publicacion, admin, ahora);
        Auditar(admin, AccionAdmin.ELIMINAR_PUBLICACION, "publicacion", id, new { publicacion.NombreMascota, publicacion.UsuarioId });
        await uow.SaveChangesAsync(ct);
    }

    public async Task<PaginaDto<UsuarioAdminDto>> UsuariosAsync(string? busqueda, int pagina, int tamano, CancellationToken ct = default)
    {
        ExigirSuperAdmin();
        pagina = Math.Max(1, pagina);
        tamano = Math.Clamp(tamano, 1, 50);

        var consulta = uow.Usuarios.QuerySinSeguimiento();
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var patron = $"%{busqueda.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            consulta = consulta.Where(u => EF.Functions.ILike(u.Nombre, patron) || EF.Functions.ILike(u.Email, patron));
        }

        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderBy(u => u.Nombre)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .Select(u => new UsuarioAdminDto(u.Id, u.Nombre, u.Email, u.Rol, u.Activo, u.FechaCreacion))
            .ToListAsync(ct);

        return new PaginaDto<UsuarioAdminDto>(items, pagina, tamano, total);
    }

    public async Task<UsuarioAdminDto> CambiarRolAsync(Guid usuarioId, CambiarRolRequest request, CancellationToken ct = default)
    {
        var superAdmin = ExigirSuperAdmin();
        await validarRol.ValidarYLanzarAsync(request, ct);

        var usuario = await uow.Usuarios.Query().FirstOrDefaultAsync(u => u.Id == usuarioId, ct)
            ?? throw new NotFoundException("El usuario no existe.");

        if (usuario.Rol == Rol.SUPERADMIN)
            throw new ReglaNegocioException("No se puede modificar el rol de un superadministrador.");

        if (usuario.Rol != request.Rol)
        {
            usuario.Rol = request.Rol;
            usuario.PromovidoPorId = request.Rol == Rol.ADMIN ? superAdmin : null;
            Auditar(superAdmin, request.Rol == Rol.ADMIN ? AccionAdmin.PROMOVER_ADMIN : AccionAdmin.REVOCAR_ADMIN,
                "usuario", usuario.Id, new { usuario.Email });
            await uow.SaveChangesAsync(ct);
        }

        return new UsuarioAdminDto(usuario.Id, usuario.Nombre, usuario.Email, usuario.Rol, usuario.Activo, usuario.FechaCreacion);
    }

    private Guid ExigirAdmin()
    {
        var id = actual.IdRequerido;
        if (!actual.EsAdmin) throw new ForbiddenException("Se requiere rol de administrador.");
        return id;
    }

    private Guid ExigirSuperAdmin()
    {
        var id = actual.IdRequerido;
        if (!actual.EsSuperAdmin) throw new ForbiddenException("Se requiere rol de superadministrador.");
        return id;
    }

    private static void Cerrar(Reporte r, EstadoReporte estado, Guid adminId, DateTime fecha, string? nota)
    {
        r.Estado = estado;
        r.RevisadoPorId = adminId;
        r.FechaRevision = fecha;
        r.NotaRevision = nota;
    }

    private static void EliminarPublicacion(Publicacion p, Guid adminId, DateTime fecha)
    {
        p.Estado = EstadoPublicacion.ELIMINADA;
        p.FechaEliminacion = fecha;
        p.EliminadaPorId = adminId;
    }

    private void Auditar(Guid adminId, AccionAdmin accion, string entidad, Guid entidadId, object detalle) =>
        uow.Auditoria.Agregar(new AuditoriaAdmin
        {
            AdminId = adminId,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            Detalle = JsonSerializer.Serialize(detalle)
        });

    private Task<ReporteDto?> ReportesPorIdAsync(Guid id, CancellationToken ct) =>
        uow.Reportes.QuerySinSeguimiento()
            .Where(r => r.Id == id)
            .Select(r => new ReporteDto(
                r.Id,
                r.PublicacionId,
                r.Publicacion.NombreMascota,
                r.Publicacion.Imagenes.OrderBy(i => i.Orden).Select(i => i.UrlMiniatura).FirstOrDefault(),
                r.Publicacion.Estado,
                r.Publicacion.Usuario.Nombre,
                r.ReportadoPor.Nombre,
                r.Motivo,
                r.Comentario,
                r.Estado,
                r.RevisadoPor == null ? null : r.RevisadoPor.Nombre,
                r.NotaRevision,
                r.FechaCreacion))
            .FirstOrDefaultAsync(ct);
}
