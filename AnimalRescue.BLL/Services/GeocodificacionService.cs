using AnimalRescue.BLL.DTOs.Publicaciones;
using AnimalRescue.BLL.Exceptions;
using AnimalRescue.BLL.Interfaces;
using AnimalRescue.DAL.External.Geocoding;

namespace AnimalRescue.BLL.Services;

public class GeocodificacionService(IGeocodificador geocodificador) : IGeocodificacionService
{
    public async Task<IReadOnlyList<GeocodificacionDto>> BuscarAsync(string texto, CancellationToken ct = default)
    {
        try
        {
            var resultados = await geocodificador.BuscarAsync(texto, 5, ct);
            return resultados.Select(r => new GeocodificacionDto(r.Direccion, r.Latitud, r.Longitud)).ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw new ServicioExternoException("El servicio de mapas no está disponible. Inténtalo de nuevo en unos minutos.");
        }
    }

    /// <summary>Devuelve la mejor coincidencia o null si la dirección no existe.</summary>
    public async Task<GeocodificacionDto?> PrimeraAsync(string texto, CancellationToken ct = default) =>
        (await BuscarAsync(texto, ct)).FirstOrDefault();
}
