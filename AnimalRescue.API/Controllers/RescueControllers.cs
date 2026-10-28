using AnimalRescue.BLL.DTOs.Rescue;
using AnimalRescue.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnimalRescue.API.Controllers;

[ApiController]
[Route("api/hogares-transito")]
[Authorize]
public class HogaresTransitoController(IHogarTransitoService hogares) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<HogarTransitoDto>> Listar(CancellationToken ct) => hogares.ListarAsync(ct);

    [HttpGet("mio")]
    public async Task<ActionResult<HogarTransitoDto>> Mio(CancellationToken ct)
    {
        var hogar = await hogares.MioAsync(ct);
        return hogar is null ? NotFound() : hogar;
    }

    [HttpPost]
    public async Task<ActionResult<HogarTransitoDto>> Crear(CrearHogarRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await hogares.CrearAsync(request, ct));

    [HttpDelete("mio")]
    public async Task<IActionResult> EliminarMio(CancellationToken ct)
    {
        await hogares.EliminarMioAsync(ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/refugios")]
public class RefugiosController(IRefugioService refugios) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<RefugioDto>> Listar(CancellationToken ct) => refugios.ListarAsync(ct);

    /// <summary>Geocodifica la dirección y agrega el refugio al mapa.</summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<RefugioDto>> Crear(CrearRefugioRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await refugios.CrearAsync(request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await refugios.EliminarAsync(id, ct);
        return NoContent();
    }
}
