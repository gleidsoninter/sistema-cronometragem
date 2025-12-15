using DevNationCrono.API.Data;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevNationCrono.API.Services;

public class CampeonatoService : ICampeonatoService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CampeonatoService> _logger;

    public CampeonatoService(ApplicationDbContext context, ILogger<CampeonatoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region CRUD Campeonato

    public async Task<List<CampeonatoResumoDto>> GetAllAsync(int? ano = null, int? idModalidade = null)
    {
        var query = _context.Campeonatos
            .Include(c => c.Modalidade)
            .Where(c => c.Ativo);

        if (ano.HasValue)
            query = query.Where(c => c.Ano == ano.Value);

        if (idModalidade.HasValue)
            query = query.Where(c => c.IdModalidade == idModalidade.Value);

        var campeonatos = await query
            .OrderByDescending(c => c.Ano)
            .ThenBy(c => c.Nome)
            .ToListAsync();

        var result = new List<CampeonatoResumoDto>();
        foreach (var c in campeonatos)
        {
            var totalEventos = await _context.Eventos
                .CountAsync(e => e.IdCampeonato == c.Id && e.Ativo);

            var totalInscritos = await _context.Inscricoes
                .Where(i => i.Evento.IdCampeonato == c.Id)
                .Select(i => i.IdPiloto)
                .Distinct()
                .CountAsync();

            result.Add(new CampeonatoResumoDto
            {
                Id = c.Id,
                Nome = c.Nome,
                Sigla = c.Sigla,
                Ano = c.Ano,
                IdModalidade = c.IdModalidade,
                NomeModalidade = c.Modalidade?.Nome ?? "",
                Status = c.Status,
                TotalEventos = totalEventos,
                TotalInscritos = totalInscritos
            });
        }

        return result;
    }

    public async Task<CampeonatoDto?> GetByIdAsync(int id)
    {
        var campeonato = await _context.Campeonatos
            .Include(c => c.Modalidade)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (campeonato == null)
            return null;

        var pontuacoes = await _context.CampeonatoPontuacoes
            .Where(p => p.IdCampeonato == id)
            .OrderBy(p => p.Posicao)
            .Select(p => new CampeonatoPontuacaoDto
            {
                Id = p.Id,
                IdCampeonato = p.IdCampeonato,
                Posicao = p.Posicao,
                Pontos = p.Pontos
            })
            .ToListAsync();

        var totalEventos = await _context.Eventos
            .CountAsync(e => e.IdCampeonato == id && e.Ativo);

        var totalInscritos = await _context.Inscricoes
            .Where(i => i.Evento.IdCampeonato == id)
            .Select(i => i.IdPiloto)
            .Distinct()
            .CountAsync();

        return new CampeonatoDto
        {
            Id = campeonato.Id,
            Nome = campeonato.Nome,
            Sigla = campeonato.Sigla,
            Ano = campeonato.Ano,
            Descricao = campeonato.Descricao,
            Regulamento = campeonato.Regulamento,
            ImagemBanner = campeonato.ImagemBanner,
            IdModalidade = campeonato.IdModalidade,
            NomeModalidade = campeonato.Modalidade?.Nome ?? "",
            TipoCronometragem = campeonato.Modalidade?.TipoCronometragem ?? "",
            QtdeEtapasValidas = campeonato.QtdeEtapasValidas,
            PercentualMinimoVoltasLider = campeonato.PercentualMinimoVoltasLider,
            ExigeBandeirada = campeonato.ExigeBandeirada,
            PercentualMinimoProvaEnduro = campeonato.PercentualMinimoProvaEnduro,
            TodosParticipantesPontuam = campeonato.TodosParticipantesPontuam,
            DesclassificadoNaoPontua = campeonato.DesclassificadoNaoPontua,
            AbandonoNaoPontua = campeonato.AbandonoNaoPontua,
            Status = campeonato.Status,
            Ativo = campeonato.Ativo,
            DataCriacao = campeonato.DataCriacao,
            TotalEventos = totalEventos,
            TotalInscritos = totalInscritos,
            Pontuacoes = pontuacoes
        };
    }

    public async Task<CampeonatoDto> CreateAsync(CampeonatoCreateDto dto)
    {
        // Validar modalidade
        var modalidade = await _context.Modalidades.FindAsync(dto.IdModalidade);
        if (modalidade == null)
            throw new ArgumentException($"Modalidade com ID {dto.IdModalidade} não encontrada");

        var campeonato = new Campeonato
        {
            Nome = dto.Nome,
            Sigla = dto.Sigla,
            Ano = dto.Ano,
            Descricao = dto.Descricao,
            Regulamento = dto.Regulamento,
            ImagemBanner = dto.ImagemBanner,
            IdModalidade = dto.IdModalidade,
            QtdeEtapasValidas = dto.QtdeEtapasValidas,
            PercentualMinimoVoltasLider = dto.PercentualMinimoVoltasLider,
            ExigeBandeirada = dto.ExigeBandeirada,
            PercentualMinimoProvaEnduro = dto.PercentualMinimoProvaEnduro,
            TodosParticipantesPontuam = dto.TodosParticipantesPontuam,
            DesclassificadoNaoPontua = dto.DesclassificadoNaoPontua,
            AbandonoNaoPontua = dto.AbandonoNaoPontua,
            Status = "PLANEJADO",
            Ativo = true,
            DataCriacao = DateTime.UtcNow,
            DataAtualizacao = DateTime.UtcNow
        };

        _context.Campeonatos.Add(campeonato);
        await _context.SaveChangesAsync();

        // Se vieram pontuações, criar
        if (dto.Pontuacoes?.Any() == true)
        {
            await SetPontuacoesAsync(campeonato.Id, dto.Pontuacoes);
        }

        return await GetByIdAsync(campeonato.Id)
            ?? throw new InvalidOperationException("Erro ao criar campeonato");
    }

    public async Task<CampeonatoDto> UpdateAsync(int id, CampeonatoUpdateDto dto)
    {
        var campeonato = await _context.Campeonatos.FindAsync(id);
        if (campeonato == null)
            throw new ArgumentException($"Campeonato com ID {id} não encontrado");

        if (!string.IsNullOrWhiteSpace(dto.Nome))
            campeonato.Nome = dto.Nome;

        if (dto.Sigla != null)
            campeonato.Sigla = dto.Sigla;

        if (dto.Ano.HasValue)
            campeonato.Ano = dto.Ano.Value;

        if (dto.Descricao != null)
            campeonato.Descricao = dto.Descricao;

        if (dto.Regulamento != null)
            campeonato.Regulamento = dto.Regulamento;

        if (dto.ImagemBanner != null)
            campeonato.ImagemBanner = dto.ImagemBanner;

        if (dto.QtdeEtapasValidas.HasValue)
            campeonato.QtdeEtapasValidas = dto.QtdeEtapasValidas;

        // Regras Circuito
        if (dto.PercentualMinimoVoltasLider.HasValue)
            campeonato.PercentualMinimoVoltasLider = dto.PercentualMinimoVoltasLider;

        if (dto.ExigeBandeirada.HasValue)
            campeonato.ExigeBandeirada = dto.ExigeBandeirada.Value;

        // Regras Enduro
        if (dto.PercentualMinimoProvaEnduro.HasValue)
            campeonato.PercentualMinimoProvaEnduro = dto.PercentualMinimoProvaEnduro;

        // Regras Gerais
        if (dto.TodosParticipantesPontuam.HasValue)
            campeonato.TodosParticipantesPontuam = dto.TodosParticipantesPontuam.Value;

        if (dto.DesclassificadoNaoPontua.HasValue)
            campeonato.DesclassificadoNaoPontua = dto.DesclassificadoNaoPontua.Value;

        if (dto.AbandonoNaoPontua.HasValue)
            campeonato.AbandonoNaoPontua = dto.AbandonoNaoPontua.Value;

        if (!string.IsNullOrWhiteSpace(dto.Status))
            campeonato.Status = dto.Status;

        if (dto.Ativo.HasValue)
            campeonato.Ativo = dto.Ativo.Value;

        campeonato.DataAtualizacao = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id)
            ?? throw new InvalidOperationException("Erro ao atualizar campeonato");
    }

    public async Task<CampeonatoDto> AlterarStatusAsync(int id, string status)
    {
        var validStatuses = new[] { "PLANEJADO", "EM_ANDAMENTO", "FINALIZADO", "CANCELADO" };
        if (!validStatuses.Contains(status.ToUpperInvariant()))
            throw new ArgumentException($"Status inválido. Use: {string.Join(", ", validStatuses)}");

        var campeonato = await _context.Campeonatos.FindAsync(id);
        if (campeonato == null)
            throw new ArgumentException($"Campeonato com ID {id} não encontrado");

        campeonato.Status = status.ToUpperInvariant();
        campeonato.DataAtualizacao = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id)
            ?? throw new InvalidOperationException("Erro ao alterar status");
    }

    public async Task DeleteAsync(int id)
    {
        var campeonato = await _context.Campeonatos.FindAsync(id);
        if (campeonato == null)
            throw new ArgumentException($"Campeonato com ID {id} não encontrado");

        // Verificar se há eventos vinculados
        var hasEventos = await _context.Eventos.AnyAsync(e => e.IdCampeonato == id);
        if (hasEventos)
            throw new InvalidOperationException("Não é possível excluir campeonato com eventos vinculados. Desvincule os eventos primeiro.");

        // Soft delete
        campeonato.Ativo = false;
        campeonato.DataAtualizacao = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Pontuação

    public async Task<List<CampeonatoPontuacaoDto>> GetPontuacoesAsync(int idCampeonato)
    {
        var campeonato = await _context.Campeonatos.FindAsync(idCampeonato);
        if (campeonato == null)
            throw new ArgumentException($"Campeonato com ID {idCampeonato} não encontrado");

        return await _context.CampeonatoPontuacoes
            .Where(p => p.IdCampeonato == idCampeonato)
            .OrderBy(p => p.Posicao)
            .Select(p => new CampeonatoPontuacaoDto
            {
                Id = p.Id,
                IdCampeonato = p.IdCampeonato,
                Posicao = p.Posicao,
                Pontos = p.Pontos
            })
            .ToListAsync();
    }

    public async Task<List<CampeonatoPontuacaoDto>> SetPontuacoesAsync(int idCampeonato, List<CampeonatoPontuacaoCreateDto> pontuacoes)
    {
        var campeonato = await _context.Campeonatos.FindAsync(idCampeonato);
        if (campeonato == null)
            throw new ArgumentException($"Campeonato com ID {idCampeonato} não encontrado");

        // Validar duplicatas de posição
        var posicoes = pontuacoes.Select(p => p.Posicao).ToList();
        if (posicoes.Count != posicoes.Distinct().Count())
            throw new ArgumentException("Não é permitido ter posições duplicadas");

        // Remover pontuações existentes
        var existentes = await _context.CampeonatoPontuacoes
            .Where(p => p.IdCampeonato == idCampeonato)
            .ToListAsync();

        _context.CampeonatoPontuacoes.RemoveRange(existentes);

        // Adicionar novas
        foreach (var dto in pontuacoes.OrderBy(p => p.Posicao))
        {
            _context.CampeonatoPontuacoes.Add(new CampeonatoPontuacao
            {
                IdCampeonato = idCampeonato,
                Posicao = dto.Posicao,
                Pontos = dto.Pontos
            });
        }

        await _context.SaveChangesAsync();

        return await GetPontuacoesAsync(idCampeonato);
    }

    public async Task<List<CampeonatoPontuacaoDto>> ApplyPontuacaoTemplateAsync(int idCampeonato, string template)
    {
        var pontuacoes = template.ToLowerInvariant() switch
        {
            "top10" => new List<CampeonatoPontuacaoCreateDto>
            {
                new() { Posicao = 1, Pontos = 20 },
                new() { Posicao = 2, Pontos = 16 },
                new() { Posicao = 3, Pontos = 14 },
                new() { Posicao = 4, Pontos = 12 },
                new() { Posicao = 5, Pontos = 10 },
                new() { Posicao = 6, Pontos = 8 },
                new() { Posicao = 7, Pontos = 6 },
                new() { Posicao = 8, Pontos = 4 },
                new() { Posicao = 9, Pontos = 2 },
                new() { Posicao = 10, Pontos = 1 }
            },
            "top15" => new List<CampeonatoPontuacaoCreateDto>
            {
                new() { Posicao = 1, Pontos = 20 },
                new() { Posicao = 2, Pontos = 17 },
                new() { Posicao = 3, Pontos = 15 },
                new() { Posicao = 4, Pontos = 13 },
                new() { Posicao = 5, Pontos = 11 },
                new() { Posicao = 6, Pontos = 10 },
                new() { Posicao = 7, Pontos = 9 },
                new() { Posicao = 8, Pontos = 8 },
                new() { Posicao = 9, Pontos = 7 },
                new() { Posicao = 10, Pontos = 6 },
                new() { Posicao = 11, Pontos = 5 },
                new() { Posicao = 12, Pontos = 4 },
                new() { Posicao = 13, Pontos = 3 },
                new() { Posicao = 14, Pontos = 2 },
                new() { Posicao = 15, Pontos = 1 }
            },
            "top20" => new List<CampeonatoPontuacaoCreateDto>
            {
                new() { Posicao = 1, Pontos = 25 },
                new() { Posicao = 2, Pontos = 22 },
                new() { Posicao = 3, Pontos = 20 },
                new() { Posicao = 4, Pontos = 18 },
                new() { Posicao = 5, Pontos = 16 },
                new() { Posicao = 6, Pontos = 15 },
                new() { Posicao = 7, Pontos = 14 },
                new() { Posicao = 8, Pontos = 13 },
                new() { Posicao = 9, Pontos = 12 },
                new() { Posicao = 10, Pontos = 11 },
                new() { Posicao = 11, Pontos = 10 },
                new() { Posicao = 12, Pontos = 9 },
                new() { Posicao = 13, Pontos = 8 },
                new() { Posicao = 14, Pontos = 7 },
                new() { Posicao = 15, Pontos = 6 },
                new() { Posicao = 16, Pontos = 5 },
                new() { Posicao = 17, Pontos = 4 },
                new() { Posicao = 18, Pontos = 3 },
                new() { Posicao = 19, Pontos = 2 },
                new() { Posicao = 20, Pontos = 1 }
            },
            _ => throw new ArgumentException($"Template inválido: {template}. Use: top10, top15 ou top20")
        };

        return await SetPontuacoesAsync(idCampeonato, pontuacoes);
    }

    #endregion

    #region Classificação

    public async Task<ClassificacaoCampeonatoDto> GetClassificacaoAsync(int idCampeonato)
    {
        var campeonato = await _context.Campeonatos
            .Include(c => c.Modalidade)
            .FirstOrDefaultAsync(c => c.Id == idCampeonato);

        if (campeonato == null)
            throw new ArgumentException($"Campeonato com ID {idCampeonato} não encontrado");

        var eventos = await _context.Eventos
            .Where(e => e.IdCampeonato == idCampeonato && e.Ativo)
            .OrderBy(e => e.DataInicio)
            .ToListAsync();

        var eventosRealizados = eventos.Count(e => e.Status == "FINALIZADO");

        // Buscar todas as categorias do campeonato
        var categorias = await _context.EtapaCategorias
            .Where(ec => ec.Etapa.Evento.IdCampeonato == idCampeonato)
            .Select(ec => new { ec.Categoria.Id, ec.Categoria.Nome, ec.Categoria.Sigla })
            .Distinct()
            .ToListAsync();

        var classificacaoCategorias = new List<ClassificacaoCategoriaCampeonatoDto>();

        foreach (var categoria in categorias.DistinctBy(c => c.Nome))
        {
            var classificacaoCategoria = await GetClassificacaoCategoriaAsync(idCampeonato, categoria.Id);
            if (classificacaoCategoria.Pilotos.Any())
                classificacaoCategorias.Add(classificacaoCategoria);
        }

        return new ClassificacaoCampeonatoDto
        {
            IdCampeonato = campeonato.Id,
            NomeCampeonato = campeonato.Nome,
            Sigla = campeonato.Sigla,
            Ano = campeonato.Ano,
            NomeModalidade = campeonato.Modalidade?.Nome ?? "",
            TotalEventos = eventos.Count,
            EventosRealizados = eventosRealizados,
            QtdeEtapasValidas = campeonato.QtdeEtapasValidas,
            Categorias = classificacaoCategorias
        };
    }

    public async Task<ClassificacaoCategoriaCampeonatoDto> GetClassificacaoCategoriaAsync(int idCampeonato, int idCategoria)
    {
        var campeonato = await _context.Campeonatos.FindAsync(idCampeonato);
        if (campeonato == null)
            throw new ArgumentException($"Campeonato com ID {idCampeonato} não encontrado");

        var categoria = await _context.Categorias.FindAsync(idCategoria);
        if (categoria == null)
            throw new ArgumentException($"Categoria com ID {idCategoria} não encontrada");

        var pontuacoes = await _context.CampeonatoPontuacoes
            .Where(p => p.IdCampeonato == idCampeonato)
            .ToDictionaryAsync(p => p.Posicao, p => p.Pontos);

        // TODO: Implementar lógica completa de cálculo de classificação
        // Por enquanto retorna estrutura vazia
        // A lógica completa precisa:
        // 1. Buscar todos os resultados por etapa
        // 2. Aplicar regras de pontuação (PercentualMinimoVoltasLider, ExigeBandeirada, etc)
        // 3. Calcular pontos de cada piloto
        // 4. Aplicar descarte se QtdeEtapasValidas estiver configurado
        // 5. Ordenar por total de pontos

        return new ClassificacaoCategoriaCampeonatoDto
        {
            IdCategoria = idCategoria,
            NomeCategoria = categoria.Nome,
            TotalPilotos = 0,
            Pilotos = new List<PilotoCampeonatoDto>()
        };
    }

    #endregion

    #region Eventos

    public async Task<List<EventoResumoDto>> GetEventosAsync(int idCampeonato)
    {
        var campeonato = await _context.Campeonatos.FindAsync(idCampeonato);
        if (campeonato == null)
            throw new ArgumentException($"Campeonato com ID {idCampeonato} não encontrado");

        return await _context.Eventos
            .Include(e => e.Modalidade)
            .Where(e => e.IdCampeonato == idCampeonato && e.Ativo)
            .OrderBy(e => e.DataInicio)
            .Select(e => new EventoResumoDto
            {
                Id = e.Id,
                IdCampeonato = e.IdCampeonato,
                NomeCampeonato = e.Campeonato != null ? e.Campeonato.Nome : null,
                Nome = e.Nome,
                Local = e.Local,
                Cidade = e.Cidade,
                Uf = e.Uf,
                DataInicio = e.DataInicio,
                DataFim = e.DataFim,
                IdModalidade = e.IdModalidade,
                NomeModalidade = e.Modalidade != null ? e.Modalidade.Nome : "",
                TipoCronometragem = e.Modalidade != null ? e.Modalidade.TipoCronometragem : "",
                Status = e.Status,
                InscricoesAbertas = e.InscricoesAbertas,
                TotalInscritos = e.Inscricoes.Count
            })
            .ToListAsync();
    }

    public async Task VincularEventoAsync(int idCampeonato, int idEvento)
    {
        var campeonato = await _context.Campeonatos.FindAsync(idCampeonato);
        if (campeonato == null)
            throw new ArgumentException($"Campeonato com ID {idCampeonato} não encontrado");

        var evento = await _context.Eventos.FindAsync(idEvento);
        if (evento == null)
            throw new ArgumentException($"Evento com ID {idEvento} não encontrado");

        // Verificar se modalidade é compatível
        if (evento.IdModalidade != campeonato.IdModalidade)
        {
            var modalidadeEvento = await _context.Modalidades.FindAsync(evento.IdModalidade);
            var modalidadeCampeonato = await _context.Modalidades.FindAsync(campeonato.IdModalidade);
            throw new InvalidOperationException(
                $"Modalidade do evento ({modalidadeEvento?.Nome ?? string.Empty}) é diferente da modalidade do campeonato ({modalidadeCampeonato?.Nome ?? string.Empty})");
        }

        evento.IdCampeonato = idCampeonato;
        await _context.SaveChangesAsync();
    }

    public async Task DesvincularEventoAsync(int idCampeonato, int idEvento)
    {
        var evento = await _context.Eventos.FindAsync(idEvento);
        if (evento == null)
            throw new ArgumentException($"Evento com ID {idEvento} não encontrado");

        if (evento.IdCampeonato != idCampeonato)
            throw new ArgumentException($"Evento {idEvento} não está vinculado ao campeonato {idCampeonato}");

        evento.IdCampeonato = null;
        await _context.SaveChangesAsync();
    }

    #endregion
}
