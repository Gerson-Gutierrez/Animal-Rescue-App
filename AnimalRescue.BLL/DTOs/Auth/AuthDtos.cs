using AnimalRescue.DAL.Entities;

namespace AnimalRescue.BLL.DTOs.Auth;

public record RegistroRequest(string Nombre, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record ActualizarPerfilRequest(string Nombre);

public record UsuarioDto(Guid Id, string Nombre, string Email, Rol Rol, string? FotoUrl);

public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiraEn, UsuarioDto Usuario);
