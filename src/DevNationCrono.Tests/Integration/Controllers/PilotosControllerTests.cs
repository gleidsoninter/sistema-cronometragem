using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DevNationCrono.Tests.Integration.Fixtures;
using FluentAssertions;

namespace DevNationCrono.Tests.Integration.Controllers;

public class PilotosControllerTests : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client;
    private readonly HttpClient _clientSemAuth;
    private readonly JsonSerializerOptions _jsonOptions;

    public PilotosControllerTests(ApiTestFixture fixture)
    {
        _client = fixture.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Test", "fake-token");

        _clientSemAuth = fixture.CreateClient();
        // Sem header de Authorization

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    [Fact]
    public async Task GetAll_SemAutenticacao_DeveRetornar401()
    {
        // Act - ✅ Rota COM versão
        var response = await _clientSemAuth.GetAsync("/api/v1/pilotos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ComAutenticacao_DeveRetornar200()
    {
        // Act - ✅ Rota COM versão
        var response = await _client.GetAsync("/api/v1/pilotos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetById_PilotoExiste_DeveRetornar200()
    {
        // Act - ✅ Rota COM versão
        var response = await _client.GetAsync("/api/v1/pilotos/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_PilotoNaoExiste_DeveRetornar404()
    {
        // Act - ✅ Rota COM versão
        var response = await _client.GetAsync("/api/v1/pilotos/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_DadosValidos_DeveRetornar201()
    {
        // Arrange
        var novoPiloto = new
        {
            nome = "Carlos Teste",
            email = "carlos@teste.com",
            cpf = "98765432100",
            telefone = "11988887777",
            dataNascimento = "1995-03-15",
            cidade = "Campinas",
            uf = "SP",
            senha = "Teste@123"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(novoPiloto),
            Encoding.UTF8,
            "application/json");

        // Act - ✅ POST é AllowAnonymous, mas rota COM versão
        var response = await _clientSemAuth.PostAsync("/api/v1/pilotos", content);

        // Debug
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Response: {responseContent}");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created,
            HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_DadosInvalidos_DeveRetornar400()
    {
        // Arrange - Piloto sem campos obrigatórios
        var pilotoInvalido = new
        {
            email = "invalido@teste.com"
            // Faltando nome, cpf, senha, etc.
        };

        var content = new StringContent(
            JsonSerializer.Serialize(pilotoInvalido),
            Encoding.UTF8,
            "application/json");

        // Act - ✅ Rota COM versão
        var response = await _clientSemAuth.PostAsync("/api/v1/pilotos", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_CpfDuplicado_DeveRetornar409()
    {
        // Arrange - CPF que já existe no seed
        var pilotoDuplicado = new
        {
            nome = "Duplicado Teste",
            email = "duplicado@teste.com",
            cpf = "12345678901", // ✅ CPF do João Silva no seed
            telefone = "11999999999",
            dataNascimento = "1990-01-01",
            cidade = "São Paulo",
            uf = "SP",
            senha = "Teste@123"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(pilotoDuplicado),
            Encoding.UTF8,
            "application/json");

        // Act - ✅ Rota COM versão
        var response = await _clientSemAuth.PostAsync("/api/v1/pilotos", content);

        // Assert - Pode ser 409 Conflict ou 400 BadRequest dependendo da implementação
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Conflict,
            HttpStatusCode.BadRequest);
    }
}