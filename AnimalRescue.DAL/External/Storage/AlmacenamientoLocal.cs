using Microsoft.Extensions.Options;

namespace AnimalRescue.DAL.External.Storage;

internal class AlmacenamientoLocal(IOptions<StorageOptions> options) : IAlmacenamientoArchivos
{
    private readonly string _raiz = Path.GetFullPath(options.Value.RootPath);
    private readonly string _prefijo = options.Value.RequestPath.TrimEnd('/');

    public async Task<string> GuardarAsync(Stream contenido, string carpeta, string extension, CancellationToken ct = default)
    {
        var nombre = $"{Guid.NewGuid():N}{extension}";
        var directorio = Path.Combine(_raiz, carpeta);
        Directory.CreateDirectory(directorio);

        await using var archivo = File.Create(Path.Combine(directorio, nombre));
        await contenido.CopyToAsync(archivo, ct);

        return $"{_prefijo}/{carpeta}/{nombre}";
    }

    public Task EliminarAsync(string? url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith(_prefijo + "/", StringComparison.Ordinal))
            return Task.CompletedTask;

        var relativa = url[(_prefijo.Length + 1)..].Replace('/', Path.DirectorySeparatorChar);
        var ruta = Path.GetFullPath(Path.Combine(_raiz, relativa));

        // Evita salir de la carpeta raíz con rutas como ../../
        if (ruta.StartsWith(_raiz, StringComparison.OrdinalIgnoreCase) && File.Exists(ruta))
            File.Delete(ruta);

        return Task.CompletedTask;
    }
}
