using AnimalRescue.BLL.DTOs.Auth;
using AnimalRescue.BLL.Exceptions;
using AnimalRescue.BLL.Interfaces;
using AnimalRescue.BLL.Options;
using AnimalRescue.BLL.Validators;
using AnimalRescue.DAL.Entities;
using AnimalRescue.DAL.UnitOfWork;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AnimalRescue.BLL.Services;

public class AuthService(
    IUnitOfWork uow,
    TokenService tokens,
    ICurrentUser actual,
    IOptions<BootstrapOptions> bootstrap,
    IValidator<RegistroRequest> validarRegistro,
    IValidator<LoginRequest> validarLogin,
    IValidator<ActualizarPerfilRequest> validarPerfil) : IAuthService
{
    public async Task<AuthResponse> RegistrarAsync(RegistroRequest request, CancellationToken ct = default)
    {
        await validarRegistro.ValidarYLanzarAsync(request, ct);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await uow.Usuarios.QuerySinSeguimiento().AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("Ya existe una cuenta con ese correo electrónico.");

        var esSuperAdmin = !string.IsNullOrWhiteSpace(bootstrap.Value.SuperAdminEmail)
            && string.Equals(bootstrap.Value.SuperAdminEmail.Trim(), email, StringComparison.OrdinalIgnoreCase);

        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Rol = esSuperAdmin ? Rol.SUPERADMIN : Rol.USUARIO
        };
        uow.Usuarios.Agregar(usuario);

        try
        {
            return await EmitirSesionAsync(usuario, ct);
        }
        catch (ConflictoDatosException)
        {
            throw new ConflictException("Ya existe una cuenta con ese correo electrónico.");
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        await validarLogin.ValidarYLanzarAsync(request, ct);

        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await uow.Usuarios.Query().FirstOrDefaultAsync(u => u.Email == email, ct);

        // Mismo mensaje para correo inexistente y contraseña errónea.
        if (usuario is null || !usuario.Activo || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
            throw new CredencialesInvalidasException("Correo o contraseña incorrectos.");

        return await EmitirSesionAsync(usuario, ct);
    }

    public async Task<AuthResponse> RefrescarAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = TokenService.Hashear(refreshToken ?? string.Empty);
        var existente = await uow.RefreshTokens.Query()
            .Include(t => t.Usuario)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new CredencialesInvalidasException("La sesión no es válida.");

        if (existente.FechaRevocacion is not null)
        {
            // Reutilización de un token ya rotado: se asume robo y se cierran todas las sesiones.
            var activos = await uow.RefreshTokens.Query()
                .Where(t => t.UsuarioId == existente.UsuarioId && t.FechaRevocacion == null)
                .ToListAsync(ct);
            foreach (var t in activos) t.FechaRevocacion = DateTime.UtcNow;
            await uow.SaveChangesAsync(ct);
            throw new CredencialesInvalidasException("La sesión no es válida.");
        }

        if (existente.FechaExpiracion <= DateTime.UtcNow || !existente.Usuario.Activo)
            throw new CredencialesInvalidasException("La sesión expiró. Inicia sesión nuevamente.");

        existente.FechaRevocacion = DateTime.UtcNow;
        return await EmitirSesionAsync(existente.Usuario, ct, existente);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = TokenService.Hashear(refreshToken ?? string.Empty);
        var token = await uow.RefreshTokens.Query().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null || token.FechaRevocacion is not null) return;

        token.FechaRevocacion = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
    }

    public async Task<UsuarioDto> MiPerfilAsync(CancellationToken ct = default)
    {
        var usuario = await uow.Usuarios.QuerySinSeguimiento().FirstOrDefaultAsync(u => u.Id == actual.IdRequerido, ct)
            ?? throw new NotFoundException("La cuenta no existe.");
        return Mapear(usuario);
    }

    public async Task<UsuarioDto> ActualizarPerfilAsync(ActualizarPerfilRequest request, CancellationToken ct = default)
    {
        await validarPerfil.ValidarYLanzarAsync(request, ct);

        var usuario = await uow.Usuarios.Query().FirstOrDefaultAsync(u => u.Id == actual.IdRequerido, ct)
            ?? throw new NotFoundException("La cuenta no existe.");
        usuario.Nombre = request.Nombre.Trim();
        await uow.SaveChangesAsync(ct);
        return Mapear(usuario);
    }

    private async Task<AuthResponse> EmitirSesionAsync(Usuario usuario, CancellationToken ct, RefreshToken? reemplazado = null)
    {
        var (access, expira) = tokens.CrearAccessToken(usuario);
        var (valor, hash) = tokens.CrearRefreshToken();

        var nuevo = new RefreshToken { UsuarioId = usuario.Id, TokenHash = hash, FechaExpiracion = tokens.ExpiracionRefresh() };
        uow.RefreshTokens.Agregar(nuevo);
        if (reemplazado is not null) reemplazado.ReemplazadoPor = nuevo;

        await uow.SaveChangesAsync(ct);
        return new AuthResponse(access, valor, expira, Mapear(usuario));
    }

    private static UsuarioDto Mapear(Usuario u) => new(u.Id, u.Nombre, u.Email, u.Rol, u.FotoUrl);
}
