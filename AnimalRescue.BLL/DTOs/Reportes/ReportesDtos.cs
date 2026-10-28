using AnimalRescue.DAL.Entities;

namespace AnimalRescue.BLL.DTOs.Reportes;

public record CrearReporteRequest(MotivoReporte Motivo, string? Comentario);

public enum AccionReporte { ACEPTAR, RECHAZAR }

public record ResolverReporteRequest(AccionReporte Accion, string? Nota);

public record ReporteDto(
    Guid Id,
    Guid PublicacionId,
    string PublicacionNombre,
    string? PublicacionImagenUrl,
    EstadoPublicacion PublicacionEstado,
    string AutorNombre,
    string ReportadoPorNombre,
    MotivoReporte Motivo,
    string? Comentario,
    EstadoReporte Estado,
    string? RevisadoPorNombre,
    string? NotaRevision,
    DateTime FechaCreacion);
