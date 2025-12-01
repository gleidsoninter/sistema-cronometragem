using DevNationCrono.API.Configuration;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DevNationCrono.API.Services.Implementations;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        IOptions<JwtSettings> jwtSettings,
        ILogger<TokenService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public string GerarTokenPiloto(Piloto piloto)
    {
        var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, piloto.Id.ToString()),
                new Claim(ClaimTypes.Name, piloto.Nome),
                new Claim(ClaimTypes.Email, piloto.Email),
                new Claim(ClaimTypes.Role, "Piloto"),
                new Claim("cpf", piloto.Cpf),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

        return GerarToken(claims);
    }

    public string GerarTokenColetor(DispositivoColetor coletor)
    {
        var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, coletor.Id.ToString()),
                new Claim(ClaimTypes.Name, coletor.Nome),
                new Claim(ClaimTypes.Role, "Coletor"),
                new Claim("deviceId", coletor.DeviceId),
                new Claim("idEvento", coletor.IdEvento.ToString()),
                new Claim("idEtapa", coletor.IdEtapa.ToString()),
                new Claim("tipo", coletor.Tipo),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

        if (coletor.IdEspecial.HasValue)
        {
            claims.Add(new Claim("idEspecial", coletor.IdEspecial.Value.ToString()));
        }

        return GerarToken(claims);
    }

    public string GerarTokenOrganizador(int userId, string nome, string email)
    {
        var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, nome),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, "Organizador"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

        return GerarToken(claims);
    }

    public string GerarTokenAdmin(int userId, string nome, string email)
    {
        var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, nome),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

        return GerarToken(claims);
    }

    private string GerarToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidarToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token inválido");
            return null;
        }
    }

    public int? ObterUserIdDoToken(string token)
    {
        var principal = ValidarToken(token);
        if (principal == null) return null;

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return null;

        return int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }

    public string? ObterRoleDoToken(string token)
    {
        var principal = ValidarToken(token);
        return principal?.FindFirst(ClaimTypes.Role)?.Value;
    }
}
