namespace AnimalRescue.BLL.Exceptions;

/// <summary>Base de las excepciones que la API traduce a una respuesta HTTP.</summary>
public abstract class NegocioException(string mensaje) : Exception(mensaje);

public class NotFoundException(string mensaje = "El recurso no existe.") : NegocioException(mensaje);

public class ForbiddenException(string mensaje = "No tienes permiso para realizar esta acción.") : NegocioException(mensaje);

public class ConflictException(string mensaje) : NegocioException(mensaje);

/// <summary>La petición es válida pero incumple una regla de negocio (HTTP 422).</summary>
public class ReglaNegocioException(string mensaje) : NegocioException(mensaje);

public class CredencialesInvalidasException(string mensaje = "Credenciales inválidas.") : NegocioException(mensaje);

public class ServicioExternoException(string mensaje) : NegocioException(mensaje);

public class ValidacionException(IDictionary<string, string[]> errores) : NegocioException("Hay errores de validación.")
{
    public IDictionary<string, string[]> Errores { get; } = errores;
}
