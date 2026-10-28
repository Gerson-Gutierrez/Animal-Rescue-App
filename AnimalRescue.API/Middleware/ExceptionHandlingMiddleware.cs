using AnimalRescue.BLL.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace AnimalRescue.API.Middleware;

/// <summary>Traduce las excepciones de negocio a respuestas ProblemDetails (RFC 7807).</summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // El cliente cerró la conexión: no hay a quién responder.
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted) throw;
            await Responder(context, ex);
        }
    }

    private async Task Responder(HttpContext context, Exception ex)
    {
        ProblemDetails problema;
        switch (ex)
        {
            case ValidacionException v:
                problema = new ValidationProblemDetails(v.Errores)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Hay errores de validación."
                };
                break;
            case NegocioException n:
                problema = new ProblemDetails { Status = Estado(n), Title = n.Message };
                break;
            default:
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", context.Request.Method, context.Request.Path);
                problema = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Ocurrió un error inesperado. Inténtalo de nuevo."
                };
                break;
        }

        problema.Instance = context.Request.Path;
        context.Response.StatusCode = problema.Status!.Value;
        await context.Response.WriteAsJsonAsync(problema, problema.GetType(), options: null, contentType: "application/problem+json");
    }

    private static int Estado(NegocioException ex) => ex switch
    {
        NotFoundException => StatusCodes.Status404NotFound,
        ForbiddenException => StatusCodes.Status403Forbidden,
        ConflictException => StatusCodes.Status409Conflict,
        ReglaNegocioException => StatusCodes.Status422UnprocessableEntity,
        CredencialesInvalidasException => StatusCodes.Status401Unauthorized,
        ServicioExternoException => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status400BadRequest
    };
}
