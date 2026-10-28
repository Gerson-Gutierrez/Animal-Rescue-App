using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using AnimalRescue.BLL.Options;
using AnimalRescue.DAL.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AnimalRescue.BLL.Services;

public class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _opciones = options.Value;

    public (string Token, DateTime ExpiraEn) CrearAccessToken(Usuario usuario)
    {
        var expira = DateTime.UtcNow.AddMinutes(_opciones.AccessTokenMinutes);
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.Secret)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _opciones.Issuer,
            _opciones.Audience,
            claims:
            [
                new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, usuario.Email),
                new("name", usuario.Nombre),
                new("role", usuario.Rol.ToString())
            ],
            expires: expira,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }

    /// <summary>Genera un refresh token opaco; al cliente se le entrega <c>Valor</c> y en la BD se guarda <c>Hash</c>.</summary>
    public (string Valor, string Hash) CrearRefreshToken()
    {
        var valor = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(48));
        return (valor, Hashear(valor));
    }

    public static string Hashear(string valor) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor))).ToLowerInvariant();

    public DateTime ExpiracionRefresh() => DateTime.UtcNow.AddDays(_opciones.RefreshTokenDays);
}
