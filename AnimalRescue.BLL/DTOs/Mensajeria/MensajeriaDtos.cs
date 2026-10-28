using AnimalRescue.BLL.DTOs.Comun;

namespace AnimalRescue.BLL.DTOs.Mensajeria;

public record AbrirConversacionRequest(Guid DestinatarioId, Guid? PublicacionId);

public record EnviarMensajeRequest(string? Contenido);

public record MensajeDto(
    Guid Id,
    Guid ConversacionId,
    Guid RemitenteId,
    string? Contenido,
    string? ImagenUrl,
    DateTime FechaEnvio);

public record ConversacionResumenDto(
    Guid Id,
    UsuarioResumenDto Otro,
    Guid? PublicacionId,
    string? PublicacionNombre,
    string? UltimoMensaje,
    bool UltimoMensajeEsImagen,
    DateTime? FechaUltimoMensaje,
    int NoLeidos);
