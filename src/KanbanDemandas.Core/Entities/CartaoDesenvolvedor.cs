namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Tabela de junção N:N entre Cartão e Desenvolvedor (Usuário).
/// Um desenvolvedor pode ser marcado como principal para notificações/e-mail padrão.
/// </summary>
public class CartaoDesenvolvedor
{
    public int CartaoId { get; set; }
    public Cartao Cartao { get; set; } = null!;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    /// <summary>
    /// Desenvolvedor principal do cartão (destinatário padrão de notificações).
    /// </summary>
    public bool Principal { get; set; } = false;
}
