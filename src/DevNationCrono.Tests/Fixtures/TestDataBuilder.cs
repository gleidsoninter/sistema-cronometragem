using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;

namespace DevNationCrono.Tests.Fixtures;

public static class TestDataBuilder
{
    public static Piloto CriarPiloto(
        int id = 1,
        string nome = "João Silva",
        string cpf = "12345678900")
    {
        return new Piloto
        {
            Id = id,
            Nome = nome,
            Cpf = cpf,
            DataNascimento = new DateTime(1990, 5, 15),
            Email = $"piloto{id}@teste.com",
            Telefone = "11999999999",
            Cidade = "São Paulo",
            Uf = "SP",
            PasswordHash = "hash_teste",
            PasswordSalt = "salt_teste",
            Ativo = true,
            DataCriacao = DateTime.UtcNow
        };
    }

    public static Modalidade CriarModalidade(
        int id = 1,
        string nome = "Motocross",
        string tipoCronometragem = "CIRCUITO")
    {
        return new Modalidade
        {
            Id = id,
            Nome = nome,
            TipoCronometragem = tipoCronometragem,
            Ativo = true
        };
    }

    public static Evento CriarEvento(
        int id = 1,
        string nome = "Campeonato Teste",
        int idModalidade = 1)
    {
        return new Evento
        {
            Id = id,
            Nome = nome,
            IdModalidade = idModalidade,
            DataInicio = DateTime.Today,
            DataFim = DateTime.Today.AddDays(1),
            Status = "ABERTO",
            Ativo = true
        };
    }

    public static Etapa CriarEtapa(
        int id = 1,
        int idEvento = 1,
        string tipoCronometragem = "CIRCUITO",
        int numeroVoltas = 10,
        int numeroEspeciais = 0)
    {
        var modalidade = new Modalidade
        {
            Id = 1,
            Nome = tipoCronometragem == "CIRCUITO" ? "Motocross" : "Enduro",
            TipoCronometragem = tipoCronometragem,
            Ativo = true
        };

        var evento = new Evento
        {
            Id = idEvento,
            Nome = "Evento Teste",
            IdModalidade = 1,
            Modalidade = modalidade,
            DataInicio = DateTime.Today,
            DataFim = DateTime.Today.AddDays(1),
            Status = "ABERTO",
            Ativo = true
        };

        return new Etapa
        {
            Id = id,
            IdEvento = idEvento,
            Evento = evento,
            NumeroEtapa = 1,
            Nome = "Etapa 1",
            DataHora = DateTime.Today.AddHours(8),
            NumeroVoltas = numeroVoltas,
            NumeroEspeciais = numeroEspeciais > 0 ? numeroEspeciais : null,
            NumeroVoltasEnduro = tipoCronometragem == "ENDURO" ? numeroVoltas : null,
            VoltaReconhecimento = tipoCronometragem != "CIRCUITO", // PrimeiraVoltaValida é o inverso
            PenalidadeSegundos = 1200,
            Status = "NAO_INICIADA",
            Ativo = true
        };
    }

    public static Categoria CriarCategoria(int id = 1, string nome = "MX1")
    {
        return new Categoria
        {
            Id = id,
            Nome = nome,
            Ativo = true
        };
    }

    public static Inscricao CriarInscricao(
        int id = 1,
        int idPiloto = 1,
        int idEvento = 1,
        int idEtapa = 1,
        int idCategoria = 1,
        int numeroMoto = 42)
    {
        return new Inscricao
        {
            Id = id,
            IdPiloto = idPiloto,
            IdEvento = idEvento,
            IdEtapa = idEtapa,
            IdCategoria = idCategoria,
            NumeroMoto = numeroMoto,
            StatusPagamento = "CONFIRMADO",  // ✅ Corrigido: Status -> StatusPagamento
            Piloto = CriarPiloto(idPiloto),
            Categoria = CriarCategoria(idCategoria),
            Ativo = true
        };
    }

    public static Tempo CriarTempo(
        long id = 1,
        int idEtapa = 1,
        int numeroMoto = 42,
        string tipo = "P",
        int volta = 1,
        DateTime? timestamp = null,
        decimal? tempoCalculado = null)
    {
        return new Tempo
        {
            Id = id,
            IdEtapa = idEtapa,
            NumeroMoto = numeroMoto,
            Tipo = tipo,
            Volta = volta,
            Timestamp = timestamp ?? DateTime.UtcNow,
            TempoCalculadoSegundos = tempoCalculado,
            TempoFormatado = tempoCalculado.HasValue ? FormatarTempo(tempoCalculado.Value) : null,
            Descartada = false,
            Sincronizado = true
        };
    }

    public static LeituraDto CriarLeituraDto(
        int numeroMoto = 42,
        string tipo = "P",
        int idEtapa = 1,
        int volta = 1,
        int? idEspecial = null)
    {
        return new LeituraDto
        {
            NumeroMoto = numeroMoto,
            Timestamp = DateTime.UtcNow,
            Tipo = tipo,
            IdEtapa = idEtapa,
            Volta = volta,
            IdEspecial = idEspecial,
            DeviceId = "TEST-DEVICE-001"
        };
    }

    public static List<Tempo> CriarPassagensCircuito(
        int idEtapa,
        int numeroMoto,
        int quantidadeVoltas,
        DateTime horaLargada,
        decimal tempoMedioVoltaSegundos = 80)
    {
        var passagens = new List<Tempo>();
        var timestamp = horaLargada;

        for (int volta = 1; volta <= quantidadeVoltas; volta++)
        {
            // Variar tempo um pouco
            var variacao = (decimal)(new Random().NextDouble() * 5 - 2.5);
            var tempoVolta = tempoMedioVoltaSegundos + variacao;
            timestamp = timestamp.AddSeconds((double)tempoVolta);

            passagens.Add(new Tempo
            {
                Id = volta,
                IdEtapa = idEtapa,
                NumeroMoto = numeroMoto,
                Tipo = "P",
                Volta = volta,
                Timestamp = timestamp,
                TempoCalculadoSegundos = volta == 1 ? null : tempoVolta,
                TempoFormatado = volta == 1 ? "LARGADA" : FormatarTempo(tempoVolta),
                Descartada = false
            });
        }

        return passagens;
    }

    public static DispositivoColetor CriarDispositivo(
        int id = 1,
        string deviceId = "TEST-DEVICE-001",
        string tipo = "P",
        bool ativo = true)
    {
        return new DispositivoColetor
        {
            Id = id,
            DeviceId = deviceId,
            StatusConexao = "ONLINE",
            Nome = deviceId,
            Tipo = tipo,
            Ativo = ativo
        };
    }

    private static string FormatarTempo(decimal segundos)
    {
        var ts = TimeSpan.FromSeconds((double)segundos);
        return $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
    }
}