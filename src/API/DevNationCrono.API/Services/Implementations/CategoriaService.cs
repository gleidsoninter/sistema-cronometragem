using AutoMapper;
using DevNationCrono.API.Exceptions;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Interfaces;

namespace DevNationCrono.API.Services.Implementations;

public class CategoriaService : ICategoriaService
{
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IEventoRepository _eventoRepository;
    private readonly IModalidadeRepository _modalidadeRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoriaService> _logger;

    public CategoriaService(
        ICategoriaRepository categoriaRepository,
        IEventoRepository eventoRepository,
        IModalidadeRepository modalidadeRepository,
        IMapper mapper,
        ILogger<CategoriaService> logger)
    {
        _categoriaRepository = categoriaRepository;
        _eventoRepository = eventoRepository;
        _modalidadeRepository = modalidadeRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CategoriaDto?> GetByIdAsync(int id)
    {
        var categoria = await _categoriaRepository.GetByIdAsync(id);

        if (categoria == null)
            return null;

        var dto = _mapper.Map<CategoriaDto>(categoria);
        await PreencherContagens(dto);

        return dto;
    }

    public async Task<List<CategoriaDto>> GetByEventoAsync(int idEvento)
    {
        var categorias = await _categoriaRepository.GetByEventoAsync(idEvento);
        var dtos = _mapper.Map<List<CategoriaDto>>(categorias);

        foreach (var dto in dtos)
        {
            await PreencherContagens(dto);
        }

        return dtos;
    }

    public async Task<List<CategoriaResumoDto>> GetActivesByEventoAsync(int idEvento)
    {
        var categorias = await _categoriaRepository.GetActivesByEventoAsync(idEvento);
        var dtos = _mapper.Map<List<CategoriaResumoDto>>(categorias);

        foreach (var dto in dtos)
        {
            dto.TotalInscritos = await _categoriaRepository.CountInscritosAsync(dto.Id);

            var categoria = categorias.First(c => c.Id == dto.Id);
            if (categoria.VagasLimitadas && categoria.NumeroVagas.HasValue)
            {
                dto.VagasDisponiveis = categoria.NumeroVagas.Value - dto.TotalInscritos;
            }
        }

        return dtos;
    }

    public async Task<CategoriaDto> CreateAsync(CategoriaCreateDto dto)
    {
        // Validar evento existe
        var evento = await _eventoRepository.GetByIdAsync(dto.IdEvento);
        if (evento == null)
        {
            throw new NotFoundException($"Evento com ID {dto.IdEvento} não encontrado");
        }

        // Validar modalidade
        var modalidade = await _modalidadeRepository.GetByIdAsync(dto.IdModalidade);
        if (modalidade == null)
        {
            throw new NotFoundException($"Modalidade com ID {dto.IdModalidade} não encontrada");
        }

        // Validar que modalidade é a mesma do evento
        if (evento.IdModalidade != dto.IdModalidade)
        {
            throw new ValidationException(
                "Modalidade da categoria deve ser a mesma do evento");
        }

        // Validar nome único no evento
        if (await _categoriaRepository.NomeExistsNoEventoAsync(dto.Nome, dto.IdEvento))
        {
            throw new ValidationException(
                $"Já existe uma categoria '{dto.Nome}' neste evento");
        }

        // Validar idade
        if (dto.IdadeMinima.HasValue && dto.IdadeMaxima.HasValue)
        {
            if (dto.IdadeMinima > dto.IdadeMaxima)
            {
                throw new ValidationException("Idade mínima não pode ser maior que a máxima");
            }
        }

        // Validar cilindrada
        if (dto.CilindradaMinima.HasValue && dto.CilindradaMaxima.HasValue)
        {
            if (dto.CilindradaMinima > dto.CilindradaMaxima)
            {
                throw new ValidationException(
                    "Cilindrada mínima não pode ser maior que a máxima");
            }
        }

        // Validar vagas
        if (dto.VagasLimitadas && !dto.NumeroVagas.HasValue)
        {
            throw new ValidationException(
                "Número de vagas é obrigatório quando vagas são limitadas");
        }

        var categoria = _mapper.Map<Categoria>(dto);
        categoria.Ativo = true;
        categoria.DataCriacao = DateTime.UtcNow;

        await _categoriaRepository.AddAsync(categoria);

        _logger.LogInformation(
            "Categoria criada: {Nome} no evento {Evento}",
            categoria.Nome, evento.Nome);

        // Recarregar com includes
        categoria = await _categoriaRepository.GetByIdAsync(categoria.Id);
        var result = _mapper.Map<CategoriaDto>(categoria);

        return result;
    }

    public async Task<CategoriaDto> UpdateAsync(int id, CategoriaUpdateDto dto)
    {
        var categoria = await _categoriaRepository.GetByIdAsync(id);

        if (categoria == null)
        {
            throw new NotFoundException($"Categoria com ID {id} não encontrada");
        }

        // Verificar se evento permite alteração
        if (categoria.Evento.Status == "FINALIZADO" ||
            categoria.Evento.Status == "CANCELADO")
        {
            throw new ValidationException(
                "Não é possível alterar categoria de evento finalizado ou cancelado");
        }

        // Validar nome único
        if (!string.IsNullOrEmpty(dto.Nome) &&
            await _categoriaRepository.NomeExistsNoEventoAsync(dto.Nome, categoria.IdEvento, id))
        {
            throw new ValidationException(
                $"Já existe uma categoria '{dto.Nome}' neste evento");
        }

        // Atualizar campos
        if (!string.IsNullOrEmpty(dto.Nome))
            categoria.Nome = dto.Nome;

        if (dto.Descricao != null)
            categoria.Descricao = dto.Descricao;

        if (dto.ValorInscricao.HasValue)
            categoria.ValorInscricao = dto.ValorInscricao.Value;

        if (dto.DescontoSegundaCategoria.HasValue)
            categoria.DescontoSegundaCategoria = dto.DescontoSegundaCategoria.Value;

        if (dto.IdadeMinima.HasValue)
            categoria.IdadeMinima = dto.IdadeMinima;

        if (dto.IdadeMaxima.HasValue)
            categoria.IdadeMaxima = dto.IdadeMaxima;

        if (dto.CilindradaMinima.HasValue)
            categoria.CilindradaMinima = dto.CilindradaMinima;

        if (dto.CilindradaMaxima.HasValue)
            categoria.CilindradaMaxima = dto.CilindradaMaxima;

        if (dto.VagasLimitadas.HasValue)
            categoria.VagasLimitadas = dto.VagasLimitadas.Value;

        if (dto.NumeroVagas.HasValue)
            categoria.NumeroVagas = dto.NumeroVagas;

        if (dto.Ordem.HasValue)
            categoria.Ordem = dto.Ordem.Value;

        if (dto.Ativo.HasValue)
            categoria.Ativo = dto.Ativo.Value;

        await _categoriaRepository.UpdateAsync(categoria);

        _logger.LogInformation("Categoria atualizada: ID {Id}", id);

        var result = _mapper.Map<CategoriaDto>(categoria);
        await PreencherContagens(result);

        return result;
    }

    public async Task DeleteAsync(int id)
    {
        var categoria = await _categoriaRepository.GetByIdAsync(id);

        if (categoria == null)
        {
            throw new NotFoundException($"Categoria com ID {id} não encontrada");
        }

        // Verificar se tem inscritos
        var totalInscritos = await _categoriaRepository.CountInscritosAsync(id);
        if (totalInscritos > 0)
        {
            throw new ValidationException(
                $"Não é possível excluir. Existem {totalInscritos} inscrição(ões) nesta categoria.");
        }

        await _categoriaRepository.DeleteAsync(id);

        _logger.LogInformation("Categoria deletada (soft): ID {Id}", id);
    }

    private async Task PreencherContagens(CategoriaDto dto)
    {
        dto.TotalInscritos = await _categoriaRepository.CountInscritosAsync(dto.Id);

        if (dto.VagasLimitadas && dto.NumeroVagas.HasValue)
        {
            dto.VagasDisponiveis = dto.NumeroVagas.Value - dto.TotalInscritos;
        }
    }
}
