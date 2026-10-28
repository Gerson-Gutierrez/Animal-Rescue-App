using AnimalRescue.API.Models;
using AnimalRescue.BLL.DTOs.Mensajeria;
using AnimalRescue.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnimalRescue.API.Controllers;

[ApiController]
[Route("api/conversaciones")]
[Authorize]
public class ConversacionesController(IMensajeriaService mensajeria) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ConversacionResumenDto>> Listar(CancellationToken ct) => mensajeria.ListarAsync(ct);

    /// <summary>Obtiene o crea el chat con otro usuario (botón "Enviar mensaje").</summary>
    [HttpPost]
    public Task<ConversacionResumenDto> Abrir(AbrirConversacionRequest request, CancellationToken ct) =>
        mensajeria.AbrirAsync(request, ct);

    [HttpGet("{id:guid}/mensajes")]
    public Task<IReadOnlyList<MensajeDto>> Mensajes(
        Guid id, [FromQuery] DateTime? antesDe, [FromQuery] int limite = 30, CancellationToken ct = default) =>
        mensajeria.MensajesAsync(id, antesDe?.ToUniversalTime(), limite, ct);

    [HttpPost("{id:guid}/mensajes")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<MensajeDto>> Enviar(Guid id, [FromForm] EnviarMensajeForm form, CancellationToken ct)
    {
        using var imagen = form.Imagen?.ParaNegocio();
        return StatusCode(StatusCodes.Status201Created,
            await mensajeria.EnviarAsync(id, new EnviarMensajeRequest(form.Contenido), imagen, ct));
    }

    [HttpPost("{id:guid}/leida")]
    public async Task<IActionResult> MarcarLeida(Guid id, CancellationToken ct)
    {
        await mensajeria.MarcarLeidaAsync(id, ct);
        return NoContent();
    }

    /// <summary>La "X" de la lista: elimina la conversación para el usuario actual.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await mensajeria.EliminarAsync(id, ct);
        return NoContent();
    }
}
