namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Template de quadro. Ao criar um quadro novo, o usuário pode partir de um template
/// com listas, sublistas, campos e regras de automação já configurados.
/// A estrutura é serializada como JSON para facilitar cópia.
/// </summary>
public class TemplateQuadro : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    /// <summary>
    /// Estrutura completa do template (listas, campos, automações) serializada em JSON.
    /// </summary>
    public string EstruturaJson { get; set; } = "{}";
}
