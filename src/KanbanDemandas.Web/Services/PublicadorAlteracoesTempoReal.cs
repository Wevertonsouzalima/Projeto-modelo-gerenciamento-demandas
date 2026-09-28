using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace KanbanDemandas.Web.Services;

/// <summary>
/// Quadros: avisa o grupo SignalR de cada quadro afetado. Notificações: dispara um evento em memória,
/// já que no Blazor Server todos os circuitos (e o sino do layout) vivem neste mesmo processo.
/// </summary>
public sealed class PublicadorAlteracoesTempoReal(IHubContext<QuadroHub> hubContext) : IPublicadorAlteracoes
{
    public event Action<IReadOnlyCollection<int>>? NotificacoesAlteradas;

    public async Task PublicarAsync(IReadOnlyCollection<int> quadroIds, IReadOnlyCollection<int> destinatariosNotificacao)
    {
        foreach (var quadroId in quadroIds)
            await hubContext.Clients.Group(QuadroHub.Grupo(quadroId)).SendAsync("QuadroAtualizado");

        if (destinatariosNotificacao.Count > 0)
            NotificacoesAlteradas?.Invoke(destinatariosNotificacao);
    }
}
