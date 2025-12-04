using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DevNationCrono.API.Data;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.Tests.Integration.Helpers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevNationCrono.Tests.Integration.Fixtures;

public class ApiTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // ========== REMOVER DBCONTEXT EXISTENTE ==========
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            var dbContextServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ApplicationDbContext));
            if (dbContextServiceDescriptor != null)
                services.Remove(dbContextServiceDescriptor);

            // ========== ADICIONAR DBCONTEXT EM MEMÓRIA ==========
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.EnableSensitiveDataLogging();
            });

            // ========== REMOVER AUTENTICAÇÃO EXISTENTE ==========
            // Remove todos os schemes de autenticação existentes
            services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();
            services.RemoveAll<IPostConfigureOptions<AuthenticationOptions>>();

            // ========== ADICIONAR AUTENTICAÇÃO DE TESTE ==========
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            // ========== CRIAR BANCO E SEED ==========
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            SeedTestData(db);
        });
    }

    private void SeedTestData(ApplicationDbContext db)
    {
        // Modalidades
        var modalidadeCircuito = new Modalidade
        {
            Id = 1,
            Nome = "Motocross",
            TipoCronometragem = "CIRCUITO",
            Ativo = true
        };
        var modalidadeEnduro = new Modalidade
        {
            Id = 2,
            Nome = "Enduro",
            TipoCronometragem = "ENDURO",
            Ativo = true
        };
        db.Modalidades.AddRange(modalidadeCircuito, modalidadeEnduro);
        db.SaveChanges();

        // Categorias
        var categoria1 = new Categoria { Id = 1, Nome = "MX1", Ativo = true };
        var categoria2 = new Categoria { Id = 2, Nome = "MX2", Ativo = true };
        db.Categorias.AddRange(categoria1, categoria2);
        db.SaveChanges();

        // ✅ Eventos - CORRIGIDO com campos obrigatórios
        var evento = new Evento
        {
            Id = 1,
            Nome = "Campeonato Teste",
            IdModalidade = 1,
            DataInicio = DateTime.Today,
            DataFim = DateTime.Today.AddDays(1),
            Status = "ABERTO",
            // ✅ Campos obrigatórios que estavam faltando:
            Local = "Autódromo de Interlagos",
            Cidade = "São Paulo",
            Uf = "SP",
            Ativo = true
        };
        db.Eventos.Add(evento);
        db.SaveChanges();

        // Etapas
        var etapa = new Etapa
        {
            Id = 1,
            IdEvento = 1,
            NumeroEtapa = 1,
            Nome = "Etapa 1",
            DataHora = DateTime.Today.AddHours(8),
            Status = "EM_ANDAMENTO",
            NumeroVoltas = 10,
            NumeroEspeciais = 3,
            Ativo = true
        };
        db.Etapas.Add(etapa);
        db.SaveChanges();

        // Dispositivos Coletores
        var dispositivo = new DispositivoColetor
        {
            Id = 1,
            DeviceId = "TEST-DEVICE-001",
            Nome = "Dispositivo Teste",
            Tipo = "P",
            IdEvento = 1,
            IdEtapa = 1,
            StatusConexao = "ONLINE",
            Ativo = true
        };
        db.DispositivosColetores.Add(dispositivo);
        db.SaveChanges();

        // Pilotos
        var piloto1 = new Piloto
        {
            Id = 1,
            Nome = "João Silva",
            Cpf = "12345678901",
            Email = "joao@teste.com",
            Telefone = "11999999999",
            Cidade = "São Paulo",
            Uf = "SP",
            DataNascimento = new DateTime(1990, 1, 1),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Teste@123"),
            PasswordSalt = "salt",
            Ativo = true
        };
        var piloto2 = new Piloto
        {
            Id = 2,
            Nome = "Pedro Santos",
            Cpf = "12345678902",
            Email = "pedro@teste.com",
            Telefone = "11999999998",
            Cidade = "Rio de Janeiro",
            Uf = "RJ",
            DataNascimento = new DateTime(1985, 5, 15),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Teste@123"),
            PasswordSalt = "salt",
            Ativo = true
        };
        db.Pilotos.AddRange(piloto1, piloto2);
        db.SaveChanges();

        // Inscrições
        var inscricao = new Inscricao
        {
            Id = 1,
            IdPiloto = 1,
            IdEvento = 1,
            IdEtapa = 1,
            IdCategoria = 1,
            NumeroMoto = 42,
            StatusPagamento = "CONFIRMADO",
            Ativo = true
        };
        db.Inscricoes.Add(inscricao);
        db.SaveChanges();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new Task DisposeAsync() => Task.CompletedTask;
}

// ========== TEST AUTH HANDLER (no mesmo arquivo ou separado) ==========
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Se não tem header Authorization, retorna NoResult (vai dar 401)
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "Test Admin"),
            new Claim(ClaimTypes.Email, "admin@teste.com"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Organizador"),
            new Claim(ClaimTypes.Role, "Coletor"), // Para o LeiturasController
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}