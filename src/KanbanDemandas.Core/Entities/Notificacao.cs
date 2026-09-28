using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Notificação in-app para um usuário. Agrega: atribuição, menção, alerta de campo, conflito, bloqueio.
/// </summary>
public class Notificacao : EntidadeBase
{
    public int DestinatarioId { get; set; }
    public Usuario Destinatario { get; set; } = null!;

    public TipoNotificacao Tipo { get; set; }

    public string Mensagem { get; set; } = string.Empty;

    public bool Lida { get; set; } = false;
    public DateTime? LidaEm { get; set; }

    /// <summary>Cartão de origem para link direto.</summary>
    public int? CartaoOrigemId { get; set; }
    public Cartao? CartaoOrigem { get; set; }
}
