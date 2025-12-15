using AutoMapper;
using DevNationCrono.API.Exceptions;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Models.Pagination;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Interfaces;

namespace DevNationCrono.API.Services.Implementations;

public class InscricaoService : IInscricaoService
{
    private readonly IInscricaoRepository _inscricaoRepository;
    private readonly IPilotoRepository _pilotoRepository;
    private readonly IEventoRepository _eventoRepository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IEtapaRepository _etapaRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<InscricaoService> _logger;

    private static readonly string[] StatusPagamentoValidos =
    {
            "PENDENTE", "AGUARDANDO_PAGAMENTO", "PAGO", "CANCELADO", "REEMBOLSADO"
        };

    private static readonly string[] MetodosPagamentoValidos =
    {
            "PIX", "CARTAO", "DINHEIRO", "CORTESIA"
        };

    public InscricaoService(
        IInscricaoRepository inscricaoRepository,
        IPilotoRepository pilotoRepository,
        IEventoRepository eventoRepository,
        ICategoriaRepository categoriaRepository,
        IEtapaRepository etapaRepository,
        IMapper mapper,
        ILogger<InscricaoService> logger)
    {
        _inscricaoRepository = inscricaoRepository;
        _pilotoRepository = pilotoRepository;
        _eventoRepository = eventoRepository;
        _categoriaRepository = categoriaRepository;
        _etapaRepository = etapaRepository;
        _mapper = mapper;
        _logger = logger;
    }

    #region Buscar

    public async Task<InscricaoDto?> GetByIdAsync(int id)
    {
        var inscricao = await _inscricaoRepository.GetByIdWithDetailsAsync(id);
        return inscricao == null ? null : MapearParaDto(inscricao);
    }

    public async Task<List<InscricaoDto>> GetByEventoAsync(int idEvento)
    {
        var inscricoes = await _inscricaoRepository.GetByEventoAsync(idEvento);
        return inscricoes.Select(MapearParaDto).ToList();
    }

    public async Task<List<InscricaoDto>> GetByEtapaAsync(int idEtapa)
    {
        var inscricoes = await _inscricaoRepository.GetByEtapaAsync(idEtapa);
        return inscricoes.Select(MapearParaDto).ToList();
    }

    public async Task<List<InscricaoDto>> GetByCategoriaAsync(int idCategoria)
    {
        var inscricoes = await _inscricaoRepository.GetByCategoriaAsync(idCategoria);
        return inscricoes.Select(MapearParaDto).ToList();
    }

    public async Task<List<InscricaoDto>> GetByPilotoAsync(int idPiloto)
    {
        var inscricoes = await _inscricaoRepository.GetByPilotoAsync(idPiloto);
        return inscricoes.Select(MapearParaDto).ToList();
    }

    public async Task<PagedResult<InscricaoResumoDto>> GetPagedAsync(InscricaoFilterParams filter)
    {
        var result = await _inscricaoRepository.GetPagedAsync(filter);

        var dtos = result.Items.Select(i => new InscricaoResumoDto
        {
            Id = i.Id,
            NumeroMoto = i.NumeroMoto,
            NomePiloto = i.Piloto.Nome,
            NomeCategoria = i.Categoria.Nome,
            ValorFinal = i.ValorFinal,
            StatusPagamento = i.StatusPagamento,
            DataInscricao = i.DataInscricao
        }).ToList();

        return new PagedResult<InscricaoResumoDto>(
            dtos, result.TotalCount, result.PageNumber, result.PageSize);
    }

    #endregion

    #region Inscrever

