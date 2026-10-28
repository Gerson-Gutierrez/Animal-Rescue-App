namespace AnimalRescue.BLL.DTOs.Comun;

public record PaginaDto<T>(IReadOnlyList<T> Items, int Pagina, int Tamano, int Total)
{
    public bool HayMas => Pagina * Tamano < Total;
}

public record UsuarioResumenDto(Guid Id, string Nombre, string? FotoUrl);

/// <summary>Archivo recibido por la API, desacoplado de ASP.NET.</summary>
public sealed class ArchivoSubido(Stream contenido, string nombre, long longitud) : IDisposable
{
    public Stream Contenido { get; } = contenido;
    public string Nombre { get; } = nombre;
    public long Longitud { get; } = longitud;

    public void Dispose() => Contenido.Dispose();
}
