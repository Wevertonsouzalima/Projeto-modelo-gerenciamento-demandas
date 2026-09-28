namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Template de cartão associado a um quadro.
/// Ao criar novo cartão, o usuário pode escolher entre branco ou a partir de um template.
/// </summary>
public class TemplateCartao : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? TitulopadraO { get; set; }
    public string? DescricaoPadrao { get; set; }

    public int QuadroId { get; set; }
    public Quadro Quadro { get; set; } = null!;

    /// <summary>Itens de tarefa padrão, serializados como JSON.</summary>
    public string? ItensTarefaPadrao { get; set; }

    /// <summary>Campos pré-preenchidos, serializados como JSON (dicionário nome→valor).</summary>
    public string? CamposPadrao { get; set; }
}
