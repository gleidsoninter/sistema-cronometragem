using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DevNationCrono.Tests.Integration.Fixtures;
using FluentAssertions;

namespace DevNationCrono.Tests.Integration.Controllers;

public class LeiturasControllerTests : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client;
    private readonly HttpClient _clientSemAuth;

    public LeiturasControllerTests(ApiTestFixture fixture)
    {
        _client = fixture.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Test", "fake-token");

        _clientSemAuth = fixture.CreateClient();
    }

    [Fact]
    public async Task PostLeitura_SemAutenticacao_DeveRetornar401()
    {
        // Arrange
        var leitura = new
        {
            numeroMoto = 42,
            timestamp = DateTime.UtcNow.ToString("o"),
            tipo = "P",
            idEtapa = 1,
            deviceId = "TEST-DEVICE-001"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(leitura),
            Encoding.UTF8,
            "application/json");

        // Act - ✅ Rota COM versão, sem auth
        var response = await _clientSemAuth.PostAsync("/api/v1/leituras", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostLeitura_CircuitoValida_DeveRetornar200()
    {
        // Arrange
        var leitura = new
        {
            numeroMoto = 42,
            timestamp = DateTime.UtcNow.ToString("o"),
            tipo = "P",
            idEtapa = 1,
            deviceId = "TEST-DEVICE-001"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(leitura),
            Encoding.UTF8,
            "application/json");

        // Act - ✅ Rota COM versão, com auth
        var response = await _client.PostAsync("/api/v1/leituras", content);

        // Debug
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Response: {responseContent}");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Created,
            HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostLeitura_TipoInvalido_DeveRetornar400()
    {
        // Arrange - Tipo inválido para circuito
        var leitura = new
        {
            numeroMoto = 42,
            timestamp = DateTime.UtcNow.ToString("o"),
            tipo = "X", // ✅ Tipo inválido
            idEtapa = 1,
            deviceId = "TEST-DEVICE-001"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(leitura),
            Encoding.UTF8,
            "application/json");

        // Act - ✅ Rota COM versão
        var response = await _client.PostAsync("/api/v1/leituras", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}