    public async Task<InscricaoDto> InscreverAsync(InscricaoCreateDto dto)
    {
        // Validar piloto
        var piloto = await _pilotoRepository.GetByIdAsync(dto.IdPiloto);
        if (piloto == null || !piloto.Ativo)
        {
            throw new NotFoundException("Piloto não encontrado ou inativo");
        }

        // Validar evento
        var evento = await _eventoRepository.GetByIdAsync(dto.IdEvento);
        if (evento == null || !evento.Ativo)
        {
            throw new NotFoundException("Evento não encontrado ou inativo");
        }

        // Validar inscrições abertas
        if (!evento.InscricoesAbertas)
        {
            throw new ValidationException("Inscrições não estão abertas para este evento");
        }

        // Validar categoria
        var categoria = await _categoriaRepository.GetByIdAsync(dto.IdCategoria);
        if (categoria == null || !categoria.Ativo)
        {
            throw new NotFoundException("Categoria não encontrada ou inativa");
        }

        // Validar etapa
        var etapa = await _etapaRepository.GetByIdAsync(dto.IdEtapa);
        if (etapa == null || etapa.Status == "CANCELADA")
        {
            throw new NotFoundException("Etapa não encontrada ou cancelada");
        }

        // Validar etapa pertence ao evento
        if (etapa.IdEvento != dto.IdEvento)
        {
            throw new ValidationException("Etapa não pertence a este evento");
        }
        if (categoria.IdModalidade != evento.IdModalidade)
            throw new ValidationException(
                "Categoria não é da mesma modalidade do evento");

        // Validar vagas disponíveis
        if (categoria.VagasLimitadas && categoria.NumeroVagas.HasValue)
        {
            var inscritosCategoria = await _inscricaoRepository.CountByCategoriaAsync(dto.IdCategoria);
            if (inscritosCategoria >= categoria.NumeroVagas.Value)
            {
                throw new ValidationException($"Não há mais vagas disponíveis na categoria {categoria.Nome}");
            }
        }

        // ===========================================
        // REGRAS POR TIPO DE CRONOMETRAGEM
        // ===========================================
        var tipoCronometragem = evento.Modalidade.TipoCronometragem;

        if (tipoCronometragem == "ENDURO")
        {
            // ENDURO: Apenas UMA inscrição por evento
            var jaInscrito = await _inscricaoRepository.JaInscritoNoEventoAsync(dto.IdPiloto, dto.IdEvento);
            if (jaInscrito)
            {
                throw new ValidationException(
                    "No Enduro, o piloto pode fazer apenas uma inscrição por evento. " +
                    "Você já está inscrito neste evento.");
            }
        }
        else
        {
            // CIRCUITO: Pode múltiplas, mas não na mesma categoria
            var jaInscritoCategoria = await _inscricaoRepository.JaInscritoNaCategoriaAsync(
                dto.IdPiloto, dto.IdCategoria, dto.IdEtapa);

            if (jaInscritoCategoria)
            {
                throw new ValidationException(
                    $"Você já está inscrito na categoria {categoria.Nome} para esta etapa.");
            }
        }

        // Validar idade do piloto para categoria
        await ValidarIdadePilotoCategoria(piloto, categoria);

        // Obter ou gerar número de moto
        int numeroMoto;
        if (dto.NumeroMoto.HasValue)
        {
            // Usar número informado
            if (await _inscricaoRepository.NumeroMotoEmUsoAsync(dto.NumeroMoto.Value, dto.IdEvento))
            {
                throw new ValidationException($"Número de moto {dto.NumeroMoto.Value} já está em uso neste evento");
            }
            numeroMoto = dto.NumeroMoto.Value;
        }
        else
        {
            // Verificar se piloto já tem número neste evento
            var numeroExistente = await _inscricaoRepository.GetNumeroMotoPilotoEventoAsync(dto.IdPiloto, dto.IdEvento);
            if (numeroExistente.HasValue)
            {
                numeroMoto = numeroExistente.Value;
            }
            else
            {
                // Gerar próximo número disponível
                numeroMoto = await _inscricaoRepository.GetProximoNumeroMotoAsync(dto.IdEvento);
            }
        }

        // Calcular valor
        var (valorOriginal, percentualDesconto, valorFinal) = await CalcularValorInscricao(
            dto.IdPiloto, dto.IdEvento, categoria);

        // Criar inscrição
        var inscricao = new Inscricao
        {
            IdPiloto = dto.IdPiloto,
            IdEvento = dto.IdEvento,
            IdCategoria = dto.IdCategoria,
            IdEtapa = dto.IdEtapa,
            NumeroMoto = numeroMoto,
            ValorOriginal = valorOriginal,
            PercentualDesconto = percentualDesconto,
            ValorFinal = valorFinal,
            StatusPagamento = "PENDENTE",
            DataInscricao = DateTime.UtcNow,
            Ativo = true,
            Observacoes = dto.Observacoes
        };

        await _inscricaoRepository.AddAsync(inscricao);

        _logger.LogInformation(
            "Inscrição criada: Piloto {Piloto} no evento {Evento}, categoria {Categoria}, moto #{Moto}",
            piloto.Nome, evento.Nome, categoria.Nome, numeroMoto);

        // Recarregar com includes
        inscricao = await _inscricaoRepository.GetByIdWithDetailsAsync(inscricao.Id);

        return MapearParaDto(inscricao!);
    }

