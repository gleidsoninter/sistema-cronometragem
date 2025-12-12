using AutoMapper;
using DevNationCrono.API.Exceptions;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Interfaces;

namespace DevNationCrono.API.Services.Implementations;
public class EtapaService : IEtapaService
{
    private readonly IEtapaRepository _etapaRepository;
    private readonly IEventoRepository _eventoRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<EtapaService> _logger;

    private static readonly string[] StatusValidos =
    {
        "AGENDADA", "EM_ANDAMENTO", "FINALIZADA", "CANCELADA"
    };

    public EtapaService(
        IEtapaRepository etapaRepository,
        IEventoRepository eventoRepository,
        IMapper mapper,
        ILogger<EtapaService> logger)
    {
        _etapaRepository = etapaRepository;
        _eventoRepository = eventoRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<EtapaDto?> GetByIdAsync(int id)
    {
        var etapa = await _etapaRepository.GetByIdAsync(id);

        if (etapa == null)
            return null;

        var dto = MapearParaDto(etapa);
        await PreencherContagens(dto);

        return dto;
    }

    public async Task<List<EtapaDto>> GetByEventoAsync(int idEvento)
    {
        try
        {
            var etapas = await _etapaRepository.GetByEventoAsync(idEvento);
            var dtos = new List<EtapaDto>();

            foreach (var etapa in etapas)
            {
                var dto = MapearParaDto(etapa);
                await PreencherContagens(dto);
                dtos.Add(dto);
            }

            return dtos;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }    

    }

    public async Task<EtapaDto> CreateAsync(EtapaCreateDto dto)
    {
        // Validar evento
        var evento = await _eventoRepository.GetByIdAsync(dto.IdEvento);
        if (evento == null)
        {
            throw new NotFoundException($"Evento com ID {dto.IdEvento} não encontrado");
        }

        // Validar que evento não está finalizado
        if (evento.Status == "FINALIZADO" || evento.Status == "CANCELADO")
        {
            throw new ValidationException(
                "Não é possível criar etapa em evento finalizado ou cancelado");
        }

        // Validar número único
        if (await _etapaRepository.NumeroExistsNoEventoAsync(dto.NumeroEtapa, dto.IdEvento))
        {
            throw new ValidationException(
                $"Já existe uma etapa número {dto.NumeroEtapa} neste evento");
        }

        // Validar data da etapa dentro do período do evento
        if (dto.DataHora.Date < evento.DataInicio.Date ||
            dto.DataHora.Date > evento.DataFim.Date)
        {
            throw new ValidationException(
                $"Data da etapa deve estar entre {evento.DataInicio:dd/MM/yyyy} e {evento.DataFim:dd/MM/yyyy}");
        }

        // ========================================
        // VALIDAÇÕES ESPECÍFICAS POR MODALIDADE
        // ========================================
        var tipoCronometragem = evento.Modalidade.TipoCronometragem;

        if (tipoCronometragem == "ENDURO")
        {
            ValidarConfiguracoesEnduro(dto);
        }
        else if (tipoCronometragem == "CIRCUITO")
        {
            ValidarConfiguracoesCircuito(dto);
        }

        var etapa = _mapper.Map<Etapa>(dto);
        etapa.Status = "AGENDADA";
        etapa.DataCriacao = DateTime.UtcNow;
        etapa.DataAtualizacao = DateTime.UtcNow;

        await _etapaRepository.AddAsync(etapa);

        _logger.LogInformation(
            "Etapa criada: {Nome} (Etapa {Numero}) no evento {Evento}",
            etapa.Nome, etapa.NumeroEtapa, evento.Nome);

        // Recarregar com includes
        etapa = await _etapaRepository.GetByIdAsync(etapa.Id);

        return MapearParaDto(etapa!);
    }

    public async Task<EtapaDto> UpdateAsync(int id, EtapaUpdateDto dto)
    {
        var etapa = await _etapaRepository.GetByIdAsync(id);

        if (etapa == null)
        {
            throw new NotFoundException($"Etapa com ID {id} não encontrada");
        }

        // Validar status
        if (etapa.Status == "FINALIZADA")
        {
            throw new ValidationException("Não é possível alterar uma etapa finalizada");
        }

        if (etapa.Status == "CANCELADA")
        {
            throw new ValidationException("Não é possível alterar uma etapa cancelada");
        }

        // Atualizar campos
        if (!string.IsNullOrEmpty(dto.Nome))
            etapa.Nome = dto.Nome;

        if (dto.DataHora.HasValue)
        {
            // Validar data dentro do período
            if (dto.DataHora.Value.Date < etapa.Evento.DataInicio.Date ||
                dto.DataHora.Value.Date > etapa.Evento.DataFim.Date)
            {
                throw new ValidationException("Data da etapa fora do período do evento");
            }
            etapa.DataHora = dto.DataHora.Value;
        }

        var tipoCronometragem = etapa.Evento.Modalidade.TipoCronometragem;

        // Atualizar configurações conforme modalidade
        if (tipoCronometragem == "ENDURO")
        {
            if (dto.NumeroEspeciais.HasValue)
                etapa.NumeroEspeciais = dto.NumeroEspeciais.Value;

            if (dto.NumeroVoltas.HasValue)
                etapa.NumeroVoltas = dto.NumeroVoltas.Value;

            if (dto.PrimeiraVoltaValida.HasValue)
                etapa.PrimeiraVoltaValida = dto.PrimeiraVoltaValida.Value;

            if (dto.PenalidadePorFaltaSegundos.HasValue)
                etapa.PenalidadePorFaltaSegundos = dto.PenalidadePorFaltaSegundos.Value;
        }
        else if (tipoCronometragem == "CIRCUITO")
        {
            if (dto.DuracaoCorridaMinutos.HasValue)
                etapa.TempoProvaMinutos = dto.DuracaoCorridaMinutos.Value;

            if (dto.VoltasAposTempoMinimo.HasValue)
                etapa.VoltasAposTempoMinimo = dto.VoltasAposTempoMinimo.Value;
        }

        if (!string.IsNullOrEmpty(dto.Status))
        {
            await AlterarStatusInterno(etapa, dto.Status);
        }

        if (dto.Observacoes != null)
            etapa.Observacoes = dto.Observacoes;

        await _etapaRepository.UpdateAsync(etapa);

        _logger.LogInformation("Etapa atualizada: ID {Id}", id);

        var result = MapearParaDto(etapa);
        await PreencherContagens(result);

        return result;
    }

    public async Task<EtapaDto> AlterarStatusAsync(int id, string novoStatus)
    {
        var etapa = await _etapaRepository.GetByIdAsync(id);

        if (etapa == null)
        {
            throw new NotFoundException($"Etapa com ID {id} não encontrada");
        }

        await AlterarStatusInterno(etapa, novoStatus);
        await _etapaRepository.UpdateAsync(etapa);

        _logger.LogInformation("Status da etapa alterado: ID {Id}, Status: {Status}", id, novoStatus);

        var result = MapearParaDto(etapa);
        await PreencherContagens(result);

        return result;
    }

    public async Task DeleteAsync(int id)
    {
        var etapa = await _etapaRepository.GetByIdAsync(id);

        if (etapa == null)
        {
            throw new NotFoundException($"Etapa com ID {id} não encontrada");
        }

        // Verificar se tem leituras
        var totalLeituras = await _etapaRepository.CountLeiturasAsync(id);
        if (totalLeituras > 0)
        {
            throw new ValidationException(
                $"Não é possível excluir. Existem {totalLeituras} leitura(s) registrada(s).");
        }

        await _etapaRepository.DeleteAsync(id);

        _logger.LogInformation("Etapa deletada: ID {Id}", id);
    }

    // ========================================
    // MÉTODOS PRIVADOS - REGRAS DE NEGÓCIO
    // ========================================

    private void ValidarConfiguracoesEnduro(EtapaCreateDto dto)
    {
        // ENDURO precisa de especiais
        if (dto.NumeroEspeciais < 1)
        {
            throw new ValidationException(
                "Enduro deve ter pelo menos 1 especial");
        }

        // ENDURO precisa de voltas
        if (dto.NumeroVoltas < 1)
        {
            throw new ValidationException(
                "Enduro deve ter pelo menos 1 volta");
        }

        // Validar penalidade razoável
        if (dto.PenalidadePorFaltaSegundos < 60)
        {
            throw new ValidationException(
                "Penalidade mínima deve ser 60 segundos (1 minuto)");
        }

        _logger.LogInformation(
            "Configuração ENDURO: {Especiais} especiais, {Voltas} voltas, " +
            "Volta reconhecimento: {VoltaReconhecimento}, Penalidade: {Penalidade}s",
            dto.NumeroEspeciais, dto.NumeroVoltas,
            !dto.PrimeiraVoltaValida, dto.PenalidadePorFaltaSegundos);
    }

    private void ValidarConfiguracoesCircuito(EtapaCreateDto dto)
    {
        // CIRCUITO precisa de duração
        if (!dto.DuracaoCorridaMinutos.HasValue || dto.DuracaoCorridaMinutos < 5)
        {
            throw new ValidationException(
                "Circuito fechado deve ter duração mínima de 5 minutos");
        }

        // Validar voltas após tempo
        if (dto.VoltasAposTempoMinimo < 1)
        {
            throw new ValidationException(
                "Deve ter pelo menos 1 volta após o tempo mínimo");
        }

        _logger.LogInformation(
            "Configuração CIRCUITO: {Duracao} minutos + {Voltas} voltas",
            dto.DuracaoCorridaMinutos, dto.VoltasAposTempoMinimo);
    }

    private async Task AlterarStatusInterno(Etapa etapa, string novoStatus)
    {
        novoStatus = novoStatus.ToUpper();

        if (!StatusValidos.Contains(novoStatus))
        {
            throw new ValidationException(
                $"Status inválido. Valores permitidos: {string.Join(", ", StatusValidos)}");
        }

        var transicoesValidas = new Dictionary<string, string[]>
        {
            ["AGENDADA"] = new[] { "EM_ANDAMENTO", "CANCELADA" },
            ["EM_ANDAMENTO"] = new[] { "FINALIZADA", "CANCELADA" },
            ["FINALIZADA"] = Array.Empty<string>(),
            ["CANCELADA"] = Array.Empty<string>()
        };

        if (!transicoesValidas[etapa.Status].Contains(novoStatus))
        {
            throw new ValidationException(
                $"Transição de '{etapa.Status}' para '{novoStatus}' não é permitida");
        }

        // Validações adicionais
        if (novoStatus == "EM_ANDAMENTO")
        {
            // Verificar se tem coletores configurados
            var totalColetores = await _etapaRepository.CountColetoresAsync(etapa.Id);
            if (totalColetores == 0)
            {
                throw new ValidationException(
                    "Não é possível iniciar. Nenhum coletor configurado para esta etapa.");
            }
        }

        etapa.Status = novoStatus;
    }

    private EtapaDto MapearParaDto(Etapa etapa)
    {
        return new EtapaDto
        {
            Id = etapa.Id,
            IdEvento = etapa.IdEvento,
            NomeEvento = etapa.Evento.Nome,
            TipoCronometragem = etapa.Evento.Modalidade.TipoCronometragem,
            NumeroEtapa = etapa.NumeroEtapa,
            Nome = etapa.Nome,
            DataHora = etapa.DataHora,
            NumeroEspeciais = etapa.NumeroEspeciais ?? 0,
            NumeroVoltas = etapa.NumeroVoltas ?? 0,
            PrimeiraVoltaValida = etapa.PrimeiraVoltaValida,
            PenalidadePorFaltaSegundos = etapa.PenalidadePorFaltaSegundos ?? 0,
            PenalidadeFormatada = FormatarTempo(etapa.PenalidadePorFaltaSegundos ?? 0),
            DuracaoCorridaMinutos = etapa.TempoProvaMinutos,
            VoltasAposTempoMinimo = etapa.VoltasAposTempoMinimo,
            Status = etapa.Status,
            Observacoes = etapa.Observacoes,
            DataCriacao = etapa.DataCriacao
        };
    }

    private string FormatarTempo(int segundos)
    {
        var ts = TimeSpan.FromSeconds(segundos);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    private async Task PreencherContagens(EtapaDto dto)
    {
        dto.TotalLeituras = await _etapaRepository.CountLeiturasAsync(dto.Id);
        dto.TotalColetores = await _etapaRepository.CountColetoresAsync(dto.Id);
    }
}
