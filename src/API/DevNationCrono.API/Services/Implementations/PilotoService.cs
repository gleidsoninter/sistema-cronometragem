using AutoMapper;
using DevNationCrono.API.Exceptions;
using DevNationCrono.API.Helpers;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Models.Pagination;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Interfaces;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace DevNationCrono.API.Services.Implementations;

public class PilotoService : IPilotoService
{
    private readonly IPilotoRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<PilotoService> _logger;

    public PilotoService(
        IPilotoRepository repository,
        IMapper mapper,
        ILogger<PilotoService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PilotoResponseDto?> GetByIdAsync(int id)
    {
        var piloto = await _repository.GetByIdAsync(id);

        if (piloto == null)
            return null;

        return _mapper.Map<PilotoResponseDto>(piloto);
    }

    public async Task<List<PilotoResponseDto>> GetAllAsync()
    {
        var pilotos = await _repository.GetAllAsync();
        return _mapper.Map<List<PilotoResponseDto>>(pilotos);
    }

    public async Task<List<PilotoResponseDto>> GetActivesAsync()
    {
        var pilotos = await _repository.GetActivesAsync();
        return _mapper.Map<List<PilotoResponseDto>>(pilotos);
    }

    public async Task<PilotoResponseDto> CadastrarAsync(PilotoCadastroDto dto)
    {
        // Validar CPF
        var cpfLimpo = CpfValidator.RemoverFormatacao(dto.Cpf);

        if (!CpfValidator.Validar(cpfLimpo))
        {
            throw new ValidationException("CPF inválido");
        }

        // Verificar se CPF já existe
        if (await _repository.CpfExistsAsync(cpfLimpo))
        {
            throw new ValidationException("CPF já cadastrado");
        }

        // Verificar se email já existe
        if (await _repository.EmailExistsAsync(dto.Email.ToLower()))
        {
            throw new ValidationException("Email já cadastrado");
        }

        // Validar idade mínima (16 anos)
        var idade = DateTime.Now.Year - dto.DataNascimento.Year;
        if (DateTime.Now.DayOfYear < dto.DataNascimento.DayOfYear)
            idade--;

        if (idade < 16)
        {
            throw new ValidationException("Piloto deve ter no mínimo 16 anos");
        }

        // Mapear para entidade
        var piloto = _mapper.Map<Piloto>(dto);
        piloto.Cpf = cpfLimpo;
        piloto.Email = dto.Email.ToLower().Trim();

        // Hash da senha
        var salt = BCrypt.Net.BCrypt.GenerateSalt(12);
        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Senha, salt);
        piloto.PasswordHash = hash;
        piloto.PasswordSalt = salt;

        // Salvar
        await _repository.AddAsync(piloto);

        _logger.LogInformation("Piloto cadastrado: {Email}", piloto.Email);

        return _mapper.Map<PilotoResponseDto>(piloto);
    }

    public async Task<PilotoResponseDto> AtualizarAsync(int id, PilotoAtualizacaoDto dto)
    {
        var piloto = await _repository.GetByIdAsync(id);

        if (piloto == null)
        {
            throw new NotFoundException($"Piloto com ID {id} não encontrado");
        }

        // Mapear apenas campos não nulos
        _mapper.Map(dto, piloto);
        piloto.DataAtualizacao = DateTime.UtcNow;

        await _repository.UpdateAsync(piloto);

        _logger.LogInformation("Piloto atualizado: ID {Id}", id);

        return _mapper.Map<PilotoResponseDto>(piloto);
    }

    public async Task DeletarAsync(int id)
    {
        var piloto = await _repository.GetByIdAsync(id);

        if (piloto == null)
        {
            throw new NotFoundException($"Piloto com ID {id} não encontrado");
        }

        // Soft delete
        await _repository.DeleteAsync(id);

        _logger.LogInformation("Piloto deletado (soft): ID {Id}", id);
    }

    public async Task<PilotoResponseDto?> GetByCpfAsync(string cpf)
    {
        var cpfLimpo = CpfValidator.RemoverFormatacao(cpf);
        var piloto = await _repository.GetByCpfAsync(cpfLimpo);

        if (piloto == null)
            return null;

        return _mapper.Map<PilotoResponseDto>(piloto);
    }

    public async Task<PagedResult<PilotoResponseDto>> GetPagedAsync(PilotoFilterParams filterParams)
    {
        var result = await _repository.GetPagedAsync(filterParams);

        var itemsDto = _mapper.Map<List<PilotoResponseDto>>(result.Items);

        return new PagedResult<PilotoResponseDto>(
            itemsDto,
            result.TotalCount,
            result.PageNumber,
            result.PageSize
        );
    }
}