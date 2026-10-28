using AnimalRescue.BLL.DTOs.Comun;
using AnimalRescue.BLL.DTOs.Mensajeria;
using AnimalRescue.BLL.Exceptions;
using AnimalRescue.BLL.Interfaces;
using AnimalRescue.BLL.Validators;
using AnimalRescue.DAL.Entities;
using AnimalRescue.DAL.External.Storage;
using AnimalRescue.DAL.UnitOfWork;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AnimalRescue.BLL.Services;

public class MensajeriaService(
    IUnitOfWork uow,
    ICurrentUser actual,
    ProcesadorImagenes procesador,
    IAlmacenamientoArchivos almacenamiento,
    INotificadorMensajes notificador,
    IValidator<AbrirConversacionRequest> validarAbrir,
    IValidator<EnviarMensajeRequest> validarEnviar) : IMensajeriaService
{
    private const string CarpetaImagenes = "mensajes";

    public async Task<IReadOnlyList<ConversacionResumenDto>> ListarAsync(CancellationToken ct = default)
    {
        var yo = actual.IdRequerido;

        // Sólo chats con mensajes y que yo no haya eliminado (o que hayan recibido mensajes después de eliminarlos).
        var conversaciones = await uow.Conversaciones.QuerySinSeguimiento()
            .Include(c => c.Usuario1).Include(c => c.Usuario2).Include(c => c.Publicacion)
            .Where(c => c.FechaUltimoMensaje != null &&
                ((c.Usuario1Id == yo && (c.Usuario1EliminadaEn == null || c.FechaUltimoMensaje > c.Usuario1EliminadaEn)) ||
                 (c.Usuario2Id == yo && (c.Usuario2EliminadaEn == null || c.FechaUltimoMensaje > c.Usuario2EliminadaEn))))
            .OrderByDescending(c => c.FechaUltimoMensaje)
            .ToListAsync(ct);

        var ids = conversaciones.Select(c => c.Id).ToList();

        var noLeidos = await uow.Mensajes.QuerySinSeguimiento()
            .Where(m => ids.Contains(m.ConversacionId) && m.RemitenteId != yo)
            .Where(m =>
                (m.Conversacion.Usuario1Id == yo &&
                    (m.Conversacion.Usuario1UltimaLectura == null || m.FechaEnvio > m.Conversacion.Usuario1UltimaLectura) &&
                    (m.Conversacion.Usuario1EliminadaEn == null || m.FechaEnvio > m.Conversacion.Usuario1EliminadaEn)) ||
                (m.Conversacion.Usuario2Id == yo &&
                    (m.Conversacion.Usuario2UltimaLectura == null || m.FechaEnvio > m.Conversacion.Usuario2UltimaLectura) &&
                    (m.Conversacion.Usuario2EliminadaEn == null || m.FechaEnvio > m.Conversacion.Usuario2EliminadaEn)))
            .GroupBy(m => m.ConversacionId)
            .Select(g => new { Id = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Total, ct);

        // fecha_ultimo_mensaje la mantiene un trigger con la fecha exacta del último mensaje.
        var ultimos = (await uow.Mensajes.QuerySinSeguimiento()
                .Where(m => ids.Contains(m.ConversacionId) && m.FechaEnvio == m.Conversacion.FechaUltimoMensaje)
                .Select(m => new { m.ConversacionId, m.Contenido, m.ImagenUrl })
                .ToListAsync(ct))
            .GroupBy(m => m.ConversacionId)
            .ToDictionary(g => g.Key, g => g.First());

        return conversaciones.Select(c =>
        {
            ultimos.TryGetValue(c.Id, out var ultimo);
            return Resumen(c, yo, ultimo?.Contenido, ultimo?.ImagenUrl, noLeidos.GetValueOrDefault(c.Id));
        }).ToList();
    }

    public async Task<ConversacionResumenDto> AbrirAsync(AbrirConversacionRequest request, CancellationToken ct = default)
    {
        var yo = actual.IdRequerido;
        await validarAbrir.ValidarYLanzarAsync(request, ct);

        if (request.DestinatarioId == yo)
            throw new ReglaNegocioException("No puedes enviarte mensajes a ti mismo.");

        var destinatarioActivo = await uow.Usuarios.QuerySinSeguimiento()
            .AnyAsync(u => u.Id == request.DestinatarioId && u.Activo, ct);
        if (!destinatarioActivo) throw new NotFoundException("El usuario no existe.");

        if (request.PublicacionId is { } publicacionId)
        {
            var coincide = await uow.Publicaciones.QuerySinSeguimiento()
                .AnyAsync(p => p.Id == publicacionId && p.UsuarioId == request.DestinatarioId, ct);
            if (!coincide) throw new ReglaNegocioException("La publicación no pertenece al destinatario.");
        }

        var otro = request.DestinatarioId;
        var existente = await BuscarParAsync(yo, otro, ct);
        if (existente is null)
        {
            existente = new Conversacion { Usuario1Id = yo, Usuario2Id = otro, PublicacionId = request.PublicacionId };
            uow.Conversaciones.Agregar(existente);
            try
            {
                await uow.SaveChangesAsync(ct);
            }
            catch (ConflictoDatosException)
            {
                // Otra petición creó el mismo chat al mismo tiempo.
                existente = await BuscarParAsync(yo, otro, ct) ?? throw new ConflictException("No se pudo abrir la conversación.");
            }
        }
        else if (request.PublicacionId.HasValue && existente.PublicacionId != request.PublicacionId)
        {
            existente.PublicacionId = request.PublicacionId;
            await uow.SaveChangesAsync(ct);
        }

        var c = await uow.Conversaciones.QuerySinSeguimiento()
            .Include(x => x.Usuario1).Include(x => x.Usuario2).Include(x => x.Publicacion)
            .FirstAsync(x => x.Id == existente.Id, ct);
        return Resumen(c, yo, null, null, 0);
    }

    public async Task<IReadOnlyList<MensajeDto>> MensajesAsync(Guid conversacionId, DateTime? antesDe, int limite, CancellationToken ct = default)
    {
        var yo = actual.IdRequerido;
        var c = await ObtenerParticipanteAsync(conversacionId, yo, seguimiento: false, ct);
        limite = Math.Clamp(limite, 1, 100);

        // Tras pulsar "X" sólo se ven los mensajes posteriores.
        var desde = c.Usuario1Id == yo ? c.Usuario1EliminadaEn : c.Usuario2EliminadaEn;

        var consulta = uow.Mensajes.QuerySinSeguimiento().Where(m => m.ConversacionId == conversacionId);
        if (desde.HasValue) consulta = consulta.Where(m => m.FechaEnvio > desde.Value);
        if (antesDe.HasValue) consulta = consulta.Where(m => m.FechaEnvio < antesDe.Value);

        var mensajes = await consulta
            .OrderByDescending(m => m.FechaEnvio)
            .Take(limite)
            .Select(m => new MensajeDto(m.Id, m.ConversacionId, m.RemitenteId, m.Contenido, m.ImagenUrl, m.FechaEnvio))
            .ToListAsync(ct);

        mensajes.Reverse();
        return mensajes;
    }

    public async Task<MensajeDto> EnviarAsync(Guid conversacionId, EnviarMensajeRequest request, ArchivoSubido? imagen, CancellationToken ct = default)
    {
        var yo = actual.IdRequerido;
        await validarEnviar.ValidarYLanzarAsync(request, ct);

        var texto = string.IsNullOrWhiteSpace(request.Contenido) ? null : request.Contenido.Trim();
        if (texto is null && imagen is null)
            throw new ValidacionException(new Dictionary<string, string[]> { ["contenido"] = ["Escribe un mensaje o adjunta una imagen."] });

        var c = await ObtenerParticipanteAsync(conversacionId, yo, seguimiento: true, ct);

        string? imagenUrl = null;
        if (imagen is not null)
            imagenUrl = (await procesador.GuardarAsync(imagen, CarpetaImagenes, ct)).Url;

        // PostgreSQL guarda microsegundos: se trunca para que coincida con fecha_ultimo_mensaje.
        var ahora = new DateTime(DateTime.UtcNow.Ticks / 10 * 10, DateTimeKind.Utc);
        var mensaje = new Mensaje { ConversacionId = c.Id, RemitenteId = yo, Contenido = texto, ImagenUrl = imagenUrl, FechaEnvio = ahora };
        c.FechaUltimoMensaje = mensaje.FechaEnvio;
        uow.Mensajes.Agregar(mensaje);

        try
        {
            await uow.SaveChangesAsync(ct);
        }
        catch
        {
            await almacenamiento.EliminarAsync(imagenUrl);
            throw;
        }

        var dto = new MensajeDto(mensaje.Id, c.Id, yo, texto, imagenUrl, mensaje.FechaEnvio);
        var destinatario = c.Usuario1Id == yo ? c.Usuario2Id : c.Usuario1Id;
        await notificador.MensajeNuevoAsync(destinatario, dto, ct);
        return dto;
    }

    public async Task MarcarLeidaAsync(Guid conversacionId, CancellationToken ct = default)
    {
        var yo = actual.IdRequerido;
        var c = await ObtenerParticipanteAsync(conversacionId, yo, seguimiento: true, ct);

        if (c.Usuario1Id == yo) c.Usuario1UltimaLectura = DateTime.UtcNow;
        else c.Usuario2UltimaLectura = DateTime.UtcNow;

        await uow.SaveChangesAsync(ct);
    }

    public async Task EliminarAsync(Guid conversacionId, CancellationToken ct = default)
    {
        var yo = actual.IdRequerido;
        var c = await ObtenerParticipanteAsync(conversacionId, yo, seguimiento: true, ct);

        var ahora = DateTime.UtcNow;
        if (c.Usuario1Id == yo) (c.Usuario1EliminadaEn, c.Usuario1UltimaLectura) = (ahora, ahora);
        else (c.Usuario2EliminadaEn, c.Usuario2UltimaLectura) = (ahora, ahora);

        // Si ambos la eliminaron, se borra físicamente junto con sus imágenes.
        var ambos = c.Usuario1EliminadaEn.HasValue && c.Usuario2EliminadaEn.HasValue;
        List<string> archivos = [];
        if (ambos)
        {
            archivos = await uow.Mensajes.QuerySinSeguimiento()
                .Where(m => m.ConversacionId == c.Id && m.ImagenUrl != null)
                .Select(m => m.ImagenUrl!)
                .ToListAsync(ct);
            uow.Conversaciones.Eliminar(c);
        }

        await uow.SaveChangesAsync(ct);
        foreach (var url in archivos) await almacenamiento.EliminarAsync(url, ct);
    }

    private Task<Conversacion?> BuscarParAsync(Guid a, Guid b, CancellationToken ct) =>
        uow.Conversaciones.Query().FirstOrDefaultAsync(c =>
            (c.Usuario1Id == a && c.Usuario2Id == b) || (c.Usuario1Id == b && c.Usuario2Id == a), ct);

    private async Task<Conversacion> ObtenerParticipanteAsync(Guid id, Guid usuarioId, bool seguimiento, CancellationToken ct)
    {
        var consulta = seguimiento ? uow.Conversaciones.Query() : uow.Conversaciones.QuerySinSeguimiento();
        var c = await consulta.FirstOrDefaultAsync(x => x.Id == id, ct);

        // Para quien no participa la conversación "no existe": no se revela su existencia.
        if (c is null || (c.Usuario1Id != usuarioId && c.Usuario2Id != usuarioId))
            throw new NotFoundException("La conversación no existe.");
        return c;
    }

    private static ConversacionResumenDto Resumen(Conversacion c, Guid yo, string? contenido, string? imagenUrl, int noLeidos)
    {
        var otro = c.Usuario1Id == yo ? c.Usuario2 : c.Usuario1;
        var esImagen = string.IsNullOrWhiteSpace(contenido) && imagenUrl is not null;
        return new ConversacionResumenDto(
            c.Id,
            new UsuarioResumenDto(otro.Id, otro.Nombre, otro.FotoUrl),
            c.PublicacionId,
            c.Publicacion?.NombreMascota,
            esImagen ? null : contenido,
            esImagen,
            c.FechaUltimoMensaje,
            noLeidos);
    }
}
