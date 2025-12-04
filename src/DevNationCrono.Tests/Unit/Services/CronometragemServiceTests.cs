using DevNationCrono.API.Exceptions;  // Adicionar este using
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Implementations;
using DevNationCrono.API.Services.Interfaces;
using DevNationCrono.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DevNationCrono.Tests.Unit.Services;

public class CronometragemServiceTests
{
    private readonly Mock<ITempoRepository> _tempoRepositoryMock;
    private readonly Mock<IEtapaRepository> _etapaRepositoryMock;
    private readonly Mock<IInscricaoRepository> _inscricaoRepositoryMock;
    private readonly Mock<IDispositivoColetorRepository> _dispositivoRepositoryMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ILogger<CronometragemService>> _loggerMock;
    private readonly Mock<IResultadoCircuitoService> _resultadoCircuitoServiceMock;
    private readonly Mock<INotificacaoTempoRealService> _notificacaoServiceMock;
    private readonly CronometragemService _service;

    public CronometragemServiceTests()
    {
        _tempoRepositoryMock = new Mock<ITempoRepository>();
        _etapaRepositoryMock = new Mock<IEtapaRepository>();
        _inscricaoRepositoryMock = new Mock<IInscricaoRepository>();
        _dispositivoRepositoryMock = new Mock<IDispositivoColetorRepository>();
        _tokenServiceMock = new Mock<ITokenService>();
        _loggerMock = new Mock<ILogger<CronometragemService>>();
        _resultadoCircuitoServiceMock = new Mock<IResultadoCircuitoService>();
        _notificacaoServiceMock = new Mock<INotificacaoTempoRealService>();

        _service = new CronometragemService(
            _tempoRepositoryMock.Object,
            _dispositivoRepositoryMock.Object,
            _etapaRepositoryMock.Object,
            _inscricaoRepositoryMock.Object,
            _tokenServiceMock.Object,
            _loggerMock.Object,
            _resultadoCircuitoServiceMock.Object,
            _notificacaoServiceMock.Object
        );
    }

    #region ProcessarLeituraAsync - Circuito

