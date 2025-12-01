using AutoMapper;
using DevNationCrono.API.Exceptions;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Models.Pagination;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Interfaces;

namespace DevNationCrono.API.Services.Implementations;

public class EventoService : IEventoService
{
    private readonly IEventoRepository _eventoRepository;
    private readonly IModalidadeRepository _modalidadeRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<EventoService> _logger;

    // Status válidos
    private static readonly string[] StatusValidos =
    {
            "AGENDADO",
            "INSCRICOES_ABERTAS",
            "INSCRICOES_FECHADAS",
            "EM_ANDAMENTO",
            "FINALIZADO",
            "CANCELADO"
        };

    public EventoService(
        IEventoRepository eventoRepository,
        IModalidadeRepository modalidadeRepository,
        IMapper mapper,
        ILogger<EventoService> logger)
    {
        _eventoRepository = eventoRepository;
        _modalidadeRepository = modalidadeRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<EventoDto?> GetByIdAsync(int id)
    {
        var evento = await _eventoRepository.GetByIdWithDetailsAsync(id);

        if (evento == null)
            return null;

        var dto = _mapper.Map<EventoDto>(evento);
        await PreencherContagens(dto);

        return dto;
    }

    public async Task<List<EventoResumoDto>> GetAllAsync()
    {
        var eventos = await _eventoRepository.GetAllAsync();
        var dtos = _mapper.Map<List<EventoResumoDto>>(eventos);

        foreach (var dto in dtos)
        {
            dto.TotalInscritos = await _eventoRepository.CountInscritosAsync(dto.Id);
        }

        return dtos;
    }

    public async Task<List<EventoResumoDto>> GetActivesAsync()
    {
        var eventos = await _eventoRepository.GetActivesAsync();
        var dtos = _mapper.Map<List<EventoResumoDto>>(eventos);

        foreach (var dto in dtos)
        {
            dto.TotalInscritos = await _eventoRepository.CountInscritosAsync(dto.Id);
        }

        return dtos;
    }

    public async Task<PagedResult<EventoResumoDto>> GetPagedAsync(EventoFilterParams filter)
    {
        var result = await _eventoRepository.GetPagedAsync(filter);
        var dtos = _mapper.Map<List<EventoResumoDto>>(result.Items);

        foreach (var dto in dtos)
        {
            dto.TotalInscritos = await _eventoRepository.CountInscritosAsync(dto.Id);
        }

        return new PagedResult<EventoResumoDto>(
            dtos,
            result.TotalCount,
            result.PageNumber,
            result.PageSize
        );
    }

    public async Task<List<EventoResumoDto>> GetProximosAsync(int quantidade = 5)
    {
        var eventos = await _eventoRepository.GetProximosAsync(quantidade);
        var dtos = _mapper.Map<List<EventoResumoDto>>(eventos);

        foreach (var dto in dtos)
        {
            dto.TotalInscritos = await _eventoRepository.CountInscritosAsync(dto.Id);
        }

        return dtos;
    }

    public async Task<EventoDto> CreateAsync(EventoCreateDto dto)
    {
        // Validar modalidade existe
        var modalidade = await _modalidadeRepository.GetByIdAsync(dto.IdModalidade);
        if (modalidade == null)
        {
            throw new ValidationException($"Modalidade com ID {dto.IdModalidade} não encontrada");
        }

        if (!modalidade.Ativo)
        {
            throw new ValidationException("Modalidade está inativa");
        }

        // Validar datas
        if (dto.DataFim < dto.DataInicio)
        {
            throw new ValidationException("Data de fim não pode ser anterior à data de início");
        }

        if (dto.DataInicio < DateTime.UtcNow.Date)
        {
            throw new ValidationException("Data de início não pode ser no passado");
        }

        // Validar datas de inscrição
        if (dto.DataAberturaInscricoes.HasValue && dto.DataFechamentoInscricoes.HasValue)
        {
            if (dto.DataFechamentoInscricoes < dto.DataAberturaInscricoes)
            {
                throw new ValidationException(
                    "Data de fechamento de inscrições não pode ser anterior à abertura");
            }

            if (dto.DataFechamentoInscricoes > dto.DataInicio)
            {
                throw new ValidationException(
                    "Inscrições devem fechar antes do início do evento");
            }
        }

        var evento = _mapper.Map<Evento>(dto);
        evento.Status = "AGENDADO";
        evento.InscricoesAbertas = false;
        evento.Ativo = true;
        evento.DataCriacao = DateTime.UtcNow;
        evento.DataAtualizacao = DateTime.UtcNow;

        await _eventoRepository.AddAsync(evento);

        _logger.LogInformation("Evento criado: {Nome} (ID: {Id})", evento.Nome, evento.Id);

        // Recarregar com includes
        evento = await _eventoRepository.GetByIdAsync(evento.Id);
        var result = _mapper.Map<EventoDto>(evento);

        return result;
    }

    public async Task<EventoDto> UpdateAsync(int id, EventoUpdateDto dto)
    {
        var evento = await _eventoRepository.GetByIdAsync(id);

        if (evento == null)
        {
            throw new NotFoundException($"Evento com ID {id} não encontrado");
        }

        // Validar se pode alterar
        if (evento.Status == "FINALIZADO")
        {
            throw new ValidationException("Não é possível alterar um evento finalizado");
        }

        if (evento.Status == "CANCELADO")
        {
            throw new ValidationException("Não é possível alterar um evento cancelado");
        }

        // Atualizar campos não nulos
        if (!string.IsNullOrEmpty(dto.Nome))
            evento.Nome = dto.Nome;

        if (dto.Descricao != null)
            evento.Descricao = dto.Descricao;

        if (!string.IsNullOrEmpty(dto.Local))
            evento.Local = dto.Local;

        if (!string.IsNullOrEmpty(dto.Cidade))
            evento.Cidade = dto.Cidade;

        if (!string.IsNullOrEmpty(dto.Uf))
            evento.Uf = dto.Uf.ToUpper();

        if (dto.DataInicio.HasValue)
        {
            if (dto.DataInicio.Value < DateTime.UtcNow.Date &&
                evento.Status == "AGENDADO")
            {
                throw new ValidationException("Data de início não pode ser no passado");
            }
            evento.DataInicio = dto.DataInicio.Value;
        }

        if (dto.DataFim.HasValue)
        {
            if (dto.DataFim.Value < evento.DataInicio)
            {
                throw new ValidationException("Data de fim não pode ser anterior à data de início");
            }
            evento.DataFim = dto.DataFim.Value;
        }

        if (dto.DataAberturaInscricoes.HasValue)
            evento.DataAberturaInscricoes = dto.DataAberturaInscricoes;

        if (dto.DataFechamentoInscricoes.HasValue)
            evento.DataFechamentoInscricoes = dto.DataFechamentoInscricoes;

        if (!string.IsNullOrEmpty(dto.Regulamento))
            evento.Regulamento = dto.Regulamento;

        if (dto.ImagemBanner != null)
            evento.ImagemBanner = dto.ImagemBanner;

        if (dto.Ativo.HasValue)
            evento.Ativo = dto.Ativo.Value;

        await _eventoRepository.UpdateAsync(evento);

        _logger.LogInformation("Evento atualizado: ID {Id}", id);

        var result = _mapper.Map<EventoDto>(evento);
        await PreencherContagens(result);

        return result;
    }

    public async Task<EventoDto> AbrirInscricoesAsync(int id)
    {
        var evento = await _eventoRepository.GetByIdAsync(id);

        if (evento == null)
        {
            throw new NotFoundException($"Evento com ID {id} não encontrado");
        }

        if (evento.Status != "AGENDADO" && evento.Status != "INSCRICOES_FECHADAS")
        {
            throw new ValidationException(
                $"Não é possível abrir inscrições. Status atual: {evento.Status}");
        }

        // Verificar se tem categorias
        var totalCategorias = await _eventoRepository.CountCategoriasAsync(id);
        if (totalCategorias == 0)
        {
            throw new ValidationException(
                "Não é possível abrir inscrições sem categorias cadastradas");
        }

        // Verificar se tem etapas
        var totalEtapas = await _eventoRepository.CountEtapasAsync(id);
        if (totalEtapas == 0)
        {
            throw new ValidationException(
                "Não é possível abrir inscrições sem etapas cadastradas");
        }

        evento.InscricoesAbertas = true;
        evento.Status = "INSCRICOES_ABERTAS";
        evento.DataAberturaInscricoes = DateTime.UtcNow;

        await _eventoRepository.UpdateAsync(evento);

        _logger.LogInformation("Inscrições abertas para evento: ID {Id}", id);

        var result = _mapper.Map<EventoDto>(evento);
        await PreencherContagens(result);

        return result;
    }

    public async Task<EventoDto> FecharInscricoesAsync(int id)
    {
        var evento = await _eventoRepository.GetByIdAsync(id);

        if (evento == null)
        {
            throw new NotFoundException($"Evento com ID {id} não encontrado");
        }

        if (!evento.InscricoesAbertas)
        {
            throw new ValidationException("Inscrições já estão fechadas");
        }

        evento.InscricoesAbertas = false;
        evento.Status = "INSCRICOES_FECHADAS";
        evento.DataFechamentoInscricoes = DateTime.UtcNow;

        await _eventoRepository.UpdateAsync(evento);

        _logger.LogInformation("Inscrições fechadas para evento: ID {Id}", id);

        var result = _mapper.Map<EventoDto>(evento);
        await PreencherContagens(result);

        return result;
    }

    public async Task<EventoDto> AlterarStatusAsync(int id, string novoStatus)
    {
        var evento = await _eventoRepository.GetByIdAsync(id);

        if (evento == null)
        {
            throw new NotFoundException($"Evento com ID {id} não encontrado");
        }

        novoStatus = novoStatus.ToUpper();

        if (!StatusValidos.Contains(novoStatus))
        {
            throw new ValidationException(
                $"Status inválido. Valores permitidos: {string.Join(", ", StatusValidos)}");
        }

        // Validar transições permitidas
        var transicaoValida = ValidarTransicaoStatus(evento.Status, novoStatus);
        if (!transicaoValida)
        {
            throw new ValidationException(
                $"Transição de '{evento.Status}' para '{novoStatus}' não é permitida");
        }

        evento.Status = novoStatus;

        // Ações automáticas baseadas no status
        if (novoStatus == "EM_ANDAMENTO")
        {
            evento.InscricoesAbertas = false;
        }
        else if (novoStatus == "CANCELADO")
        {
            evento.InscricoesAbertas = false;
            evento.Ativo = false;
        }

        await _eventoRepository.UpdateAsync(evento);

        _logger.LogInformation("Status do evento alterado: ID {Id}, Status: {Status}", id, novoStatus);

        var result = _mapper.Map<EventoDto>(evento);
        await PreencherContagens(result);

        return result;
    }

    public async Task DeleteAsync(int id)
    {
        var evento = await _eventoRepository.GetByIdAsync(id);

        if (evento == null)
        {
            throw new NotFoundException($"Evento com ID {id} não encontrado");
        }

        // Verificar se tem inscrições pagas
        var totalInscritos = await _eventoRepository.CountInscritosAsync(id);
        if (totalInscritos > 0)
        {
            throw new ValidationException(
                $"Não é possível excluir. Existem {totalInscritos} inscrição(ões) no evento.");
        }

        await _eventoRepository.DeleteAsync(id);

        _logger.LogInformation("Evento deletado (soft): ID {Id}", id);
    }

    private async Task PreencherContagens(EventoDto dto)
    {
        dto.TotalEtapas = await _eventoRepository.CountEtapasAsync(dto.Id);
        dto.TotalCategorias = await _eventoRepository.CountCategoriasAsync(dto.Id);
        dto.TotalInscritos = await _eventoRepository.CountInscritosAsync(dto.Id);
    }

    private bool ValidarTransicaoStatus(string statusAtual, string novoStatus)
    {
        // Define transições válidas
        var transicoesValidas = new Dictionary<string, string[]>
        {
            ["AGENDADO"] = new[] { "INSCRICOES_ABERTAS", "CANCELADO" },
            ["INSCRICOES_ABERTAS"] = new[] { "INSCRICOES_FECHADAS", "EM_ANDAMENTO", "CANCELADO" },
            ["INSCRICOES_FECHADAS"] = new[] { "INSCRICOES_ABERTAS", "EM_ANDAMENTO", "CANCELADO" },
            ["EM_ANDAMENTO"] = new[] { "FINALIZADO", "CANCELADO" },
            ["FINALIZADO"] = Array.Empty<string>(), // Não pode mudar
            ["CANCELADO"] = Array.Empty<string>()   // Não pode mudar
        };

        if (!transicoesValidas.ContainsKey(statusAtual))
            return false;

        return transicoesValidas[statusAtual].Contains(novoStatus);
    }
}