namespace AnimalRescue.DAL.External.Geocoding;

public record ResultadoGeocodificacion(string Direccion, double Latitud, double Longitud);

public interface IGeocodificador
{
    /// <summary>Convierte un texto de dirección en coordenadas. Lista vacía si no hay coincidencias.</summary>
    Task<IReadOnlyList<ResultadoGeocodificacion>> BuscarAsync(string texto, int limite = 5, CancellationToken ct = default);
}
