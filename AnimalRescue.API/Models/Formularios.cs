using AnimalRescue.BLL.DTOs.Comun;
using AnimalRescue.BLL.DTOs.Publicaciones;
using AnimalRescue.DAL.Entities;

namespace AnimalRescue.API.Models;

public static class FormFileExtensions
{
    public static ArchivoSubido ParaNegocio(this IFormFile archivo) =>
        new(archivo.OpenReadStream(), archivo.FileName, archivo.Length);

    public static IReadOnlyList<ArchivoSubido> ParaNegocio(this IFormFileCollection? archivos) =>
        archivos?.Select(a => a.ParaNegocio()).ToList() ?? [];
}

public class CrearPublicacionForm
{
    public string NombreMascota { get; set; } = "";
    public string Color { get; set; } = "";
    public Tamano Tamano { get; set; }
    public string? Edad { get; set; }
    public bool Castrado { get; set; }
    public bool TieneCollar { get; set; }
    public bool TieneChapitaQr { get; set; }
    public string? Descripcion { get; set; }
    public DateOnly FechaPerdida { get; set; }
    public decimal? Recompensa { get; set; }
    public string UbicacionTexto { get; set; } = "";
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public IFormFileCollection? Imagenes { get; set; }

    public CrearPublicacionRequest ToRequest() => new(
        NombreMascota, Color, Tamano, Edad, Castrado, TieneCollar, TieneChapitaQr,
        Descripcion, FechaPerdida, Recompensa, UbicacionTexto, Latitud, Longitud);
}

public class EditarPublicacionForm
{
    public string NombreMascota { get; set; } = "";
    public string Color { get; set; } = "";
    public Tamano Tamano { get; set; }
    public string? Edad { get; set; }
    public bool Castrado { get; set; }
    public bool TieneCollar { get; set; }
    public bool TieneChapitaQr { get; set; }
    public string? Descripcion { get; set; }
    public DateOnly FechaPerdida { get; set; }
    public decimal? Recompensa { get; set; }
    public string UbicacionTexto { get; set; } = "";
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public EstadoPublicacion Estado { get; set; } = EstadoPublicacion.ACTIVA;
    public List<Guid>? EliminarImagenIds { get; set; }
    public IFormFileCollection? NuevasImagenes { get; set; }

    public EditarPublicacionRequest ToRequest() => new(
        NombreMascota, Color, Tamano, Edad, Castrado, TieneCollar, TieneChapitaQr,
        Descripcion, FechaPerdida, Recompensa, UbicacionTexto, Latitud, Longitud, Estado, EliminarImagenIds);
}

public class EnviarMensajeForm
{
    public string? Contenido { get; set; }
    public IFormFile? Imagen { get; set; }
}
