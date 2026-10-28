namespace AnimalRescue.DAL.Entities;

public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public Rol Rol { get; set; } = Rol.USUARIO;
    public string? FotoUrl { get; set; }
    public bool Activo { get; set; } = true;
    public Guid? PromovidoPorId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

    public Usuario? PromovidoPor { get; set; }
}

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime FechaExpiracion { get; set; }
    public DateTime? FechaRevocacion { get; set; }
    public Guid? ReemplazadoPorId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public Usuario Usuario { get; set; } = null!;
    public RefreshToken? ReemplazadoPor { get; set; }

    public bool EstaActivo => FechaRevocacion is null && FechaExpiracion > DateTime.UtcNow;
}
