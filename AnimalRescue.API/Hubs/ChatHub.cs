using AnimalRescue.BLL.DTOs.Mensajeria;
using AnimalRescue.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AnimalRescue.API.Hubs;

/// <summary>Canal de tiempo real: el cliente sólo escucha; los mensajes se envían por REST.</summary>
[Authorize]
public class ChatHub : Hub
{
    public const string Ruta = "/hubs/chat";
    public const string EventoMensaje = "MensajeRecibido";
}

public class SignalRNotificador(IHubContext<ChatHub> hub) : INotificadorMensajes
{
    public Task MensajeNuevoAsync(Guid destinatarioId, MensajeDto mensaje, CancellationToken ct = default) =>
        hub.Clients.User(destinatarioId.ToString()).SendAsync(ChatHub.EventoMensaje, mensaje, ct);
}

/// <summary>Identifica la conexión por el claim "sub" del JWT.</summary>
public class SubUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => connection.User?.FindFirst("sub")?.Value;
}
