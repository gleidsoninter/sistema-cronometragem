using NBomber.Contracts.Stats;
using NBomber.CSharp;
using System.Text;
using System.Text.Json;

namespace DevNationCrono.Tests.LoadTests;

public class LeiturasLoadTest
{
    [Fact(Skip = "Executar manualmente para testes de carga")]
    public void TesteDeCarga_100LeiturasParalelas()
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        var scenario = Scenario.Create("enviar_leituras", async context =>
        {
            var leitura = new
            {
                numeroMoto = Random.Shared.Next(1, 100),
                timestamp = DateTime.UtcNow.ToString("o"),
                tipo = "P",
                idEtapa = 1,
                deviceId = $"LOAD-TEST-{context.ScenarioInfo.ThreadNumber}"
            };

            var json = JsonSerializer.Serialize(leitura);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/leituras")
            {
                Content = content
            };
            request.Headers.Add("Authorization", "Bearer token-teste");

            try
            {
                var response = await httpClient.SendAsync(request);
                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            }
            catch
            {
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Verificações
        var scenarioStats = stats.ScenarioStats[0];

        // 95% das requisições devem ser bem-sucedidas
        scenarioStats.Ok.Request.Percent.Should().BeGreaterThan(95);

        // Latência P99 deve ser menor que 500ms
        scenarioStats.Ok.Latency.Percent99.Should().BeLessThan(500);
    }

    [Fact(Skip = "Executar manualmente para testes de carga")]
    public void TesteDeEstresse_AumentandoCarga()
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        var scenario = Scenario.Create("estresse_leituras", async context =>
        {
            var leitura = new
            {
                numeroMoto = Random.Shared.Next(1, 100),
                timestamp = DateTime.UtcNow.ToString("o"),
                tipo = "P",
                idEtapa = 1,
                deviceId = $"STRESS-{context.ScenarioInfo.ThreadNumber}"
            };

            var json = JsonSerializer.Serialize(leitura);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/leituras")
            {
                Content = content
            };
            request.Headers.Add("Authorization", "Bearer token-teste");

            try
            {
                var response = await httpClient.SendAsync(request);
                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            }
            catch
            {
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            // Rampa de subida
            Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),
            // Carga constante
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            // Pico
            Simulation.Inject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),
            // Descida
            Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("./load-test-reports")
            .WithReportFormats(ReportFormat.Html)
            .Run();
    }
}