using DevNationCrono.API.Models.Entities;
using System.Security.Claims;

namespace DevNationCrono.API.Services.Interfaces;

public interface ITokenService
{
    string GerarTokenPiloto(Piloto piloto);
    string GerarTokenColetor(DispositivoColetor coletor);
    string GerarTokenOrganizador(int userId, string nome, string email);
    string GerarTokenAdmin(int userId, string nome, string email);
    ClaimsPrincipal? ValidarToken(string token);
    int? ObterUserIdDoToken(string token);
    string? ObterRoleDoToken(string token);
}
