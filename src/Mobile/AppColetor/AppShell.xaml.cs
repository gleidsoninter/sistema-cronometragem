namespace MotoTimingApp.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private void RegisterRoutes()
    {
        // Rotas de autenticação
        Routing.RegisterRoute("confirmar-email", typeof(Views.Pages.ConfirmarEmailPage));

        // Rotas internas - Eventos
        Routing.RegisterRoute("evento-detalhe", typeof(Views.Pages.EventoDetalhePage));
        Routing.RegisterRoute("inscricao", typeof(Views.Pages.InscricaoPage));
        Routing.RegisterRoute("pagamento", typeof(Views.Pages.PagamentoPage));
        Routing.RegisterRoute("pagamento-confirmado", typeof(Views.Pages.PagamentoConfirmadoPage));

        // Rotas internas - Resultados
        Routing.RegisterRoute("resultado-detalhe", typeof(Views.Pages.ResultadoDetalhePage));
        Routing.RegisterRoute("piloto-tempos", typeof(Views.Pages.PilotoTemposPage));

        // Rotas internas - Perfil
        Routing.RegisterRoute("editar-perfil", typeof(Views.Pages.EditarPerfilPage));
        Routing.RegisterRoute("minhas-inscricoes", typeof(Views.Pages.MinhasInscricoesPage));
        Routing.RegisterRoute("configuracoes", typeof(Views.Pages.ConfiguracoesPage));
        Routing.RegisterRoute("sobre", typeof(Views.Pages.SobrePage));

        // Rotas internas - Geral
        Routing.RegisterRoute("notificacoes", typeof(Views.Pages.NotificacoesPage));
    }
}