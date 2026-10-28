namespace AnimalRescue.DAL.Entities;

public class Publicacion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public string NombreMascota { get; set; } = null!;
    public string Color { get; set; } = null!;
    public Tamano Tamano { get; set; }
    public string? Edad { get; set; }
    public bool Castrado { get; set; }
    public bool TieneCollar { get; set; }
    public bool TieneChapitaQr { get; set; }
    public string? Descripcion { get; set; }
    public DateOnly FechaPerdida { get; set; }
    public decimal? Recompensa { get; set; }
    public string UbicacionTexto { get; set; } = null!;
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public EstadoPublicacion Estado { get; set; } = EstadoPublicacion.ACTIVA;
    public DateTime? FechaEliminacion { get; set; }
    public Guid? EliminadaPorId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

    public Usuario Usuario { get; set; } = null!;
    public Usuario? EliminadaPor { get; set; }
    public List<ImagenPublicacion> Imagenes { get; set; } = [];
}

public class ImagenPublicacion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PublicacionId { get; set; }
    public string Url { get; set; } = null!;
    public string UrlMiniatura { get; set; } = null!;
    public short Orden { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public Publicacion Publicacion { get; set; } = null!;
}
