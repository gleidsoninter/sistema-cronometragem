using AutoMapper;
using DevNationCrono.API.Exceptions;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Interfaces;

namespace DevNationCrono.API.Services.Implementations;

public class ModalidadeService : IModalidadeService
{
    private readonly IModalidadeRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<ModalidadeService> _logger;

    public ModalidadeService(
        IModalidadeRepository repository,
        IMapper mapper,
        ILogger<ModalidadeService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ModalidadeDto?> GetByIdAsync(int id)
    {
        var modalidade = await _repository.GetByIdAsync(id);

        if (modalidade == null)
            return null;

        var dto = _mapper.Map<ModalidadeDto>(modalidade);
        dto.TotalEventos = await _repository.CountEventosAsync(id);

        return dto;
    }

    public async Task<List<ModalidadeDto>> GetAllAsync()
    {
        var modalidades = await _repository.GetAllAsync();
        var dtos = _mapper.Map<List<ModalidadeDto>>(modalidades);

        // Adicionar contagem de eventos
        foreach (var dto in dtos)
        {
            dto.TotalEventos = await _repository.CountEventosAsync(dto.Id);
        }

        return dtos;
    }

    public async Task<List<ModalidadeResumoDto>> GetActivesAsync()
    {
        var modalidades = await _repository.GetActivesAsync();
        return _mapper.Map<List<ModalidadeResumoDto>>(modalidades);
    }

    public async Task<List<ModalidadeResumoDto>> GetByTipoAsync(string tipoCronometragem)
    {
        // Validar tipo
        if (tipoCronometragem != "ENDURO" && tipoCronometragem != "CIRCUITO")
        {
            throw new ValidationException("Tipo de cronometragem deve ser ENDURO ou CIRCUITO");
        }

        var modalidades = await _repository.GetByTipoAsync(tipoCronometragem);
        return _mapper.Map<List<ModalidadeResumoDto>>(modalidades);
    }

    public async Task<ModalidadeDto> CreateAsync(ModalidadeCreateDto dto)
    {
        // Validar nome único
        if (await _repository.NomeExistsAsync(dto.Nome))
        {
            throw new ValidationException($"Já existe uma modalidade com o nome '{dto.Nome}'");
        }

        // Validar tipo
        if (dto.TipoCronometragem != "ENDURO" && dto.TipoCronometragem != "CIRCUITO")
        {
            throw new ValidationException("Tipo de cronometragem deve ser ENDURO ou CIRCUITO");
        }

        var modalidade = _mapper.Map<Modalidade>(dto);
        modalidade.DataCriacao = DateTime.UtcNow;
        modalidade.Ativo = true;

        await _repository.AddAsync(modalidade);

        _logger.LogInformation("Modalidade criada: {Nome} ({Tipo})",
            modalidade.Nome, modalidade.TipoCronometragem);

        return _mapper.Map<ModalidadeDto>(modalidade);
    }

    public async Task<ModalidadeDto> UpdateAsync(int id, ModalidadeUpdateDto dto)
    {
        var modalidade = await _repository.GetByIdAsync(id);

        if (modalidade == null)
        {
            throw new NotFoundException($"Modalidade com ID {id} não encontrada");
        }

        // Validar nome único (se estiver alterando)
        if (!string.IsNullOrEmpty(dto.Nome) &&
            await _repository.NomeExistsAsync(dto.Nome, id))
        {
            throw new ValidationException($"Já existe uma modalidade com o nome '{dto.Nome}'");
        }

        // Atualizar campos
        if (!string.IsNullOrEmpty(dto.Nome))
            modalidade.Nome = dto.Nome;

        if (dto.Descricao != null)
            modalidade.Descricao = dto.Descricao;

        if (dto.Ativo.HasValue)
            modalidade.Ativo = dto.Ativo.Value;

        await _repository.UpdateAsync(modalidade);

        _logger.LogInformation("Modalidade atualizada: ID {Id}", id);

        var result = _mapper.Map<ModalidadeDto>(modalidade);
        result.TotalEventos = await _repository.CountEventosAsync(id);

        return result;
    }

    public async Task DeleteAsync(int id)
    {
        var modalidade = await _repository.GetByIdAsync(id);

        if (modalidade == null)
        {
            throw new NotFoundException($"Modalidade com ID {id} não encontrada");
        }

        // Verificar se tem eventos vinculados
        var totalEventos = await _repository.CountEventosAsync(id);
        if (totalEventos > 0)
        {
            throw new ValidationException(
                $"Não é possível excluir. Existem {totalEventos} evento(s) vinculado(s) a esta modalidade.");
        }

        await _repository.DeleteAsync(id);

        _logger.LogInformation("Modalidade deletada (soft): ID {Id}", id);
    }
}
