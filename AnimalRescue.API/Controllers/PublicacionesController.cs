using AnimalRescue.API.Models;
using AnimalRescue.BLL.DTOs.Comun;
using AnimalRescue.BLL.DTOs.Publicaciones;
using AnimalRescue.BLL.DTOs.Reportes;
using AnimalRescue.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AnimalRescue.API.Controllers;

[ApiController]
[Route("api/publicaciones")]
public class PublicacionesController(IPublicacionService publicaciones, IReporteService reportes) : ControllerBase
{
    [HttpGet]
    public Task<PaginaDto<PublicacionCardDto>> Feed(
        [FromQuery] int pagina = 1, [FromQuery] int tamano = 20, [FromQuery] string? q = null, CancellationToken ct = default) =>
        publicaciones.FeedAsync(pagina, tamano, q, ct);

    [HttpGet("mapa")]
    public Task<IReadOnlyList<MarcadorMapaDto>> Mapa(
        [FromQuery] double? norte, [FromQuery] double? sur, [FromQuery] double? este, [FromQuery] double? oeste,
        CancellationToken ct) =>
        publicaciones.MapaAsync(norte, sur, este, oeste, ct);

    [HttpGet("mias")]
    [Authorize]
    public Task<IReadOnlyList<PublicacionCardDto>> Mias(CancellationToken ct) => publicaciones.MiasAsync(ct);

    [HttpGet("{id:guid}")]
    public Task<PublicacionDetalleDto> Detalle(Guid id, CancellationToken ct) => publicaciones.DetalleAsync(id, ct);

    [HttpPost]
    [Authorize]
    [RequestSizeLimit(40_000_000)]
    public async Task<ActionResult<PublicacionDetalleDto>> Crear([FromForm] CrearPublicacionForm form, CancellationToken ct)
    {
        var creada = await publicaciones.CrearAsync(form.ToRequest(), form.Imagenes.ParaNegocio(), ct);
        return CreatedAtAction(nameof(Detalle), new { id = creada.Id }, creada);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [RequestSizeLimit(40_000_000)]
    public Task<PublicacionDetalleDto> Editar(Guid id, [FromForm] EditarPublicacionForm form, CancellationToken ct) =>
        publicaciones.EditarAsync(id, form.ToRequest(), form.NuevasImagenes.ParaNegocio(), ct);

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await publicaciones.EliminarAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reportes")]
    [Authorize]
    public async Task<IActionResult> Reportar(Guid id, CrearReporteRequest request, CancellationToken ct)
    {
        await reportes.CrearAsync(id, request, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/geocodificacion")]
[Authorize]
[EnableRateLimiting("geocodificacion")]
public class GeocodificacionController(IGeocodificacionService geocodificacion) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<GeocodificacionDto>> Buscar([FromQuery] string q, CancellationToken ct) =>
        geocodificacion.BuscarAsync(q ?? string.Empty, ct);
}