    public async Task<InscricaoMultiplaResponseDto> InscreverMultiplasCategoriasAsync(InscricaoMultiplaCreateDto dto)
    {
        // Validar piloto
        var piloto = await _pilotoRepository.GetByIdAsync(dto.IdPiloto);
        if (piloto == null || !piloto.Ativo)
        {
            throw new NotFoundException("Piloto não encontrado ou inativo");
        }

        // Validar evento
        var evento = await _eventoRepository.GetByIdAsync(dto.IdEvento);
        if (evento == null || !evento.Ativo)
        {
            throw new NotFoundException("Evento não encontrado ou inativo");
        }

        // Validar inscrições abertas
        if (!evento.InscricoesAbertas)
        {
            throw new ValidationException("Inscrições não estão abertas para este evento");
        }

        // ===========================================
        // VALIDAR TIPO DE CRONOMETRAGEM
        // ===========================================
        var tipoCronometragem = evento.Modalidade.TipoCronometragem;

        if (tipoCronometragem == "ENDURO")
        {
            if (dto.IdsCategorias.Count > 1)
            {
                throw new ValidationException(
                    "No Enduro, o piloto pode se inscrever em apenas uma categoria. " +
                    "Use o endpoint de inscrição simples.");
            }

            // Verificar se já está inscrito no evento
            var jaInscrito = await _inscricaoRepository.JaInscritoNoEventoAsync(dto.IdPiloto, dto.IdEvento);
            if (jaInscrito)
            {
                throw new ValidationException(
                    "No Enduro, o piloto pode fazer apenas uma inscrição por evento. " +
                    "Você já está inscrito neste evento.");
            }
        }

        // Validar etapa
        var etapa = await _etapaRepository.GetByIdAsync(dto.IdEtapa);
        if (etapa == null || etapa.Status == "CANCELADA")
        {
            throw new NotFoundException("Etapa não encontrada ou cancelada");
        }

        if (etapa.IdEvento != dto.IdEvento)
        {
            throw new ValidationException("Etapa não pertence a este evento");
        }

        // Validar categorias
        var categorias = new List<Categoria>();
        foreach (var idCategoria in dto.IdsCategorias.Distinct())
        {
            var categoria = await _categoriaRepository.GetByIdAsync(idCategoria);
            if (categoria == null || !categoria.Ativo)
            {
                throw new NotFoundException($"Categoria com ID {idCategoria} não encontrada ou inativa");
            }

            // Verificar se já inscrito nesta categoria
            var jaInscritoCategoria = await _inscricaoRepository.JaInscritoNaCategoriaAsync(
                dto.IdPiloto, idCategoria, dto.IdEtapa);

            if (jaInscritoCategoria)
            {
                throw new ValidationException($"Você já está inscrito na categoria {categoria.Nome}");
            }

            // Validar vagas
            if (categoria.VagasLimitadas && categoria.NumeroVagas.HasValue)
            {
                var inscritosCategoria = await _inscricaoRepository.CountByCategoriaAsync(idCategoria);
                if (inscritosCategoria >= categoria.NumeroVagas.Value)
                {
                    throw new ValidationException($"Não há vagas disponíveis na categoria {categoria.Nome}");
                }
            }

            // Validar idade
            await ValidarIdadePilotoCategoria(piloto, categoria);

            categorias.Add(categoria);
        }

        // Ordenar por valor (maior primeiro - a de maior valor não tem desconto)
        categorias = categorias.OrderByDescending(c => c.ValorInscricao).ToList();

        // Obter número de moto
        int numeroMoto;
        if (dto.NumeroMoto.HasValue)
        {
            if (await _inscricaoRepository.NumeroMotoEmUsoAsync(dto.NumeroMoto.Value, dto.IdEvento))
            {
                throw new ValidationException($"Número de moto {dto.NumeroMoto.Value} já está em uso");
            }
            numeroMoto = dto.NumeroMoto.Value;
        }
        else
        {
            var numeroExistente = await _inscricaoRepository.GetNumeroMotoPilotoEventoAsync(dto.IdPiloto, dto.IdEvento);
            numeroMoto = numeroExistente ?? await _inscricaoRepository.GetProximoNumeroMotoAsync(dto.IdEvento);
        }

        // Contar inscrições existentes para cálculo de desconto
        var inscricoesExistentes = await _inscricaoRepository.ContarInscricoesPilotoEventoAsync(dto.IdPiloto, dto.IdEvento);

        // Criar inscrições
        var inscricoes = new List<Inscricao>();
        var inscricoesCategoriaDto = new List<InscricaoCategoriaDto>();
        decimal valorTotalOriginal = 0;
        decimal valorTotalDesconto = 0;
        decimal valorTotalFinal = 0;

        for (int i = 0; i < categorias.Count; i++)
        {
            var categoria = categorias[i];
            var ordem = inscricoesExistentes + i + 1; // Posição total considerando existentes

            // Calcular desconto
            decimal valorOriginal = categoria.ValorInscricao;
            decimal percentualDesconto = 0;
            decimal valorFinal = valorOriginal;

            // A primeira categoria (ordem 1) não tem desconto
            // A partir da segunda, aplica desconto configurado
            if (ordem > 1 && categoria.DescontoSegundaCategoria > 0)
            {
                percentualDesconto = categoria.DescontoSegundaCategoria;
                var valorDesconto = valorOriginal * (percentualDesconto / 100);
                valorFinal = valorOriginal - valorDesconto;
            }

            var inscricao = new Inscricao
            {
                IdPiloto = dto.IdPiloto,
                IdEvento = dto.IdEvento,
                IdCategoria = categoria.Id,
                IdEtapa = dto.IdEtapa,
                NumeroMoto = numeroMoto,
                ValorOriginal = valorOriginal,
                PercentualDesconto = percentualDesconto,
                ValorFinal = valorFinal,
                StatusPagamento = "PENDENTE",
                DataInscricao = DateTime.UtcNow,
                Ativo = true,
                Observacoes = dto.Observacoes
            };

            inscricoes.Add(inscricao);

            inscricoesCategoriaDto.Add(new InscricaoCategoriaDto
            {
                IdCategoria = categoria.Id,
                NomeCategoria = categoria.Nome,
                ValorOriginal = valorOriginal,
                PercentualDesconto = percentualDesconto,
                ValorDesconto = valorOriginal - valorFinal,
                ValorFinal = valorFinal,
                Ordem = ordem
            });

            valorTotalOriginal += valorOriginal;
            valorTotalDesconto += (valorOriginal - valorFinal);
            valorTotalFinal += valorFinal;
        }

        // Salvar todas as inscrições
        await _inscricaoRepository.AddRangeAsync(inscricoes);

        // Atualizar IDs nas DTOs
        for (int i = 0; i < inscricoes.Count; i++)
        {
            inscricoesCategoriaDto[i].IdInscricao = inscricoes[i].Id;
        }

        _logger.LogInformation(
            "Inscrições múltiplas criadas: Piloto {Piloto} no evento {Evento}, {QtdCategorias} categorias, moto #{Moto}",
            piloto.Nome, evento.Nome, categorias.Count, numeroMoto);

        // Gerar QR Code PIX para o valor total
        var (qrCode, codigoPix) = await GerarDadosPixAsync(valorTotalFinal, piloto.Nome, evento.Nome);

        // Atualizar inscrições com dados do PIX
        foreach (var inscricao in inscricoes)
        {
            inscricao.QrCodePix = qrCode;
            inscricao.CodigoPix = codigoPix;
            inscricao.StatusPagamento = "AGUARDANDO_PAGAMENTO";
            await _inscricaoRepository.UpdateAsync(inscricao);
        }

        return new InscricaoMultiplaResponseDto
        {
            IdPiloto = piloto.Id,
            NomePiloto = piloto.Nome,
            IdEvento = evento.Id,
            NomeEvento = evento.Nome,
            NumeroMoto = numeroMoto,
            Inscricoes = inscricoesCategoriaDto,
            ValorTotalOriginal = valorTotalOriginal,
            ValorTotalDesconto = valorTotalDesconto,
            ValorTotalFinal = valorTotalFinal,
            QrCodePix = qrCode,
            CodigoPix = codigoPix
        };
    }

