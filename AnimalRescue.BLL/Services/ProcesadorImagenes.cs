using AnimalRescue.BLL.DTOs.Comun;
using AnimalRescue.BLL.Exceptions;
using AnimalRescue.DAL.External.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace AnimalRescue.BLL.Services;

public record ImagenGuardada(string Url, string UrlMiniatura);

/// <summary>Valida, redimensiona y guarda las imágenes subidas por los usuarios.</summary>
public class ProcesadorImagenes(IAlmacenamientoArchivos almacenamiento)
{
    public const long TamanoMaximoBytes = 5 * 1024 * 1024;
    private const int LadoMaximo = 1280;
    private const int LadoMiniatura = 480;
    private const long PixelesMaximos = 40_000_000;

    public async Task<ImagenGuardada> GuardarAsync(ArchivoSubido archivo, string carpeta, CancellationToken ct = default)
    {
        if (archivo.Longitud is <= 0 or > TamanoMaximoBytes)
            throw new ReglaNegocioException($"La imagen '{archivo.Nombre}' debe pesar entre 1 byte y 5 MB.");

        // Se copia a memoria para poder leerla más de una vez (detección de formato + decodificación).
        using var buffer = new MemoryStream();
        await archivo.Contenido.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        ImageInfo info;
        try
        {
            info = await Image.IdentifyAsync(buffer, ct);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            throw new ReglaNegocioException($"'{archivo.Nombre}' no es una imagen válida.");
        }

        var formato = info.Metadata.DecodedImageFormat;
        if (formato is not (JpegFormat or PngFormat or WebpFormat))
            throw new ReglaNegocioException($"'{archivo.Nombre}': sólo se aceptan imágenes JPG, PNG o WebP.");
        if ((long)info.Width * info.Height > PixelesMaximos)
            throw new ReglaNegocioException($"'{archivo.Nombre}' tiene una resolución demasiado grande.");

        buffer.Position = 0;
        using var imagen = await Image.LoadAsync(buffer, ct);
        imagen.Mutate(x => x.AutoOrient());

        var url = await GuardarVersionAsync(imagen, LadoMaximo, carpeta, ct);
        var miniatura = await GuardarVersionAsync(imagen, LadoMiniatura, carpeta, ct);
        return new ImagenGuardada(url, miniatura);
    }

    private async Task<string> GuardarVersionAsync(Image original, int ladoMaximo, string carpeta, CancellationToken ct)
    {
        using var copia = original.Clone(x => x.Resize(new ResizeOptions
        {
            Size = new Size(ladoMaximo, ladoMaximo),
            Mode = ResizeMode.Max
        }));

        using var salida = new MemoryStream();
        await copia.SaveAsJpegAsync(salida, new JpegEncoder { Quality = 82 }, ct);
        salida.Position = 0;
        return await almacenamiento.GuardarAsync(salida, carpeta, ".jpg", ct);
    }
}
