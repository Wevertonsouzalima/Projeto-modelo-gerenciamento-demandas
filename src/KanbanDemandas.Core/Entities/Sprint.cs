namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Sprint do quadro. Pode conter cartões do backlog.
/// </summary>
public class Sprint : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Meta { get; set; }

    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }

    public bool Ativa { get; set; } = false;
    public bool Fechada { get; set; } = false;

    public int QuadroId { get; set; }
    public Quadro Quadro { get; set; } = null!;

    // N:N com cartões
    public ICollection<SprintCartao> Cartoes { get; set; } = [];
}
