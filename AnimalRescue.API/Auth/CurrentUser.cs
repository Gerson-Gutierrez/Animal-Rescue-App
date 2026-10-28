using AnimalRescue.BLL.Exceptions;
using AnimalRescue.BLL.Interfaces;
using AnimalRescue.DAL.Entities;

namespace AnimalRescue.API.Auth;

/// <summary>Expone al negocio los datos del JWT de la petición actual.</summary>
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private System.Security.Claims.ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool EstaAutenticado => Principal?.Identity?.IsAuthenticated == true;

    public Guid? Id => Guid.TryParse(Principal?.FindFirst("sub")?.Value, out var id) ? id : null;

    public Rol? Rol => Enum.TryParse<Rol>(Principal?.FindFirst("role")?.Value, out var rol) ? rol : null;

    public bool EsAdmin => Rol is DAL.Entities.Rol.ADMIN or DAL.Entities.Rol.SUPERADMIN;

    public bool EsSuperAdmin => Rol is DAL.Entities.Rol.SUPERADMIN;

    public Guid IdRequerido => Id ?? throw new ForbiddenException("Debes iniciar sesión.");
}
