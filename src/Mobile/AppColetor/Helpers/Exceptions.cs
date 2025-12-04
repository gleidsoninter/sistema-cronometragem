namespace AppColetor.Helpers
{
    /// <summary>
    /// Exceção base para erros do App Coletor
    /// </summary>
    public class ColetorException : Exception
    {
        public string Codigo { get; }
        public bool IsFatal { get; }

        public ColetorException(string codigo, string mensagem, bool isFatal = false)
            : base(mensagem)
        {
            Codigo = codigo;
            IsFatal = isFatal;
        }

        public ColetorException(string codigo, string mensagem, Exception innerException, bool isFatal = false)
            : base(mensagem, innerException)
        {
            Codigo = codigo;
            IsFatal = isFatal;
        }
    }

    /// <summary>
    /// Erro de conexão serial
    /// </summary>
    public class SerialConnectionException : ColetorException
    {
        public SerialConnectionException(string mensagem, bool isFatal = true)
            : base("SERIAL_CONNECTION", mensagem, isFatal) { }

        public SerialConnectionException(string mensagem, Exception inner)
            : base("SERIAL_CONNECTION", mensagem, inner, true) { }
    }

    /// <summary>
    /// Erro de permissão USB
    /// </summary>
    public class UsbPermissionException : ColetorException
    {
        public UsbPermissionException()
            : base("USB_PERMISSION", "Permissão USB negada pelo usuário", true) { }
    }

    /// <summary>
    /// Erro de parsing de dados
    /// </summary>
    public class ParseException : ColetorException
    {
        public string DadosRecebidos { get; }

        public ParseException(string dados, string mensagem)
            : base("PARSE_ERROR", mensagem, false)
        {
            DadosRecebidos = dados;
        }
    }

    /// <summary>
    /// Erro de validação
    /// </summary>
    public class ValidationException : ColetorException
    {
        public List<string> Erros { get; }

        public ValidationException(params string[] erros)
            : base("VALIDATION_ERROR", string.Join("; ", erros), false)
        {
            Erros = erros.ToList();
        }
    }

    /// <summary>
    /// Erro de comunicação com API
    /// </summary>
    public class ApiException : ColetorException
    {
        public int? StatusCode { get; }

        public ApiException(string mensagem, int? statusCode = null)
            : base("API_ERROR", mensagem, false)
        {
            StatusCode = statusCode;
        }
    }
}