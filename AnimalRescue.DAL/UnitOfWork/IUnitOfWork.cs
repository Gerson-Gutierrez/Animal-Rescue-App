using AnimalRescue.DAL.Entities;
using AnimalRescue.DAL.Repositories;

namespace AnimalRescue.DAL.UnitOfWork;

/// <summary>Se lanza cuando una restricción UNIQUE de la base de datos rechaza el guardado.</summary>
public class ConflictoDatosException(string restriccion, Exception inner)
    : Exception($"Violación de unicidad: {restriccion}", inner)
{
    public string Restriccion { get; } = restriccion;
}

public interface IUnitOfWork
{
    IRepository<Usuario> Usuarios { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    IRepository<Publicacion> Publicaciones { get; }
    IRepository<ImagenPublicacion> Imagenes { get; }
    IRepository<Conversacion> Conversaciones { get; }
    IRepository<Mensaje> Mensajes { get; }
    IRepository<HogarTransito> HogaresTransito { get; }
    IRepository<Refugio> Refugios { get; }
    IRepository<Reporte> Reportes { get; }
    IRepository<AuditoriaAdmin> Auditoria { get; }

    /// <exception cref="ConflictoDatosException">Violación de una restricción UNIQUE.</exception>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
