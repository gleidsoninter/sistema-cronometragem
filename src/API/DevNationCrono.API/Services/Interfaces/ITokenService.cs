using DevNationCrono.API.Models.Entities;
using System.Security.Claims;

namespace DevNationCrono.API.Services.Interfaces;

public interface ITokenService
{
    string GerarTokenPiloto(Piloto piloto);
    string GerarTokenColetor(DispositivoColetor dispositivo);
    string GerarTokenAdmin(string nome, string email, string role);
    bool ValidarToken(string token);
    int? ObterIdDoToken(string token);
    string GerarTokenUsuario(Usuario usuario);
}
