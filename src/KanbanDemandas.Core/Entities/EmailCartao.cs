namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Registro de e-mail enviado a partir de um cartão.
/// Aparece na linha do tempo junto com comentários e reuniões.
/// </summary>
public class EmailCartao : EntidadeBase
{
    public int CartaoId { get; set; }
    public Cartao Cartao { get; set; } = null!;

    public string Para { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string Assunto { get; set; } = string.Empty;

    /// <summary>Corpo do e-mail em HTML sanitizado.</summary>
    public string CorpoHtml { get; set; } = string.Empty;

    public DateTime EnviadoEm { get; set; }

    public int EnviadoPorId { get; set; }
    public Usuario EnviadoPor { get; set; } = null!;

    public bool Enviado { get; set; } = false;
    public string? ErroEnvio { get; set; }
}
