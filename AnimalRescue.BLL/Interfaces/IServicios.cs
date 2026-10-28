using AnimalRescue.BLL.DTOs.Admin;
using AnimalRescue.BLL.DTOs.Auth;
using AnimalRescue.BLL.DTOs.Comun;
using AnimalRescue.BLL.DTOs.Mensajeria;
using AnimalRescue.BLL.DTOs.Publicaciones;
using AnimalRescue.BLL.DTOs.Reportes;
using AnimalRescue.BLL.DTOs.Rescue;
using AnimalRescue.DAL.Entities;

namespace AnimalRescue.BLL.Interfaces;

/// <summary>Usuario de la petición actual; lo implementa la capa de presentación a partir del JWT.</summary>
public interface ICurrentUser
{
    bool EstaAutenticado { get; }
    Guid? Id { get; }
    Rol? Rol { get; }
    bool EsAdmin { get; }
    bool EsSuperAdmin { get; }

    /// <summary>Id del usuario autenticado o <see cref="Exceptions.ForbiddenException"/>.</summary>
    Guid IdRequerido { get; }
}

/// <summary>Difunde mensajes nuevos en tiempo real (SignalR en la API).</summary>
public interface INotificadorMensajes
{
    Task MensajeNuevoAsync(Guid destinatarioId, MensajeDto mensaje, CancellationToken ct = default);
}

public interface IAuthService
{
    Task<AuthResponse> RegistrarAsync(RegistroRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefrescarAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<UsuarioDto> MiPerfilAsync(CancellationToken ct = default);
    Task<UsuarioDto> ActualizarPerfilAsync(ActualizarPerfilRequest request, CancellationToken ct = default);
}

public interface IPublicacionService
{
    Task<PaginaDto<PublicacionCardDto>> FeedAsync(int pagina, int tamano, string? busqueda, CancellationToken ct = default);
    Task<PublicacionDetalleDto> DetalleAsync(Guid id, CancellationToken ct = default);
    Task<PublicacionDetalleDto> CrearAsync(CrearPublicacionRequest request, IReadOnlyList<ArchivoSubido> imagenes, CancellationToken ct = default);
    Task<PublicacionDetalleDto> EditarAsync(Guid id, EditarPublicacionRequest request, IReadOnlyList<ArchivoSubido> nuevasImagenes, CancellationToken ct = default);
    Task EliminarAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PublicacionCardDto>> MiasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MarcadorMapaDto>> MapaAsync(double? norte, double? sur, double? este, double? oeste, CancellationToken ct = default);
}

public interface IGeocodificacionService
{
    Task<IReadOnlyList<GeocodificacionDto>> BuscarAsync(string texto, CancellationToken ct = default);

    /// <summary>Mejor coincidencia o null si la dirección no existe.</summary>
    Task<GeocodificacionDto?> PrimeraAsync(string texto, CancellationToken ct = default);
}

public interface IMensajeriaService
{
    Task<IReadOnlyList<ConversacionResumenDto>> ListarAsync(CancellationToken ct = default);
    Task<ConversacionResumenDto> AbrirAsync(AbrirConversacionRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<MensajeDto>> MensajesAsync(Guid conversacionId, DateTime? antesDe, int limite, CancellationToken ct = default);
    Task<MensajeDto> EnviarAsync(Guid conversacionId, EnviarMensajeRequest request, ArchivoSubido? imagen, CancellationToken ct = default);
    Task MarcarLeidaAsync(Guid conversacionId, CancellationToken ct = default);
    Task EliminarAsync(Guid conversacionId, CancellationToken ct = default);
}

public interface IHogarTransitoService
{
    Task<IReadOnlyList<HogarTransitoDto>> ListarAsync(CancellationToken ct = default);
    Task<HogarTransitoDto?> MioAsync(CancellationToken ct = default);
    Task<HogarTransitoDto> CrearAsync(CrearHogarRequest request, CancellationToken ct = default);
    Task EliminarMioAsync(CancellationToken ct = default);
}

public interface IRefugioService
{
    Task<IReadOnlyList<RefugioDto>> ListarAsync(CancellationToken ct = default);
    Task<RefugioDto> CrearAsync(CrearRefugioRequest request, CancellationToken ct = default);
    Task EliminarAsync(Guid id, CancellationToken ct = default);
}

public interface IReporteService
{
    Task CrearAsync(Guid publicacionId, CrearReporteRequest request, CancellationToken ct = default);
}

public interface IAdminService
{
    Task<PaginaDto<ReporteDto>> ReportesAsync(EstadoReporte? estado, int pagina, int tamano, CancellationToken ct = default);
    Task<ReporteDto> ResolverReporteAsync(Guid reporteId, ResolverReporteRequest request, CancellationToken ct = default);
    Task<PaginaDto<AdminPublicacionDto>> PublicacionesAsync(EstadoPublicacion? estado, string? busqueda, int pagina, int tamano, CancellationToken ct = default);
    Task EliminarPublicacionAsync(Guid id, CancellationToken ct = default);
    Task<PaginaDto<UsuarioAdminDto>> UsuariosAsync(string? busqueda, int pagina, int tamano, CancellationToken ct = default);
    Task<UsuarioAdminDto> CambiarRolAsync(Guid usuarioId, CambiarRolRequest request, CancellationToken ct = default);
}
