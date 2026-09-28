namespace KanbanDemandas.Core.Interfaces;

/// <summary>
/// Propaga, em tempo real, que dados de quadros ou notificações de usuários mudaram após um SaveChanges.
/// </summary>
public interface IPublicadorAlteracoes
{
    Task PublicarAsync(IReadOnlyCollection<int> quadroIds, IReadOnlyCollection<int> destinatariosNotificacao);
}
