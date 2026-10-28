namespace AnimalRescue.BLL.Options;

public class JwtOptions
{
    public const string Seccion = "Jwt";

    public string Secret { get; set; } = null!;
    public string Issuer { get; set; } = "AnimalRescue";
    public string Audience { get; set; } = "AnimalRescue.App";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}

public class BootstrapOptions
{
    public const string Seccion = "Bootstrap";

    /// <summary>Quien se registre con este correo recibe el rol SUPERADMIN. Dejar vacío en producción una vez creado.</summary>
    public string? SuperAdminEmail { get; set; }
}
