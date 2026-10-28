using AnimalRescue.BLL.DTOs.Comun;
using AnimalRescue.DAL.Entities;

namespace AnimalRescue.BLL.DTOs.Publicaciones;

/// <summary>Card del feed y de Mis Mascotas.</summary>
public record PublicacionCardDto(
    Guid Id,
    string NombreMascota,
    string UbicacionTexto,
    DateOnly FechaPerdida,
    EstadoPublicacion Estado,
    string? ImagenUrl);

public record ImagenDto(Guid Id, string Url, string UrlMiniatura, short Orden);

public record PublicacionDetalleDto(
    Guid Id,
    string NombreMascota,
    string Color,
    Tamano Tamano,
    string? Edad,
    bool Castrado,
    bool TieneCollar,
    bool TieneChapitaQr,
    string? Descripcion,
    DateOnly FechaPerdida,
    decimal? Recompensa,
    string UbicacionTexto,
    double Latitud,
    double Longitud,
    EstadoPublicacion Estado,
    DateTime FechaCreacion,
    IReadOnlyList<ImagenDto> Imagenes,
    UsuarioResumenDto Autor,
    bool EsMia,
    bool YaReportada);

public record MarcadorMapaDto(Guid Id, string NombreMascota, string? MiniaturaUrl, double Latitud, double Longitud);

public record CrearPublicacionRequest(
    string NombreMascota,
    string Color,
    Tamano Tamano,
    string? Edad,
    bool Castrado,
    bool TieneCollar,
    bool TieneChapitaQr,
    string? Descripcion,
    DateOnly FechaPerdida,
    decimal? Recompensa,
    string UbicacionTexto,
    double? Latitud,
    double? Longitud);

/// <summary>Edición completa; Estado permite marcar la mascota como encontrada.</summary>
public record EditarPublicacionRequest(
    string NombreMascota,
    string Color,
    Tamano Tamano,
    string? Edad,
    bool Castrado,
    bool TieneCollar,
    bool TieneChapitaQr,
    string? Descripcion,
    DateOnly FechaPerdida,
    decimal? Recompensa,
    string UbicacionTexto,
    double? Latitud,
    double? Longitud,
    EstadoPublicacion Estado,
    IReadOnlyList<Guid>? EliminarImagenIds);

public record GeocodificacionDto(string Direccion, double Latitud, double Longitud);
