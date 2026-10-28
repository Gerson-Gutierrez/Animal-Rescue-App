namespace AnimalRescue.DAL.Entities;

public class Reporte
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PublicacionId { get; set; }
    public Guid ReportadoPorId { get; set; }
    public MotivoReporte Motivo { get; set; }
    public string? Comentario { get; set; }
    public EstadoReporte Estado { get; set; } = EstadoReporte.PENDIENTE;
    public Guid? RevisadoPorId { get; set; }
    public DateTime? FechaRevision { get; set; }
    public string? NotaRevision { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public Publicacion Publicacion { get; set; } = null!;
    public Usuario ReportadoPor { get; set; } = null!;
    public Usuario? RevisadoPor { get; set; }
}

public class AuditoriaAdmin
{
    public long Id { get; set; }
    public Guid? AdminId { get; set; }
    public AccionAdmin Accion { get; set; }
    public string Entidad { get; set; } = null!;
    public Guid? EntidadId { get; set; }
    public string? Detalle { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public Usuario? Admin { get; set; }
}
