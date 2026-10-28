using AnimalRescue.DAL.Context;
using AnimalRescue.DAL.Entities;
using AnimalRescue.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AnimalRescue.DAL.UnitOfWork;

internal class UnitOfWork(AnimalRescueDbContext db) : IUnitOfWork
{
    public IRepository<Usuario> Usuarios { get; } = new Repository<Usuario>(db);
    public IRepository<RefreshToken> RefreshTokens { get; } = new Repository<RefreshToken>(db);
    public IRepository<Publicacion> Publicaciones { get; } = new Repository<Publicacion>(db);
    public IRepository<ImagenPublicacion> Imagenes { get; } = new Repository<ImagenPublicacion>(db);
    public IRepository<Conversacion> Conversaciones { get; } = new Repository<Conversacion>(db);
    public IRepository<Mensaje> Mensajes { get; } = new Repository<Mensaje>(db);
    public IRepository<HogarTransito> HogaresTransito { get; } = new Repository<HogarTransito>(db);
    public IRepository<Refugio> Refugios { get; } = new Repository<Refugio>(db);
    public IRepository<Reporte> Reportes { get; } = new Repository<Reporte>(db);
    public IRepository<AuditoriaAdmin> Auditoria { get; } = new Repository<AuditoriaAdmin>(db);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            throw new ConflictoDatosException(pg.ConstraintName ?? "desconocida", ex);
        }
    }
}
