namespace DevNationCrono.API.Models.DTOs
{
    /// <summary>
    /// Resultado processado e salvo na tabela resultados (após "Processar Resultado")
    /// </summary>
    public class ResultadoProcessadoDto
    {
        public int Id { get; set; }
        public int IdEtapa { get; set; }
        public string NomeEtapa { get; set; } = string.Empty;
        public int IdCategoria { get; set; }
        public string NomeCategoria { get; set; } = string.Empty;

        // Piloto
        public int IdInscricao { get; set; }
        public int IdPiloto { get; set; }
        public string NomePiloto { get; set; } = string.Empty;
        public int NumeroMoto { get; set; }

        // Resultado
        public int? Posicao { get; set; }
        public int? PosicaoGeral { get; set; }
        public int TotalVoltas { get; set; }
        public long? TempoTotal { get; set; }
        public long? MelhorVolta { get; set; }

        // Diferenças
        public long? DiferencaLider { get; set; }
        public int? VoltasAtras { get; set; }

        // Status e pontuação
        public string Status { get; set; } = "CLASSIFICADO";
        public string? MotivoStatus { get; set; }
        public decimal PontosObtidos { get; set; }

        // Penalidades
        public int PenalidadeSegundos { get; set; }
        public string? MotivoPenalidade { get; set; }

        // Controle
        public bool Homologado { get; set; }
        public DateTime? ProcessadoEm { get; set; }

        // Formatação
        public string TempoTotalFormatado => FormatarTempo(TempoTotal);
        public string MelhorVoltaFormatada => FormatarTempo(MelhorVolta);

        private static string FormatarTempo(long? ms)
        {
            if (!ms.HasValue) return "-";
            var ts = TimeSpan.FromMilliseconds(ms.Value);
            return ts.Hours > 0
                ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}"
                : $"{ts.Minutes}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        }
    }

    // ================================================================
    // PROCESSAMENTO DE RESULTADO
    // ================================================================

    public class ProcessarResultadoDto
    {
        public int IdEtapa { get; set; }
        public int? IdCategoria { get; set; } // null = todas as categorias da etapa
        public bool AplicarPontuacao { get; set; } = false;
        public bool Homologar { get; set; } = false;
    }

    public class ProcessamentoResultadoResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public int TotalProcessados { get; set; }
        public int TotalCategorias { get; set; }
        public List<string> Erros { get; set; } = new();
        public DateTime ProcessadoEm { get; set; }
    }
}
