namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Usuário do sistema. Sem autenticação real na v1 — plugável depois.
/// </summary>
public class Usuario : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;

    /// <summary>
    /// Receber e-mail ao ser mencionado (configurável por usuário).
    /// </summary>
    public bool NotificarPorEmail { get; set; } = false;

    /// <summary>Permite que regras de automação enviem e-mail para este usuário.</summary>
    public bool ReceberEmailsAutomacao { get; set; } = true;

    // Navegação
    public ICollection<CartaoDesenvolvedor> CartoesDesenvolvedor { get; set; } = [];
    public ICollection<Notificacao> Notificacoes { get; set; } = [];
}
