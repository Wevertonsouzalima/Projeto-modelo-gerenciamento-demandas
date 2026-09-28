namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Quadro Kanban. Um por time/projeto/frente de trabalho.
/// </summary>
public class Quadro : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string Cor { get; set; } = "#1565C0";

    /// <summary>Id do template de quadro origem (null = criado manualmente).</summary>
    public int? TemplateQuadroId { get; set; }

    // Navegação
    public ICollection<Lista> Listas { get; set; } = [];
    public ICollection<Sprint> Sprints { get; set; } = [];
    public ICollection<Etiqueta> Etiquetas { get; set; } = [];
    public ICollection<TemplateCartao> TemplatesCartao { get; set; } = [];
}
