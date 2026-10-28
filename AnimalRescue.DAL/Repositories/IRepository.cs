namespace AnimalRescue.DAL.Repositories;

public interface IRepository<T> where T : class
{
    /// <summary>Consulta con seguimiento de cambios (para modificar entidades).</summary>
    IQueryable<T> Query();

    /// <summary>Consulta de sólo lectura.</summary>
    IQueryable<T> QuerySinSeguimiento();

    Task<T?> ObtenerAsync(Guid id, CancellationToken ct = default);
    void Agregar(T entidad);
    void Eliminar(T entidad);
    void EliminarVarios(IEnumerable<T> entidades);
}