    #endregion

    #region Simular Valores

    public async Task<SimulacaoInscricaoResponseDto> SimularValoresAsync(SimularInscricaoDto dto)
    {
        // Validar piloto
        var piloto = await _pilotoRepository.GetByIdAsync(dto.IdPiloto);
        if (piloto == null)
        {
            throw new NotFoundException("Piloto não encontrado");
        }

        // Validar evento
        var evento = await _eventoRepository.GetByIdAsync(dto.IdEvento);
        if (evento == null)
        {
            throw new NotFoundException("Evento não encontrado");
        }

        var tipoCronometragem = evento.Modalidade.TipoCronometragem;
        var permiteMultiplas = tipoCronometragem != "ENDURO";
        var avisos = new List<string>();

        // Verificar inscrições existentes
        var inscricoesExistentes = await _inscricaoRepository.GetByPilotoEventoAsync(dto.IdPiloto, dto.IdEvento);
        var categoriasJaInscritas = inscricoesExistentes.Select(i => i.IdCategoria).ToList();

        if (tipoCronometragem == "ENDURO" && inscricoesExistentes.Any())
        {
            avisos.Add("ATENÇÃO: No Enduro você já está inscrito neste evento. Não é possível adicionar mais categorias.");
        }

        if (tipoCronometragem == "ENDURO" && dto.IdsCategorias.Count > 1)
        {
            avisos.Add("ATENÇÃO: No Enduro você pode se inscrever em apenas uma categoria.");
        }

        // Buscar categorias
        var categorias = new List<Categoria>();
        foreach (var idCategoria in dto.IdsCategorias.Distinct())
        {
            var categoria = await _categoriaRepository.GetByIdAsync(idCategoria);
            if (categoria != null && categoria.Ativo)
            {
                categorias.Add(categoria);
            }
        }

        // Ordenar por valor
        categorias = categorias.OrderByDescending(c => c.ValorInscricao).ToList();

        // Obter desconto configurado (pegar da primeira categoria)
        decimal percentualDescontoConfigurado = categorias.FirstOrDefault()?.DescontoSegundaCategoria ?? 0;

        // Calcular valores
        var simulacaoCategorias = new List<SimulacaoCategoriaDto>();
        var totalExistentes = inscricoesExistentes.Count;
        decimal valorTotalOriginal = 0;
        decimal valorTotalDesconto = 0;
        decimal valorTotalFinal = 0;

        for (int i = 0; i < categorias.Count; i++)
        {
            var categoria = categorias[i];
            var ordem = totalExistentes + i + 1;
            var jaInscrito = categoriasJaInscritas.Contains(categoria.Id);

            decimal valorOriginal = categoria.ValorInscricao;
            decimal percentualDesconto = 0;
            decimal valorFinal = valorOriginal;

            if (!jaInscrito)
            {
                if (ordem > 1 && categoria.DescontoSegundaCategoria > 0 && permiteMultiplas)
                {
                    percentualDesconto = categoria.DescontoSegundaCategoria;
                    var valorDesc = valorOriginal * (percentualDesconto / 100);
                    valorFinal = valorOriginal - valorDesc;
                }

                valorTotalOriginal += valorOriginal;
                valorTotalDesconto += (valorOriginal - valorFinal);
                valorTotalFinal += valorFinal;
            }

            simulacaoCategorias.Add(new SimulacaoCategoriaDto
            {
                IdCategoria = categoria.Id,
                NomeCategoria = categoria.Nome,
                ValorOriginal = valorOriginal,
                PercentualDesconto = percentualDesconto,
                ValorDesconto = valorOriginal - valorFinal,
                ValorFinal = valorFinal,
                Ordem = ordem,
                JaInscrito = jaInscrito
            });

            if (jaInscrito)
            {
                avisos.Add($"Você já está inscrito na categoria {categoria.Nome}");
            }
        }

        return new SimulacaoInscricaoResponseDto
        {
            TipoCronometragem = tipoCronometragem,
            PermiteMultiplasCategorias = permiteMultiplas,
            PercentualDescontoConfigurado = percentualDescontoConfigurado,
            Categorias = simulacaoCategorias,
            ValorTotalOriginal = valorTotalOriginal,
            ValorTotalDesconto = valorTotalDesconto,
            ValorTotalFinal = valorTotalFinal,
            Avisos = avisos
        };
    }

