using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace AnimalRescue.DAL.External.Geocoding;

internal class NominatimGeocodificador(HttpClient http, IMemoryCache cache) : IGeocodificador
{
    public async Task<IReadOnlyList<ResultadoGeocodificacion>> BuscarAsync(string texto, int limite = 5, CancellationToken ct = default)
    {
        var consulta = texto.Trim();
        if (consulta.Length < 3) return [];

        var clave = $"geo:{limite}:{consulta.ToLowerInvariant()}";
        if (cache.TryGetValue(clave, out IReadOnlyList<ResultadoGeocodificacion>? enCache) && enCache is not null)
            return enCache;

        var url = $"search?format=jsonv2&limit={limite}&q={Uri.EscapeDataString(consulta)}";
        using var respuesta = await http.GetAsync(url, ct);
        respuesta.EnsureSuccessStatusCode();

        await using var flujo = await respuesta.Content.ReadAsStreamAsync(ct);
        using var json = await JsonDocument.ParseAsync(flujo, cancellationToken: ct);

        var resultados = json.RootElement.EnumerateArray()
            .Select(e => new ResultadoGeocodificacion(
                e.GetProperty("display_name").GetString() ?? consulta,
                double.Parse(e.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture),
                double.Parse(e.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture)))
            .ToList();

        cache.Set(clave, (IReadOnlyList<ResultadoGeocodificacion>)resultados, TimeSpan.FromHours(24));
        return resultados;
    }
}
