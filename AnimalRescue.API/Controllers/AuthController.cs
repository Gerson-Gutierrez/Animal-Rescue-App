using AnimalRescue.BLL.DTOs.Auth;
using AnimalRescue.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AnimalRescue.API.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("registro")]
    public async Task<ActionResult<AuthResponse>> Registro(RegistroRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await auth.RegistrarAsync(request, ct));

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct) =>
        await auth.LoginAsync(request, ct);

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken ct) =>
        await auth.RefrescarAsync(request.RefreshToken, ct);

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        await auth.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuariosController(IAuthService auth) : ControllerBase
{
    [HttpGet("me")]
    public Task<UsuarioDto> Me(CancellationToken ct) => auth.MiPerfilAsync(ct);

    [HttpPut("me")]
    public Task<UsuarioDto> Actualizar(ActualizarPerfilRequest request, CancellationToken ct) =>
        auth.ActualizarPerfilAsync(request, ct);
}
