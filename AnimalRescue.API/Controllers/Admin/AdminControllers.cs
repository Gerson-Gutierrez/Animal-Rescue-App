using AnimalRescue.API.Auth;
using AnimalRescue.BLL.DTOs.Admin;
using AnimalRescue.BLL.DTOs.Comun;
using AnimalRescue.BLL.DTOs.Reportes;
using AnimalRescue.BLL.Interfaces;
using AnimalRescue.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnimalRescue.API.Controllers.Admin;

[ApiController]
[Route("api/admin/reportes")]
[Authorize(Policy = Policies.EsAdmin)]
public class AdminReportesController(IAdminService admin) : ControllerBase
{
    [HttpGet]
    public Task<PaginaDto<ReporteDto>> Listar(
        [FromQuery] EstadoReporte? estado, [FromQuery] int pagina = 1, [FromQuery] int tamano = 20, CancellationToken ct = default) =>
        admin.ReportesAsync(estado, pagina, tamano, ct);

    [HttpPut("{id:guid}")]
    public Task<ReporteDto> Resolver(Guid id, ResolverReporteRequest request, CancellationToken ct) =>
        admin.ResolverReporteAsync(id, request, ct);
}

[ApiController]
[Route("api/admin/publicaciones")]
[Authorize(Policy = Policies.EsAdmin)]
public class AdminPublicacionesController(IAdminService admin) : ControllerBase
{
    [HttpGet]
    public Task<PaginaDto<AdminPublicacionDto>> Listar(
        [FromQuery] EstadoPublicacion? estado, [FromQuery] string? q, [FromQuery] int pagina = 1, [FromQuery] int tamano = 20,
        CancellationToken ct = default) =>
        admin.PublicacionesAsync(estado, q, pagina, tamano, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await admin.EliminarPublicacionAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/admin/usuarios")]
[Authorize(Policy = Policies.EsSuperAdmin)]
public class AdminUsuariosController(IAdminService admin) : ControllerBase
{
    [HttpGet]
    public Task<PaginaDto<UsuarioAdminDto>> Buscar(
        [FromQuery] string? q, [FromQuery] int pagina = 1, [FromQuery] int tamano = 20, CancellationToken ct = default) =>
        admin.UsuariosAsync(q, pagina, tamano, ct);

    [HttpPut("{id:guid}/rol")]
    public Task<UsuarioAdminDto> CambiarRol(Guid id, CambiarRolRequest request, CancellationToken ct) =>
        admin.CambiarRolAsync(id, request, ct);
}
