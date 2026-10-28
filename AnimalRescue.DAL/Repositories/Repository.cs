using AnimalRescue.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace AnimalRescue.DAL.Repositories;

internal class Repository<T>(AnimalRescueDbContext db) : IRepository<T> where T : class
{
    private readonly DbSet<T> _set = db.Set<T>();

    public IQueryable<T> Query() => _set;

    public IQueryable<T> QuerySinSeguimiento() => _set.AsNoTracking();

    public async Task<T?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        await _set.FindAsync([id], ct);

    public void Agregar(T entidad) => _set.Add(entidad);

    public void Eliminar(T entidad) => _set.Remove(entidad);

    public void EliminarVarios(IEnumerable<T> entidades) => _set.RemoveRange(entidades);
}
