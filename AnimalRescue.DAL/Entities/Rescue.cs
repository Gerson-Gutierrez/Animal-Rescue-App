namespace AnimalRescue.DAL.Entities;

public class HogarTransito
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public string Nombre { get; set; } = null!;
    public string Telefono { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Ciudad { get; set; } = null!;
    public string Pais { get; set; } = null!;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public Usuario Usuario { get; set; } = null!;
}

public class Refugio
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Nombre { get; set; }
    public string Direccion { get; set; } = null!;
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public Guid? CreadoPorId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public Usuario? CreadoPor { get; set; }
}
