using AnimalRescue.DAL.Entities;

namespace AnimalRescue.BLL.DTOs.Admin;

public record AdminPublicacionDto(
    Guid Id,
    string NombreMascota,
    string UbicacionTexto,
    string? ImagenUrl,
    EstadoPublicacion Estado,
    string AutorNombre,
    string AutorEmail,
    int ReportesPendientes,
    DateTime FechaCreacion);

public record UsuarioAdminDto(Guid Id, string Nombre, string Email, Rol Rol, bool Activo, DateTime FechaCreacion);

public record CambiarRolRequest(Rol Rol);
