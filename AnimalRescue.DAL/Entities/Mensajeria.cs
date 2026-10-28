namespace AnimalRescue.DAL.Entities;

public class Conversacion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid Usuario1Id { get; set; }
    public Guid Usuario2Id { get; set; }
    public Guid? PublicacionId { get; set; }
    public DateTime? Usuario1UltimaLectura { get; set; }
    public DateTime? Usuario2UltimaLectura { get; set; }
    public DateTime? Usuario1EliminadaEn { get; set; }
    public DateTime? Usuario2EliminadaEn { get; set; }
    public DateTime? FechaUltimoMensaje { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public Usuario Usuario1 { get; set; } = null!;
    public Usuario Usuario2 { get; set; } = null!;
    public Publicacion? Publicacion { get; set; }
    public List<Mensaje> Mensajes { get; set; } = [];
}

public class Mensaje
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversacionId { get; set; }
    public Guid RemitenteId { get; set; }
    public string? Contenido { get; set; }
    public string? ImagenUrl { get; set; }
    public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;

    public Conversacion Conversacion { get; set; } = null!;
    public Usuario Remitente { get; set; } = null!;
}
