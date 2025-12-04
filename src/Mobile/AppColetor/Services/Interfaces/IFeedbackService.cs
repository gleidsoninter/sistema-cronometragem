namespace AppColetor.Services.Interfaces
{
    public interface IFeedbackService
    {
        Task LeituraRecebidaAsync();
        Task LeituraSincronizadaAsync();
        Task ErroAsync();
        Task ConexaoEstabelecidaAsync();
        Task ConexaoPerdidaAsync();
    }
}