namespace AnimalRescue.DAL.External.Storage;

public interface IAlmacenamientoArchivos
{
    /// <summary>Guarda el contenido y devuelve la URL pública relativa (p. ej. /uploads/publicaciones/abc.jpg).</summary>
    Task<string> GuardarAsync(Stream contenido, string carpeta, string extension, CancellationToken ct = default);

    /// <summary>Elimina el archivo si existe; las URLs ajenas o inexistentes se ignoran.</summary>
    Task EliminarAsync(string? url, CancellationToken ct = default);
}

public class StorageOptions
{
    public const string Seccion = "Storage";

    /// <summary>Carpeta física donde se guardan los archivos (relativa al directorio de la API o absoluta).</summary>
    public string RootPath { get; set; } = "uploads";

    /// <summary>Prefijo de URL con el que se sirven los archivos.</summary>
    public string RequestPath { get; set; } = "/uploads";
}