    [Fact]
    public async Task ProcessarLeituraAsync_CircuitoPrimeiraPassagem_DeveRetornarSucesso()
    {
        // Arrange
        var leitura = TestDataBuilder.CriarLeituraDto(numeroMoto: 42, tipo: "P", volta: 1);
        var etapa = TestDataBuilder.CriarEtapa(tipoCronometragem: "CIRCUITO");
        var inscricao = TestDataBuilder.CriarInscricao(numeroMoto: 42);
        var dispositivo = new DispositivoColetor
        {
            Id = 1,
            DeviceId = "TEST-DEVICE-001",
            Ativo = true,
            Tipo = "P"
        };

        // Setup dos mocks NA ORDEM CORRETA
        _dispositivoRepositoryMock
            .Setup(r => r.GetByDeviceIdEtapaAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(dispositivo);

        _etapaRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(etapa);

        _tempoRepositoryMock
            .Setup(r => r.ExisteLeituraAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _tempoRepositoryMock
            .Setup(r => r.ExisteLeituraSimilarAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        _inscricaoRepositoryMock
            .Setup(r => r.GetByEventoAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Inscricao> { inscricao });

        _tempoRepositoryMock
            .Setup(r => r.GetUltimaPassagemAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((Tempo?)null);

        _tempoRepositoryMock
            .Setup(r => r.GetTotalVoltasAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(0);

        // ✅ IMPORTANTE: Configurar AddAsync para retornar o tempo
        _tempoRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Tempo>()))
            .ReturnsAsync((Tempo t) =>
            {
                t.Id = 1;
                return t;
            });

        // Mock para incrementar leituras
        _dispositivoRepositoryMock
            .Setup(r => r.IncrementarLeiturasAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Mock para notificações (não deve falhar)
        _resultadoCircuitoServiceMock
            .Setup(r => r.AtualizarResultadoIncrementalAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        _resultadoCircuitoServiceMock
            .Setup(r => r.GetResumoTempoRealAsync(It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(new List<ResumoTempoRealDto>());

        // Act
        var resultado = await _service.ProcessarLeituraAsync(leitura);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Status.Should().Be("OK");
        resultado.NumeroMoto.Should().Be(42);

        _tempoRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Tempo>()), Times.Once);
    }

    [Fact]
    public async Task ProcessarLeituraAsync_CircuitoSegundaPassagem_DeveCalcularTempo()
    {
        // Arrange
        var horaLargada = DateTime.UtcNow.AddMinutes(-5);
        var horaPrimeiraPassagem = horaLargada.AddSeconds(85);
        var horaSegundaPassagem = horaPrimeiraPassagem.AddSeconds(82.5);

        var leitura = TestDataBuilder.CriarLeituraDto(numeroMoto: 42, tipo: "P", volta: 2);
        leitura.Timestamp = horaSegundaPassagem;

        var etapa = TestDataBuilder.CriarEtapa(tipoCronometragem: "CIRCUITO");
        etapa.HoraLargada = horaLargada;

        var inscricao = TestDataBuilder.CriarInscricao(numeroMoto: 42);
        var dispositivo = new DispositivoColetor { Id = 1, DeviceId = "TEST-DEVICE-001", Ativo = true };
        var passagemAnterior = TestDataBuilder.CriarTempo(
            numeroMoto: 42,
            volta: 1,
            timestamp: horaPrimeiraPassagem);

        _etapaRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(etapa);
        _dispositivoRepositoryMock.Setup(r => r.GetByDeviceIdEtapaAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(dispositivo);
        _inscricaoRepositoryMock.Setup(r => r.GetByEventoAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Inscricao> { inscricao });
        _tempoRepositoryMock.Setup(r => r.ExisteLeituraAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _tempoRepositoryMock.Setup(r => r.ExisteLeituraSimilarAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(false);
        _tempoRepositoryMock.Setup(r => r.GetUltimaPassagemAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(passagemAnterior);
        _tempoRepositoryMock.Setup(r => r.GetTotalVoltasAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(1);
        _tempoRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Tempo>()))
            .ReturnsAsync((Tempo t) => { t.Id = 1; return t; });

        // Mock para notificações
        _resultadoCircuitoServiceMock.Setup(r => r.GetResumoTempoRealAsync(It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(new List<ResumoTempoRealDto>());
        _resultadoCircuitoServiceMock.Setup(r => r.AtualizarResultadoIncrementalAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _service.ProcessarLeituraAsync(leitura);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Status.Should().Be("OK");
        resultado.TempoCalculadoSegundos.Should().BeApproximately(82.5m, 0.1m);
    }

    [Fact]
    public async Task ProcessarLeituraAsync_LeituraDuplicada_DeveRetornarDuplicada()
    {
        // Arrange
        var leitura = TestDataBuilder.CriarLeituraDto();
        var etapa = TestDataBuilder.CriarEtapa(tipoCronometragem: "CIRCUITO");
        var dispositivo = new DispositivoColetor { Id = 1, DeviceId = "TEST-DEVICE-001", Ativo = true };

        _etapaRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(etapa);
        _dispositivoRepositoryMock.Setup(r => r.GetByDeviceIdEtapaAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(dispositivo);
        _tempoRepositoryMock.Setup(r => r.ExisteLeituraAsync(It.IsAny<string>()))
            .ReturnsAsync(true); // Duplicada!

        // Act
        var resultado = await _service.ProcessarLeituraAsync(leitura);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Status.Should().Be("DUPLICADA");

        _tempoRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Tempo>()), Times.Never);
    }

    [Fact]
    public async Task ProcessarLeituraAsync_DispositivoInativo_DeveRetornarErro()
    {
        // Arrange
        var leitura = TestDataBuilder.CriarLeituraDto();
        var dispositivo = new DispositivoColetor { Id = 1, DeviceId = "TEST-DEVICE-001", Ativo = false };

        _dispositivoRepositoryMock.Setup(r => r.GetByDeviceIdEtapaAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(dispositivo);

        // Act
        var resultado = await _service.ProcessarLeituraAsync(leitura);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Status.Should().Be("ERRO");
        resultado.Mensagem.Should().Contain("desativado");
    }

    [Fact]
    public async Task ProcessarLeituraAsync_DispositivoNaoCadastrado_DeveRetornarErro()
    {
        // Arrange
        var leitura = TestDataBuilder.CriarLeituraDto();

        _dispositivoRepositoryMock.Setup(r => r.GetByDeviceIdEtapaAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((DispositivoColetor?)null);

        // Act
        var resultado = await _service.ProcessarLeituraAsync(leitura);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Status.Should().Be("ERRO");
        resultado.Mensagem.Should().Contain("não está cadastrado");
    }

    #endregion

    #region ProcessarLeituraAsync - Enduro

    [Fact]
    public async Task ProcessarLeituraAsync_EnduroEntrada_DeveRegistrarSemCalcularTempo()
    {
        // Arrange
        var leitura = TestDataBuilder.CriarLeituraDto(
            tipo: "E",
            idEspecial: 1,
            volta: 1);

        var etapa = TestDataBuilder.CriarEtapa(
            tipoCronometragem: "ENDURO",
            numeroEspeciais: 3,
            numeroVoltas: 2);

        var inscricao = TestDataBuilder.CriarInscricao(numeroMoto: 42);
        var dispositivo = new DispositivoColetor { Id = 1, DeviceId = "TEST-DEVICE-001", Ativo = true, Tipo = "E" };

        _etapaRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(etapa);
        _dispositivoRepositoryMock.Setup(r => r.GetByDeviceIdEtapaAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(dispositivo);
        _inscricaoRepositoryMock.Setup(r => r.GetByEventoAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Inscricao> { inscricao });
        _tempoRepositoryMock.Setup(r => r.ExisteLeituraAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _tempoRepositoryMock.Setup(r => r.ExisteLeituraSimilarAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(false);
        _tempoRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Tempo>()))
            .ReturnsAsync((Tempo t) => { t.Id = 1; return t; });

        // Act
        var resultado = await _service.ProcessarLeituraAsync(leitura);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Status.Should().Be("OK");
        resultado.Tipo.Should().Be("E");
        resultado.TempoCalculadoSegundos.Should().BeNull(); // Entrada não calcula tempo
    }

    [Fact]
    public async Task ProcessarLeituraAsync_EnduroSaida_DeveCalcularTempoEspecial()
    {
        // Arrange
        var horaEntrada = DateTime.UtcNow.AddMinutes(-5);
        var horaSaida = horaEntrada.AddSeconds(323.456); // 5:23.456

        var leitura = TestDataBuilder.CriarLeituraDto(
            tipo: "S",
            idEspecial: 1,
            volta: 1);
        leitura.Timestamp = horaSaida;

        var etapa = TestDataBuilder.CriarEtapa(
            tipoCronometragem: "ENDURO",
            numeroEspeciais: 3,
            numeroVoltas: 2);

        var inscricao = TestDataBuilder.CriarInscricao(numeroMoto: 42);
        var dispositivo = new DispositivoColetor { Id = 1, DeviceId = "TEST-DEVICE-001", Ativo = true, Tipo = "S" };
        var tempoEntrada = TestDataBuilder.CriarTempo(
            numeroMoto: 42,
            tipo: "E",
            volta: 1,
            timestamp: horaEntrada);

        _etapaRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(etapa);
        _dispositivoRepositoryMock.Setup(r => r.GetByDeviceIdEtapaAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(dispositivo);
        _inscricaoRepositoryMock.Setup(r => r.GetByEventoAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Inscricao> { inscricao });
        _tempoRepositoryMock.Setup(r => r.ExisteLeituraAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _tempoRepositoryMock.Setup(r => r.ExisteLeituraSimilarAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(false);
        _tempoRepositoryMock.Setup(r => r.GetEntradaEspecialAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(tempoEntrada);
        _tempoRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Tempo>()))
            .ReturnsAsync((Tempo t) => { t.Id = 1; return t; });

        // Act
        var resultado = await _service.ProcessarLeituraAsync(leitura);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Status.Should().Be("OK");
        resultado.Tipo.Should().Be("S");
        resultado.TempoCalculadoSegundos.Should().BeApproximately(323.456m, 0.01m);
    }

    #endregion

    #region ProcessarLoteLeituraAsync

    [Fact]
    public async Task ProcessarLoteLeituraAsync_MultiplasLeituras_DeveProcessarTodas()
    {
        // Arrange
        var etapa = TestDataBuilder.CriarEtapa(tipoCronometragem: "CIRCUITO");
        var dispositivo = new DispositivoColetor { Id = 1, DeviceId = "TEST-DEVICE-001", Ativo = true };
        var inscricao42 = TestDataBuilder.CriarInscricao(numeroMoto: 42);
        var inscricao15 = TestDataBuilder.CriarInscricao(numeroMoto: 15);

        var lote = new LoteLeituraDto
        {
            DeviceId = "TEST-DEVICE-001",
            IdEtapa = 1,
            Leituras = new List<LeituraItemDto>
            {
                new() { NumeroMoto = 42, Timestamp = DateTime.UtcNow.AddSeconds(-10), Tipo = "P", Volta = 1 },
                new() { NumeroMoto = 15, Timestamp = DateTime.UtcNow.AddSeconds(-8), Tipo = "P", Volta = 1 },
                new() { NumeroMoto = 42, Timestamp = DateTime.UtcNow.AddSeconds(-5), Tipo = "P", Volta = 2 }
            }
        };

        _etapaRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(etapa);
        _dispositivoRepositoryMock.Setup(r => r.GetByDeviceIdEtapaAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(dispositivo);
        _inscricaoRepositoryMock.Setup(r => r.GetByEventoAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Inscricao> { inscricao42, inscricao15 });
        _tempoRepositoryMock.Setup(r => r.ExisteLeituraAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _tempoRepositoryMock.Setup(r => r.ExisteLeituraSimilarAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(false);
        _tempoRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Tempo>()))
            .ReturnsAsync((Tempo t) => { t.Id = 1; return t; });

        // Mock para notificações
        _resultadoCircuitoServiceMock.Setup(r => r.GetResumoTempoRealAsync(It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(new List<ResumoTempoRealDto>());
        _resultadoCircuitoServiceMock.Setup(r => r.AtualizarResultadoIncrementalAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _service.ProcessarLoteLeituraAsync(lote);

        // Assert
        resultado.Should().NotBeNull();
        resultado.TotalRecebidas.Should().Be(3);
        resultado.TotalProcessadas.Should().Be(3);
        resultado.TotalDuplicadas.Should().Be(0);
        resultado.TotalErros.Should().Be(0);
    }

    #endregion
}