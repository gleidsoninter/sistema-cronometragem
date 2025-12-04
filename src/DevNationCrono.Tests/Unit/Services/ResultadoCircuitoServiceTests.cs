using DevNationCrono.Tests.Fixtures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DevNationCrono.Tests.Unit.Services;

public class ResultadoCircuitoServiceTests
{
    private readonly Mock<ITempoRepository> _tempoRepositoryMock;
    private readonly Mock<IEtapaRepository> _etapaRepositoryMock;
    private readonly Mock<IInscricaoRepository> _inscricaoRepositoryMock;
    private readonly Mock<ILogger<ResultadoCircuitoService>> _loggerMock;
    private readonly IMemoryCache _cache;
    private readonly ResultadoCircuitoService _service;

    public ResultadoCircuitoServiceTests()
    {
        _tempoRepositoryMock = new Mock<ITempoRepository>();
        _etapaRepositoryMock = new Mock<IEtapaRepository>();
        _inscricaoRepositoryMock = new Mock<IInscricaoRepository>();
        _loggerMock = new Mock<ILogger<ResultadoCircuitoService>>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        _service = new ResultadoCircuitoService(
            _tempoRepositoryMock.Object,
            _etapaRepositoryMock.Object,
            _inscricaoRepositoryMock.Object,
            _cache,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task CalcularClassificacaoGeralAsync_ComDoisPilotos_DeveOrdenarPorVoltas()
    {
        // Arrange
        var etapa = TestDataBuilder.CriarEtapa(tipoCronometragem: "CIRCUITO");
        etapa.HoraLargada = DateTime.UtcNow.AddMinutes(-20);

        var inscricoes = new List<Inscricao>
            {
                TestDataBuilder.CriarInscricao(id: 1, idPiloto: 1, numeroMoto: 42),
                TestDataBuilder.CriarInscricao(id: 2, idPiloto: 2, numeroMoto: 15)
            };

        // Piloto 42: 5 voltas
        var passagens42 = TestDataBuilder.CriarPassagensCircuito(
            idEtapa: 1,
            numeroMoto: 42,
            quantidadeVoltas: 5,
            horaLargada: etapa.HoraLargada.Value);

        // Piloto 15: 4 voltas
        var passagens15 = TestDataBuilder.CriarPassagensCircuito(
            idEtapa: 1,
            numeroMoto: 15,
            quantidadeVoltas: 4,
            horaLargada: etapa.HoraLargada.Value);

        var todasPassagens = passagens42.Concat(passagens15).ToList();

        _etapaRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(etapa);
        _inscricaoRepositoryMock.Setup(r => r.GetByEtapaAsync(1))
            .ReturnsAsync(inscricoes);
        _tempoRepositoryMock.Setup(r => r.GetByEtapaAsync(1))
            .ReturnsAsync(todasPassagens);

        // Act
        var resultado = await _service.CalcularClassificacaoGeralAsync(1);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Classificacao.Should().HaveCount(2);
        resultado.Classificacao[0].NumeroMoto.Should().Be(42); // Mais voltas = 1º
        resultado.Classificacao[0].VoltasCompletadas.Should().Be(5);
        resultado.Classificacao[1].NumeroMoto.Should().Be(15);
        resultado.Classificacao[1].VoltasCompletadas.Should().Be(4);
    }

    [Fact]
    public async Task CalcularClassificacaoGeralAsync_MesmasVoltas_DeveDesempatarPorTempo()
    {
        // Arrange
        var etapa = TestDataBuilder.CriarEtapa(tipoCronometragem: "CIRCUITO");
        etapa.HoraLargada = DateTime.UtcNow.AddMinutes(-20);

        var inscricoes = new List<Inscricao>
            {
                TestDataBuilder.CriarInscricao(id: 1, idPiloto: 1, numeroMoto: 42),
                TestDataBuilder.CriarInscricao(id: 2, idPiloto: 2, numeroMoto: 15)
            };

        // Ambos com 5 voltas, mas 42 mais rápido
        var passagens42 = TestDataBuilder.CriarPassagensCircuito(
            idEtapa: 1,
            numeroMoto: 42,
            quantidadeVoltas: 5,
            horaLargada: etapa.HoraLargada.Value,
            tempoMedioVoltaSegundos: 75); // Mais rápido

        var passagens15 = TestDataBuilder.CriarPassagensCircuito(
            idEtapa: 1,
            numeroMoto: 15,
            quantidadeVoltas: 5,
            horaLargada: etapa.HoraLargada.Value,
            tempoMedioVoltaSegundos: 80); // Mais lento

        var todasPassagens = passagens42.Concat(passagens15).ToList();

        _etapaRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(etapa);
        _inscricaoRepositoryMock.Setup(r => r.GetByEtapaAsync(1))
            .ReturnsAsync(inscricoes);
        _tempoRepositoryMock.Setup(r => r.GetByEtapaAsync(1))
            .ReturnsAsync(todasPassagens);

        // Act
        var resultado = await _service.CalcularClassificacaoGeralAsync(1);

        // Assert
        resultado.Classificacao.Should().HaveCount(2);
        resultado.Classificacao[0].NumeroMoto.Should().Be(42); // Mesmo voltas, menor tempo
        resultado.Classificacao[1].NumeroMoto.Should().Be(15);
        resultado.Classificacao[0].TempoTotalSegundos.Should().BeLessThan(
            resultado.Classificacao[1].TempoTotalSegundos);
    }

    [Fact]
    public async Task CalcularClassificacaoGeralAsync_PilotoSemPassagem_DeveMarcarNaoLargou()
    {
        // Arrange
        var etapa = TestDataBuilder.CriarEtapa(tipoCronometragem: "CIRCUITO");
        etapa.HoraLargada = DateTime.UtcNow.AddMinutes(-20);

        var inscricoes = new List<Inscricao>
            {
                TestDataBuilder.CriarInscricao(id: 1, idPiloto: 1, numeroMoto: 42)
            };

        _etapaRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(etapa);
        _inscricaoRepositoryMock.Setup(r => r.GetByEtapaAsync(1))
            .ReturnsAsync(inscricoes);
        _tempoRepositoryMock.Setup(r => r.GetByEtapaAsync(1))
            .ReturnsAsync(new List<Tempo>()); // Sem passagens

        // Act
        var resultado = await _service.CalcularClassificacaoGeralAsync(1);

        // Assert
        resultado.Classificacao.Should().HaveCount(1);
        resultado.Classificacao[0].Status.Should().Be("NAO_LARGOU");
        resultado.Classificacao[0].VoltasCompletadas.Should().Be(0);
    }
}