    #endregion

    #region Atualizar

    public async Task<InscricaoDto> UpdateAsync(int id, InscricaoUpdateDto dto)
    {
        var inscricao = await _inscricaoRepository.GetByIdWithDetailsAsync(id);

        if (inscricao == null)
        {
            throw new NotFoundException("Inscrição não encontrada");
        }

        if (dto.NumeroMoto.HasValue && dto.NumeroMoto.Value != inscricao.NumeroMoto)
        {
            if (await _inscricaoRepository.NumeroMotoEmUsoAsync(dto.NumeroMoto.Value, inscricao.IdEvento, id))
            {
                throw new ValidationException($"Número de moto {dto.NumeroMoto.Value} já está em uso");
            }
            inscricao.NumeroMoto = dto.NumeroMoto.Value;
        }

        if (!string.IsNullOrEmpty(dto.StatusPagamento))
        {
            if (!StatusPagamentoValidos.Contains(dto.StatusPagamento))
            {
                throw new ValidationException($"Status de pagamento inválido. Valores: {string.Join(", ", StatusPagamentoValidos)}");
            }
            inscricao.StatusPagamento = dto.StatusPagamento;
        }

        if (!string.IsNullOrEmpty(dto.MetodoPagamento))
        {
            if (!MetodosPagamentoValidos.Contains(dto.MetodoPagamento))
            {
                throw new ValidationException($"Método de pagamento inválido. Valores: {string.Join(", ", MetodosPagamentoValidos)}");
            }
            inscricao.MetodoPagamento = dto.MetodoPagamento;
        }

        if (dto.TransacaoId != null)
            inscricao.TransacaoId = dto.TransacaoId;

        if (dto.DataPagamento.HasValue)
            inscricao.DataPagamento = dto.DataPagamento;

        if (dto.Observacoes != null)
            inscricao.Observacoes = dto.Observacoes;

        if (dto.Ativo.HasValue)
            inscricao.Ativo = dto.Ativo.Value;

        await _inscricaoRepository.UpdateAsync(inscricao);

        _logger.LogInformation("Inscrição atualizada: ID {Id}", id);

        return MapearParaDto(inscricao);
    }

