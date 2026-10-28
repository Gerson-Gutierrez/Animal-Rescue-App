using AnimalRescue.BLL.DTOs.Admin;
using AnimalRescue.BLL.DTOs.Auth;
using AnimalRescue.BLL.DTOs.Mensajeria;
using AnimalRescue.BLL.DTOs.Publicaciones;
using AnimalRescue.BLL.DTOs.Reportes;
using AnimalRescue.BLL.DTOs.Rescue;
using AnimalRescue.BLL.Exceptions;
using AnimalRescue.DAL.Entities;
using FluentValidation;

namespace AnimalRescue.BLL.Validators;

public static class ValidatorExtensions
{
    /// <summary>Valida el objeto y lanza <see cref="ValidacionException"/> con los errores agrupados por campo.</summary>
    public static async Task ValidarYLanzarAsync<T>(this IValidator<T> validator, T instancia, CancellationToken ct = default)
    {
        var resultado = await validator.ValidateAsync(instancia, ct);
        if (resultado.IsValid) return;

        var errores = resultado.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        throw new ValidacionException(errores);
    }
}

public class RegistroRequestValidator : AbstractValidator<RegistroRequest>
{
    public RegistroRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(72)
            .WithMessage("La contraseña debe tener entre 8 y 72 caracteres.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class ActualizarPerfilRequestValidator : AbstractValidator<ActualizarPerfilRequest>
{
    public ActualizarPerfilRequestValidator() => RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
}

public class CrearPublicacionRequestValidator : AbstractValidator<CrearPublicacionRequest>
{
    public CrearPublicacionRequestValidator()
    {
        RuleFor(x => x.NombreMascota).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Tamano).IsInEnum();
        RuleFor(x => x.Edad).MaximumLength(30);
        RuleFor(x => x.Descripcion).MaximumLength(2000);
        RuleFor(x => x.FechaPerdida)
            .Must(f => f <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("La fecha en que se perdió no puede ser futura.");
        RuleFor(x => x.Recompensa).GreaterThanOrEqualTo(0).LessThan(1_000_000_000m).When(x => x.Recompensa.HasValue);
        RuleFor(x => x.UbicacionTexto).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Latitud).InclusiveBetween(-90, 90).When(x => x.Latitud.HasValue);
        RuleFor(x => x.Longitud).InclusiveBetween(-180, 180).When(x => x.Longitud.HasValue);
        RuleFor(x => x).Must(x => x.Latitud.HasValue == x.Longitud.HasValue)
            .WithMessage("Latitud y longitud deben enviarse juntas.");
    }
}

public class EditarPublicacionRequestValidator : AbstractValidator<EditarPublicacionRequest>
{
    public EditarPublicacionRequestValidator()
    {
        RuleFor(x => x.NombreMascota).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Tamano).IsInEnum();
        RuleFor(x => x.Edad).MaximumLength(30);
        RuleFor(x => x.Descripcion).MaximumLength(2000);
        RuleFor(x => x.FechaPerdida)
            .Must(f => f <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("La fecha en que se perdió no puede ser futura.");
        RuleFor(x => x.Recompensa).GreaterThanOrEqualTo(0).LessThan(1_000_000_000m).When(x => x.Recompensa.HasValue);
        RuleFor(x => x.UbicacionTexto).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Latitud).InclusiveBetween(-90, 90).When(x => x.Latitud.HasValue);
        RuleFor(x => x.Longitud).InclusiveBetween(-180, 180).When(x => x.Longitud.HasValue);
        RuleFor(x => x).Must(x => x.Latitud.HasValue == x.Longitud.HasValue)
            .WithMessage("Latitud y longitud deben enviarse juntas.");
        RuleFor(x => x.Estado).Must(e => e is EstadoPublicacion.ACTIVA or EstadoPublicacion.ENCONTRADA)
            .WithMessage("El estado sólo puede ser ACTIVA o ENCONTRADA.");
    }
}

public class AbrirConversacionRequestValidator : AbstractValidator<AbrirConversacionRequest>
{
    public AbrirConversacionRequestValidator() => RuleFor(x => x.DestinatarioId).NotEmpty();
}

public class EnviarMensajeRequestValidator : AbstractValidator<EnviarMensajeRequest>
{
    public EnviarMensajeRequestValidator() => RuleFor(x => x.Contenido).MaximumLength(4000);
}

public class CrearHogarRequestValidator : AbstractValidator<CrearHogarRequest>
{
    public CrearHogarRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Telefono).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Ciudad).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Pais).NotEmpty().MaximumLength(100);
    }
}

public class CrearRefugioRequestValidator : AbstractValidator<CrearRefugioRequest>
{
    public CrearRefugioRequestValidator()
    {
        RuleFor(x => x.Direccion).NotEmpty().MinimumLength(3).MaximumLength(300);
        RuleFor(x => x.Nombre).MaximumLength(150);
    }
}

public class CrearReporteRequestValidator : AbstractValidator<CrearReporteRequest>
{
    public CrearReporteRequestValidator()
    {
        RuleFor(x => x.Motivo).IsInEnum();
        RuleFor(x => x.Comentario).MaximumLength(500);
    }
}

public class ResolverReporteRequestValidator : AbstractValidator<ResolverReporteRequest>
{
    public ResolverReporteRequestValidator()
    {
        RuleFor(x => x.Accion).IsInEnum();
        RuleFor(x => x.Nota).MaximumLength(500);
    }
}

public class CambiarRolRequestValidator : AbstractValidator<CambiarRolRequest>
{
    public CambiarRolRequestValidator() =>
        RuleFor(x => x.Rol).Must(r => r is Rol.USUARIO or Rol.ADMIN)
            .WithMessage("Sólo se puede asignar el rol USUARIO o ADMIN.");
}
