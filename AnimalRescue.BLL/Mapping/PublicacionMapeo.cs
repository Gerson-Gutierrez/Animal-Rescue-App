using System.Linq.Expressions;
using AnimalRescue.BLL.DTOs.Comun;
using AnimalRescue.BLL.DTOs.Publicaciones;
using AnimalRescue.DAL.Entities;

namespace AnimalRescue.BLL.Mapping;

internal static class PublicacionMapeo
{
    /// <summary>Proyección SQL de la card (imagen principal = menor orden).</summary>
    public static readonly Expression<Func<Publicacion, PublicacionCardDto>> Card = p => new PublicacionCardDto(
        p.Id,
        p.NombreMascota,
        p.UbicacionTexto,
        p.FechaPerdida,
        p.Estado,
        p.Imagenes.OrderBy(i => i.Orden).Select(i => i.UrlMiniatura).FirstOrDefault());

    /// <summary>Requiere cargar <c>Imagenes</c> y <c>Usuario</c>.</summary>
    public static PublicacionDetalleDto Detalle(Publicacion p, bool esMia, bool yaReportada) => new(
        p.Id,
        p.NombreMascota,
        p.Color,
        p.Tamano,
        p.Edad,
        p.Castrado,
        p.TieneCollar,
        p.TieneChapitaQr,
        p.Descripcion,
        p.FechaPerdida,
        p.Recompensa,
        p.UbicacionTexto,
        p.Latitud,
        p.Longitud,
        p.Estado,
        p.FechaCreacion,
        p.Imagenes.OrderBy(i => i.Orden).Select(i => new ImagenDto(i.Id, i.Url, i.UrlMiniatura, i.Orden)).ToList(),
        new UsuarioResumenDto(p.Usuario.Id, p.Usuario.Nombre, p.Usuario.FotoUrl),
        esMia,
        yaReportada);
}
