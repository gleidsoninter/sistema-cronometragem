using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace DevNationCrono.API.Services.Implementations;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GerarTokenPiloto(Piloto piloto)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, piloto.Id.ToString()),
            new Claim(ClaimTypes.Name, piloto.Nome),
            new Claim(ClaimTypes.Email, piloto.Email),
            new Claim(ClaimTypes.Role, "Piloto")
        };

        return GerarToken(claims, TimeSpan.FromDays(7));
    }

    public string GerarTokenColetor(DispositivoColetor dispositivo)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, dispositivo.Id.ToString()),
            new Claim("DeviceId", dispositivo.DeviceId),
            new Claim(ClaimTypes.Name, dispositivo.Nome ?? dispositivo.DeviceId),
            new Claim(ClaimTypes.Role, "Coletor"),
            new Claim("TipoPonto", dispositivo.Tipo)
        };

        return GerarToken(claims, TimeSpan.FromHours(24));
    }

    public string GerarTokenAdmin(string nome, string email, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, nome),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        return GerarToken(claims, TimeSpan.FromHours(8));
    }

    public bool ValidarToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:Secret"] ?? "X7q9m2vK4nL8wP1zR5jH3bM6tF0yG2cD");

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public int? ObterIdDoToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var idClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (idClaim != null && int.TryParse(idClaim.Value, out var id))
            {
                return id;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private string GerarToken(Claim[] claims, TimeSpan expiracao)
    {
        var key = Encoding.ASCII.GetBytes(
            _configuration["JwtSettings:Secret"] ?? "X7q9m2vK4nL8wP1zR5jH3bM6tF0yG2cD");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(expiracao),
            Issuer = _configuration["JwtSettings:Issuer"],
            Audience = _configuration["JwtSettings:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Gera token JWT para usuário administrativo (Admin/Organizador)
    /// </summary>
    public string GerarTokenUsuario(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Role),  // ← Role dinâmica: Admin ou Organizador
            new Claim("tipo", "Usuario")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}