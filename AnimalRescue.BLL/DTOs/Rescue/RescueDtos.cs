namespace AnimalRescue.BLL.DTOs.Rescue;

public record CrearHogarRequest(string Nombre, string Telefono, string Email, string Ciudad, string Pais);

public record HogarTransitoDto(
    Guid Id,
    string Nombre,
    string Telefono,
    string Email,
    string Ciudad,
    string Pais,
    bool EsMio,
    DateTime FechaCreacion);

public record CrearRefugioRequest(string Direccion, string? Nombre);

public record RefugioDto(
    Guid Id,
    string? Nombre,
    string Direccion,
    double Latitud,
    double Longitud,
    bool PuedeEliminar);
