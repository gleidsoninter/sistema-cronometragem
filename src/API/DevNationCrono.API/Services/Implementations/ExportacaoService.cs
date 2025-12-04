using DevNationCrono.API.Exceptions;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DevNationCrono.API.Services.Implementations;

public class ExportacaoService : IExportacaoService
{
    private readonly IResultadoCircuitoService _resultadoCircuitoService;
    private readonly IResultadoEnduroService _resultadoEnduroService;
    private readonly IEtapaRepository _etapaRepository;
    private readonly ILogger<ExportacaoService> _logger;

    public ExportacaoService(
        IResultadoCircuitoService resultadoCircuitoService,
        IResultadoEnduroService resultadoEnduroService,
        IEtapaRepository etapaRepository,
        ILogger<ExportacaoService> logger)
    {
        _resultadoCircuitoService = resultadoCircuitoService;
        _resultadoEnduroService = resultadoEnduroService;
        _etapaRepository = etapaRepository;
        _logger = logger;

        // Configurar QuestPDF (necessário na primeira execução)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> ExportarClassificacaoGeralPdfAsync(int idEtapa)
    {
        var etapa = await _etapaRepository.GetByIdAsync(idEtapa);
        if (etapa == null)
            throw new NotFoundException("Etapa não encontrada");

        var tipoCronometragem = etapa.Evento.Modalidade.TipoCronometragem;

        if (tipoCronometragem == "CIRCUITO")
        {
            return await ExportarCircuitoGeralPdfAsync(idEtapa, etapa);
        }
        else
        {
            return await ExportarEnduroGeralPdfAsync(idEtapa, etapa);
        }
    }

    private async Task<byte[]> ExportarCircuitoGeralPdfAsync(int idEtapa, Etapa etapa)
    {
        var classificacao = await _resultadoCircuitoService.CalcularClassificacaoGeralAsync(idEtapa);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);

                // Cabeçalho
                page.Header().Element(c => ComporCabecalho(c, classificacao.NomeEvento, classificacao.NomeEtapa, classificacao.DataEtapa));

                // Conteúdo
                page.Content().Element(c => ComporClassificacaoCircuito(c, classificacao));

                // Rodapé
                page.Footer().Element(ComporRodape);
            });
        });

        return document.GeneratePdf();
    }

    private async Task<byte[]> ExportarEnduroGeralPdfAsync(int idEtapa, Etapa etapa)
    {
        var classificacao = await _resultadoEnduroService.CalcularClassificacaoGeralAsync(idEtapa);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);

                page.Header().Element(c => ComporCabecalho(c, classificacao.NomeEvento, classificacao.NomeEtapa, classificacao.DataEtapa));
                page.Content().Element(c => ComporClassificacaoEnduro(c, classificacao));
                page.Footer().Element(ComporRodape);
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> ExportarClassificacaoCategoriaPdfAsync(int idEtapa, int idCategoria)
    {
        var etapa = await _etapaRepository.GetByIdAsync(idEtapa);
        if (etapa == null)
            throw new NotFoundException("Etapa não encontrada");

        var classificacao = await _resultadoCircuitoService.CalcularClassificacaoCategoriaAsync(idEtapa, idCategoria);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);

                page.Header().Element(c => ComporCabecalhoCateogria(c, etapa.Evento.Nome, etapa.Nome, classificacao.NomeCategoria, etapa.DataHora));
                page.Content().Element(c => ComporClassificacaoCategoria(c, classificacao));
                page.Footer().Element(ComporRodape);
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> ExportarResultadoPilotoPdfAsync(int idEtapa, int numeroMoto)
    {
        var resultado = await _resultadoCircuitoService.GetResultadoPilotoAsync(idEtapa, numeroMoto);
        var etapa = await _etapaRepository.GetByIdAsync(idEtapa);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);

                page.Header().Element(c => ComporCabecalhoPiloto(c, etapa, resultado));
                page.Content().Element(c => ComporResultadoPiloto(c, resultado));
                page.Footer().Element(ComporRodape);
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> ExportarRankingMelhorVoltaPdfAsync(int idEtapa)
    {
        var ranking = await _resultadoCircuitoService.GetRankingMelhorVoltaAsync(idEtapa);
        var etapa = await _etapaRepository.GetByIdAsync(idEtapa);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);

                page.Header()
                    .Text($"RANKING MELHOR VOLTA - {etapa.Evento.Nome}")
                    .FontSize(16)
                    .Bold()
                    .AlignCenter();

                page.Content().Element(c => ComporRankingMelhorVolta(c, ranking));
                page.Footer().Element(ComporRodape);
            });
        });

        return document.GeneratePdf();
    }

    #region Componentes PDF

    private void ComporCabecalho(IContainer container, string evento, string etapa, DateTime data)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(evento).FontSize(18).Bold();
                    c.Item().Text(etapa).FontSize(14);
                    c.Item().Text($"Data: {data:dd/MM/yyyy}").FontSize(10);
                });

                row.ConstantItem(100).AlignRight().Text("RESULTADO OFICIAL").FontSize(10).Bold();
            });

            column.Item().PaddingVertical(5).LineHorizontal(1);
            column.Item().PaddingBottom(10);
        });
    }

    private void ComporCabecalhoCateogria(IContainer container, string evento, string etapa, string categoria, DateTime data)
    {
        container.Column(column =>
        {
            column.Item().Text(evento).FontSize(18).Bold().AlignCenter();
            column.Item().Text($"{etapa} - {categoria}").FontSize(14).AlignCenter();
            column.Item().Text($"Data: {data:dd/MM/yyyy}").FontSize(10).AlignCenter();
            column.Item().PaddingVertical(5).LineHorizontal(1);
            column.Item().PaddingBottom(10);
        });
    }

    private void ComporCabecalhoPiloto(IContainer container, Etapa etapa, ResultadoPilotoCircuitoDto resultado)
    {
        container.Column(column =>
        {
            column.Item().Text(etapa.Evento.Nome).FontSize(16).Bold().AlignCenter();
            column.Item().Text(etapa.Nome).FontSize(12).AlignCenter();
            column.Item().PaddingTop(10);
            column.Item().Text($"#{resultado.NumeroMoto} - {resultado.NomePiloto}").FontSize(20).Bold().AlignCenter();
            column.Item().Text(resultado.NomeCategoria).FontSize(12).AlignCenter();
            column.Item().PaddingVertical(5).LineHorizontal(1);
        });
    }

    private void ComporClassificacaoCircuito(IContainer container, ClassificacaoGeralCircuitoDto classificacao)
    {
        container.Column(column =>
        {
            // Estatísticas
            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"Total: {classificacao.TotalInscritos} pilotos").FontSize(9);
                row.RelativeItem().Text($"Finalizados: {classificacao.TotalFinalizados}").FontSize(9);
                row.RelativeItem().Text($"Voltas Líder: {classificacao.VoltasLider}").FontSize(9);
            });

            if (!string.IsNullOrEmpty(classificacao.MelhorVoltaGeralFormatado))
            {
                column.Item().PaddingTop(5).Text(
                    $"Melhor Volta: {classificacao.MelhorVoltaGeralFormatado} - #{classificacao.MotoMelhorVoltaGeral} {classificacao.PilotoMelhorVoltaGeral}")
                    .FontSize(9).Bold();
            }

            column.Item().PaddingTop(10);

            // Tabela de classificação
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);  // Pos
                    columns.ConstantColumn(40);  // Moto
                    columns.RelativeColumn(3);   // Piloto
                    columns.RelativeColumn(2);   // Categoria
                    columns.ConstantColumn(40);  // Voltas
                    columns.ConstantColumn(70);  // Tempo
                    columns.ConstantColumn(70);  // Diferença
                    columns.ConstantColumn(60);  // Melhor Volta
                });

                // Cabeçalho
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Pos").Bold().FontSize(8);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Moto").Bold().FontSize(8);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Piloto").Bold().FontSize(8);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Categoria").Bold().FontSize(8);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Voltas").Bold().FontSize(8);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Tempo").Bold().FontSize(8);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Diferença").Bold().FontSize(8);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("M. Volta").Bold().FontSize(8);
                });

                // Dados
                foreach (var r in classificacao.Classificacao.Where(x => x.Status == "CORRENDO" || x.Status == "FINALIZADO"))
                {
                    var bgColor = r.PosicaoGeral <= 3
                        ? (r.PosicaoGeral == 1 ? Colors.Yellow.Lighten3 : Colors.Grey.Lighten4)
                        : Colors.White;

                    table.Cell().Background(bgColor).Padding(2).Text(r.PosicaoGeral.ToString()).FontSize(8);
                    table.Cell().Background(bgColor).Padding(2).Text(r.NumeroMoto.ToString()).FontSize(8).Bold();
                    table.Cell().Background(bgColor).Padding(2).Text(r.NomePiloto).FontSize(8);
                    table.Cell().Background(bgColor).Padding(2).Text(r.NomeCategoria).FontSize(7);
                    table.Cell().Background(bgColor).Padding(2).Text(r.VoltasCompletadas.ToString()).FontSize(8).AlignCenter();
                    table.Cell().Background(bgColor).Padding(2).Text(r.TempoTotalFormatado).FontSize(8);
                    table.Cell().Background(bgColor).Padding(2).Text(r.DiferencaLiderFormatado ?? "-").FontSize(8);
                    table.Cell().Background(bgColor).Padding(2).Text(r.MelhorVoltaFormatado ?? "-").FontSize(8);
                }
            });
        });
    }

    private void ComporClassificacaoEnduro(IContainer container, ClassificacaoGeralEnduroDto classificacao)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"Total: {classificacao.TotalInscritos} pilotos").FontSize(9);
                row.RelativeItem().Text($"Classificados: {classificacao.TotalClassificados}").FontSize(9);
                row.RelativeItem().Text($"Especiais: {classificacao.NumeroEspeciais} x {classificacao.NumeroVoltas} voltas").FontSize(9);
            });

            column.Item().PaddingTop(10);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);  // Pos
                    columns.ConstantColumn(35);  // Moto
                    columns.RelativeColumn(3);   // Piloto
                    columns.RelativeColumn(2);   // Categoria
                    columns.ConstantColumn(70);  // Tempo Total
                    columns.ConstantColumn(60);  // Diferença
                    columns.ConstantColumn(30);  // Pen
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text("Pos").Bold().FontSize(7);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text("Moto").Bold().FontSize(7);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text("Piloto").Bold().FontSize(7);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text("Categoria").Bold().FontSize(7);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text("Tempo").Bold().FontSize(7);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text("Diferença").Bold().FontSize(7);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text("Pen").Bold().FontSize(7);
                });

                foreach (var r in classificacao.Classificacao.Where(x => x.Status == "CLASSIFICADO"))
                {
                    var bgColor = r.Posicao <= 3 ? Colors.Yellow.Lighten4 : Colors.White;

                    table.Cell().Background(bgColor).Padding(2).Text(r.Posicao.ToString()).FontSize(7);
                    table.Cell().Background(bgColor).Padding(2).Text(r.NumeroMoto.ToString()).FontSize(7).Bold();
                    table.Cell().Background(bgColor).Padding(2).Text(r.NomePiloto).FontSize(7);
                    table.Cell().Background(bgColor).Padding(2).Text(r.NomeCategoria).FontSize(6);
                    table.Cell().Background(bgColor).Padding(2).Text(r.TempoTotalFormatado).FontSize(7);
                    table.Cell().Background(bgColor).Padding(2).Text(r.DiferencaLiderFormatado ?? "-").FontSize(7);
                    table.Cell().Background(bgColor).Padding(2).Text(r.TotalPenalidades.ToString()).FontSize(7).AlignCenter();
                }
            });
        });
    }

    private void ComporClassificacaoCategoria(IContainer container, ClassificacaoCategoriaCircuitoDto classificacao)
    {
        container.Column(column =>
        {
            column.Item().Text($"Total: {classificacao.TotalInscritos} pilotos | Finalizados: {classificacao.TotalFinalizados}").FontSize(9);

            if (!string.IsNullOrEmpty(classificacao.MelhorVoltaCategoriaFormatado))
            {
                column.Item().Text($"Melhor Volta: {classificacao.MelhorVoltaCategoriaFormatado} - {classificacao.PilotoMelhorVoltaCategoria}").FontSize(9).Bold();
            }

            column.Item().PaddingTop(10);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.ConstantColumn(40);
                    columns.RelativeColumn(4);
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(70);
                    columns.ConstantColumn(70);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Pos").Bold().FontSize(9);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Moto").Bold().FontSize(9);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Piloto").Bold().FontSize(9);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Voltas").Bold().FontSize(9);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Tempo").Bold().FontSize(9);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Diferença").Bold().FontSize(9);
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Melhor Volta").Bold().FontSize(9);
                });

                foreach (var r in classificacao.Classificacao.Where(x => x.Status == "CORRENDO" || x.Status == "FINALIZADO"))
                {
                    table.Cell().Padding(2).Text(r.PosicaoCategoria.ToString()).FontSize(9);
                    table.Cell().Padding(2).Text(r.NumeroMoto.ToString()).FontSize(9).Bold();
                    table.Cell().Padding(2).Text(r.NomePiloto).FontSize(9);
                    table.Cell().Padding(2).Text(r.VoltasCompletadas.ToString()).FontSize(9).AlignCenter();
                    table.Cell().Padding(2).Text(r.TempoTotalFormatado).FontSize(9);
                    table.Cell().Padding(2).Text(r.DiferencaLiderFormatado ?? "-").FontSize(9);
                    table.Cell().Padding(2).Text(r.MelhorVoltaFormatado ?? "-").FontSize(9);
                }
            });
        });
    }

    private void ComporResultadoPiloto(IContainer container, ResultadoPilotoCircuitoDto resultado)
    {
        container.Column(column =>
        {
            // Resumo
            column.Item().PaddingTop(10);
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Posição Geral: {resultado.PosicaoGeral}º").FontSize(14).Bold();
                    c.Item().Text($"Posição Categoria: {resultado.PosicaoCategoria}º").FontSize(12);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Voltas: {resultado.VoltasCompletadas}").FontSize(14).Bold();
                    c.Item().Text($"Tempo Total: {resultado.TempoTotalFormatado}").FontSize(12);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Melhor Volta: {resultado.MelhorVoltaFormatado ?? "-"}").FontSize(12);
                    c.Item().Text($"Média: {resultado.MediaVoltaFormatado ?? "-"}").FontSize(12);
                });
            });

            column.Item().PaddingTop(15);
            column.Item().Text("DETALHAMENTO POR VOLTA").FontSize(11).Bold();
            column.Item().PaddingTop(5);

            // Tabela de voltas
            if (resultado.Voltas != null && resultado.Voltas.Any())
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(50);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.ConstantColumn(80);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Volta").Bold().FontSize(9);
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Tempo Volta").Bold().FontSize(9);
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Acumulado").Bold().FontSize(9);
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("").Bold().FontSize(9);
                    });

                    foreach (var v in resultado.Voltas)
                    {
                        var bgColor = v.MelhorVolta ? Colors.Green.Lighten4 : Colors.White;

                        table.Cell().Background(bgColor).Padding(2).Text(v.NumeroVolta.ToString()).FontSize(9);
                        table.Cell().Background(bgColor).Padding(2).Text(v.TempoVoltaFormatado).FontSize(9);
                        table.Cell().Background(bgColor).Padding(2).Text(v.TempoAcumuladoFormatado).FontSize(9);
                        table.Cell().Background(bgColor).Padding(2).Text(v.MelhorVolta ? "⭐ Melhor" : "").FontSize(8);
                    }
                });
            }
        });
    }

    private void ComporRankingMelhorVolta(IContainer container, List<AnaliseDesempenhoDto> ranking)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(40);
                columns.ConstantColumn(50);
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.ConstantColumn(80);
                columns.ConstantColumn(80);
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Pos").Bold().FontSize(9);
                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Moto").Bold().FontSize(9);
                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Piloto").Bold().FontSize(9);
                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Categoria").Bold().FontSize(9);
                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Melhor Volta").Bold().FontSize(9);
                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Média").Bold().FontSize(9);
            });

            foreach (var r in ranking)
            {
                var bgColor = r.RankingMelhorVolta <= 3 ? Colors.Yellow.Lighten4 : Colors.White;

                table.Cell().Background(bgColor).Padding(2).Text(r.RankingMelhorVolta.ToString()).FontSize(9);
                table.Cell().Background(bgColor).Padding(2).Text(r.NumeroMoto.ToString()).FontSize(9).Bold();
                table.Cell().Background(bgColor).Padding(2).Text(r.NomePiloto).FontSize(9);
                table.Cell().Background(bgColor).Padding(2).Text(r.Categoria).FontSize(8);
                table.Cell().Background(bgColor).Padding(2).Text(r.MelhorVoltaFormatado).FontSize(9);
                table.Cell().Background(bgColor).Padding(2).Text(r.MediaFormatado).FontSize(9);
            }
        });
    }

    private void ComporRodape(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(0.5f);
            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Página ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" de ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });
            column.Item().Text("Sistema de Cronometragem - Resultado Oficial").FontSize(7).AlignCenter();
        });
    }

    #endregion
}
