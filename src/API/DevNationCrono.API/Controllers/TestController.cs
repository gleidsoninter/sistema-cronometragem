using DevNationCrono.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SistemaCronometragem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("conexao")]
        public async Task<IActionResult> TestarConexao()
        {
            try
            {
                // Tenta conectar ao banco
                var canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    // Conta quantas modalidades existem
                    var countModalidades = await _context.Modalidades.CountAsync();

                    return Ok(new
                    {
                        sucesso = true,
                        mensagem = "Conexão com banco de dados estabelecida!",
                        modalidades = countModalidades,
                        banco = _context.Database.GetDbConnection().Database
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        sucesso = false,
                        mensagem = "Não foi possível conectar ao banco de dados"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro ao conectar",
                    erro = ex.Message
                });
            }
        }
    }
}