    #endregion

    #region Pagamento

    public async Task<InscricaoDto> ConfirmarPagamentoAsync(int id, ConfirmarPagamentoDto dto)
    {
        var inscricao = await _inscricaoRepository.GetByIdWithDetailsAsync(id);

        if (inscricao == null)
        {
            throw new NotFoundException("Inscrição não encontrada");
        }

        if (inscricao.StatusPagamento == "PAGO")
        {
            throw new ValidationException("Esta inscrição já está paga");
        }

        if (inscricao.StatusPagamento == "CANCELADO")
        {
            throw new ValidationException("Esta inscrição foi cancelada");
        }

        if (!MetodosPagamentoValidos.Contains(dto.MetodoPagamento))
        {
            throw new ValidationException($"Método de pagamento inválido. Valores: {string.Join(", ", MetodosPagamentoValidos)}");
        }

        inscricao.StatusPagamento = "PAGO";
        inscricao.MetodoPagamento = dto.MetodoPagamento;
        inscricao.TransacaoId = dto.TransacaoId;
        inscricao.DataPagamento = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(dto.Observacoes))
        {
            inscricao.Observacoes = string.IsNullOrEmpty(inscricao.Observacoes)
                ? dto.Observacoes
                : $"{inscricao.Observacoes}\n{dto.Observacoes}";
        }

        await _inscricaoRepository.UpdateAsync(inscricao);

        _logger.LogInformation(
            "Pagamento confirmado: Inscrição {Id}, Método: {Metodo}, Valor: {Valor}",
            id, dto.MetodoPagamento, inscricao.ValorFinal);

