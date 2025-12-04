using AppColetor.Models.Entities;

namespace AppColetor.Services.Interfaces
{
    public interface IParserService
    {
        /// <summary>
        /// Parseia dados brutos em uma leitura
        /// </summary>
        LeituraParseResult Parsear(string dados, string protocolo);

        /// <summary>
        /// Valida uma leitura
        /// </summary>
        ValidationResult Validar(Leitura leitura);
    }

    public class LeituraParseResult
    {
        public bool Sucesso { get; set; }
        public Leitura? Leitura { get; set; }
        public string? Erro { get; set; }

        public static LeituraParseResult Ok(Leitura leitura) => new() { Sucesso = true, Leitura = leitura };
        public static LeituraParseResult Falha(string erro) => new() { Sucesso = false, Erro = erro };
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Erros { get; set; } = new();

        public static ValidationResult Ok() => new() { IsValid = true };
        public static ValidationResult Falha(params string[] erros) => new() { IsValid = false, Erros = erros.ToList() };
    }
}