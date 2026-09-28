using Microsoft.AspNetCore.SignalR;

namespace KanbanDemandas.Web.Hubs;

public sealed class QuadroHub(KanbanDemandas.Web.Services.PublicadorAlteracoesTempoReal publicador) : Hub
{
    /// <summary>
    /// Usado pelo portal de solicitações (processo separado) para que quadros abertos e sinos de notificação
    /// deste sistema atualizem na hora. Só dispara recarga; nenhum dado trafega por aqui.
    /// </summary>
    public Task AvisarAlteracao(int[] quadroIds, int[] destinatariosNotificacao)
        => publicador.PublicarAsync(quadroIds.Distinct().Take(50).ToList(), destinatariosNotificacao.Distinct().Take(500).ToList());

    public Task EntrarNoQuadro(int quadroId)
        => Groups.AddToGroupAsync(Context.ConnectionId, Grupo(quadroId));

    public Task SairDoQuadro(int quadroId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, Grupo(quadroId));

    public static string Grupo(int quadroId) => $"quadro:{quadroId}";
}