        return MapearParaDto(inscricao);
    }

    public async Task<InscricaoDto> CancelarInscricaoAsync(int id, string? motivo = null)
    {
        var inscricao = await _inscricaoRepository.GetByIdWithDetailsAsync(id);

        if (inscricao == null)
        {
            throw new NotFoundException("Inscrição não encontrada");
        }

        if (inscricao.StatusPagamento == "CANCELADO")
        {
            throw new ValidationException("Esta inscrição já está cancelada");
        }

        // Se estava pago, mudar para reembolsado
        if (inscricao.StatusPagamento == "PAGO")
        {
            inscricao.StatusPagamento = "REEMBOLSADO";
        }
        else
        {
            inscricao.StatusPagamento = "CANCELADO";
        }

        inscricao.Ativo = false;

        if (!string.IsNullOrEmpty(motivo))
        {
            inscricao.Observacoes = string.IsNullOrEmpty(inscricao.Observacoes)
                ? $"Cancelado: {motivo}"
                : $"{inscricao.Observacoes}\nCancelado: {motivo}";
        }

        await _inscricaoRepository.UpdateAsync(inscricao);

        _logger.LogInformation("Inscrição cancelada: ID {Id}", id);

        return MapearParaDto(inscricao);
    }

    public async Task<string> GerarQrCodePixAsync(int idInscricao)
    {
        var inscricao = await _inscricaoRepository.GetByIdWithDetailsAsync(idInscricao);

        if (inscricao == null)
        {
            throw new NotFoundException("Inscrição não encontrada");
        }

        if (inscricao.StatusPagamento == "PAGO")
        {
            throw new ValidationException("Esta inscrição já está paga");
        }

        var (qrCode, codigoPix) = await GerarDadosPixAsync(
            inscricao.ValorFinal,
            inscricao.Piloto.Nome,
            inscricao.Evento.Nome);

        inscricao.QrCodePix = qrCode;
        inscricao.CodigoPix = codigoPix;
        inscricao.StatusPagamento = "AGUARDANDO_PAGAMENTO";

        await _inscricaoRepository.UpdateAsync(inscricao);

        return qrCode;
    }

    public async Task<List<string>> GerarQrCodePixMultiplasAsync(List<int> idsInscricoes)
    {
        var qrCodes = new List<string>();

        foreach (var id in idsInscricoes)
        {
            var qrCode = await GerarQrCodePixAsync(id);
            qrCodes.Add(qrCode);
        }

        return qrCodes;
    }

    #endregion

    #region Número de Moto

    public async Task<bool> ValidarNumeroMotoAsync(int numeroMoto, int idEvento, int? excludeId = null)
    {
        return !await _inscricaoRepository.NumeroMotoEmUsoAsync(numeroMoto, idEvento, excludeId);
    }

    public async Task<InscricaoDto> AlterarNumeroMotoAsync(int id, int novoNumero)
    {
        var inscricao = await _inscricaoRepository.GetByIdWithDetailsAsync(id);

        if (inscricao == null)
        {
            throw new NotFoundException("Inscrição não encontrada");
        }

        if (await _inscricaoRepository.NumeroMotoEmUsoAsync(novoNumero, inscricao.IdEvento, id))
        {
            throw new ValidationException($"Número de moto {novoNumero} já está em uso neste evento");
        }

        // Atualizar todas as inscrições do piloto neste evento
        var inscricoesPiloto = await _inscricaoRepository.GetByPilotoEventoAsync(
            inscricao.IdPiloto, inscricao.IdEvento);

        foreach (var insc in inscricoesPiloto)
        {
            insc.NumeroMoto = novoNumero;
            await _inscricaoRepository.UpdateAsync(insc);
        }

        _logger.LogInformation(
            "Número de moto alterado: Piloto {Piloto}, Evento {Evento}, Novo número: {Numero}",
            inscricao.Piloto.Nome, inscricao.Evento.Nome, novoNumero);

        // Recarregar
        inscricao = await _inscricaoRepository.GetByIdWithDetailsAsync(id);
        return MapearParaDto(inscricao!);
    }

    #endregion

    #region Estatísticas

    public async Task<EstatisticasInscricaoDto> GetEstatisticasEventoAsync(int idEvento)
    {
        var evento = await _eventoRepository.GetByIdAsync(idEvento);
        if (evento == null)
        {
            throw new NotFoundException("Evento não encontrado");
        }

        var inscricoes = await _inscricaoRepository.GetByEventoAsync(idEvento);

        var totalInscritos = inscricoes.Count;
        var totalPagos = inscricoes.Count(i => i.StatusPagamento == "PAGO");
        var totalPendentes = inscricoes.Count(i => i.StatusPagamento == "PENDENTE" || i.StatusPagamento == "AGUARDANDO_PAGAMENTO");
        var totalCancelados = inscricoes.Count(i => i.StatusPagamento == "CANCELADO" || i.StatusPagamento == "REEMBOLSADO");

        var valorArrecadado = inscricoes
            .Where(i => i.StatusPagamento == "PAGO")
            .Sum(i => i.ValorFinal);

        var valorPendente = inscricoes
            .Where(i => i.StatusPagamento == "PENDENTE" || i.StatusPagamento == "AGUARDANDO_PAGAMENTO")
            .Sum(i => i.ValorFinal);

        var inscritosPorCategoria = inscricoes
            .Where(i => i.StatusPagamento != "CANCELADO" && i.StatusPagamento != "REEMBOLSADO")
            .GroupBy(i => i.Categoria.Nome)
            .ToDictionary(g => g.Key, g => g.Count());

        return new EstatisticasInscricaoDto
        {
            TotalInscritos = totalInscritos,
            TotalPagos = totalPagos,
            TotalPendentes = totalPendentes,
            TotalCancelados = totalCancelados,
            ValorTotalArrecadado = valorArrecadado,
            ValorTotalPendente = valorPendente,
            InscritosPorCategoria = inscritosPorCategoria
        };
    }

    #endregion

    #region Métodos Privados

    private async Task<(decimal valorOriginal, decimal percentualDesconto, decimal valorFinal)>
        CalcularValorInscricao(int idPiloto, int idEvento, Categoria categoria)
    {
        var valorOriginal = categoria.ValorInscricao;
        decimal percentualDesconto = 0;
        decimal valorFinal = valorOriginal;

        // Contar inscrições existentes do piloto neste evento
        var inscricoesExistentes = await _inscricaoRepository.ContarInscricoesPilotoEventoAsync(idPiloto, idEvento);

        // Se já tem inscrições (esta é 2ª ou posterior), aplicar desconto
        if (inscricoesExistentes > 0 && categoria.DescontoSegundaCategoria > 0)
        {
            percentualDesconto = categoria.DescontoSegundaCategoria;
            var valorDesconto = valorOriginal * (percentualDesconto / 100);
            valorFinal = valorOriginal - valorDesconto;
        }

        return (valorOriginal, percentualDesconto, valorFinal);
    }

    private async Task ValidarIdadePilotoCategoria(Piloto piloto, Categoria categoria)
    {
        var idade = CalcularIdade(piloto.DataNascimento);

        if (categoria.IdadeMinima.HasValue && idade < categoria.IdadeMinima.Value)
        {
            throw new ValidationException(
                $"Idade mínima para a categoria {categoria.Nome} é {categoria.IdadeMinima} anos. " +
                $"Sua idade: {idade} anos.");
        }

        if (categoria.IdadeMaxima.HasValue && idade > categoria.IdadeMaxima.Value)
        {
            throw new ValidationException(
                $"Idade máxima para a categoria {categoria.Nome} é {categoria.IdadeMaxima} anos. " +
                $"Sua idade: {idade} anos.");
        }
    }

    private int CalcularIdade(DateTime dataNascimento)
    {
        var hoje = DateTime.Today;
        var idade = hoje.Year - dataNascimento.Year;

        if (dataNascimento.Date > hoje.AddYears(-idade))
            idade--;

        return idade;
    }

    private async Task<(string qrCode, string codigoPix)> GerarDadosPixAsync(
        decimal valor, string nomePiloto, string nomeEvento)
    {
        // TODO: Integrar com API de pagamento (Mercado Pago, Asaas, etc.)
        // Por enquanto, gerar dados fictícios para teste

        var timestamp = DateTime.UtcNow.Ticks;
        var codigoPix = $"PIX{timestamp}";

        // Em produção, este seria o payload EMV do PIX
        var qrCodeData = $"00020126580014br.gov.bcb.pix0136{Guid.NewGuid()}" +
                        $"5204000053039865406{valor:F2}5802BR" +
                        $"5913{nomePiloto.Substring(0, Math.Min(13, nomePiloto.Length))}" +
                        $"6008CORRIDA62070503***6304";

        _logger.LogInformation(
            "QR Code PIX gerado: Valor {Valor}, Piloto: {Piloto}, Evento: {Evento}",
            valor, nomePiloto, nomeEvento);

        return (qrCodeData, codigoPix);
    }

    private InscricaoDto MapearParaDto(Inscricao inscricao)
    {
        return new InscricaoDto
        {
            Id = inscricao.Id,
            IdPiloto = inscricao.IdPiloto,
            NomePiloto = inscricao.Piloto?.Nome ?? "",
            CpfPiloto = inscricao.Piloto?.Cpf ?? "",
            TelefonePiloto = inscricao.Piloto?.Telefone,
            IdEvento = inscricao.IdEvento,
            NomeEvento = inscricao.Evento?.Nome ?? "",
            DataEvento = inscricao.Evento?.DataInicio ?? DateTime.MinValue,
            IdCategoria = inscricao.IdCategoria,
            NomeCategoria = inscricao.Categoria?.Nome ?? "",
            IdEtapa = inscricao.IdEtapa,
            NomeEtapa = inscricao.Etapa?.Nome ?? "",
            NumeroEtapa = inscricao.Etapa?.NumeroEtapa ?? 0,
            NumeroMoto = inscricao.NumeroMoto,
            ValorOriginal = inscricao.ValorOriginal,
            PercentualDesconto = inscricao.PercentualDesconto,
            ValorFinal = inscricao.ValorFinal,
            StatusPagamento = inscricao.StatusPagamento,
            MetodoPagamento = inscricao.MetodoPagamento,
            TransacaoId = inscricao.TransacaoId,
            DataPagamento = inscricao.DataPagamento,
            DataInscricao = inscricao.DataInscricao,
            Ativo = inscricao.Ativo,
            Observacoes = inscricao.Observacoes
        };
    }

    #endregion
}
