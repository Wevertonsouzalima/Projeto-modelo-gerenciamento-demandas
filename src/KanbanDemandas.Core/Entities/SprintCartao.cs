namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Tabela de junção N:N entre Sprint e Cartão.
/// </summary>
public class SprintCartao
{
    public int SprintId { get; set; }
    public Sprint Sprint { get; set; } = null!;

    public int CartaoId { get; set; }
    public Cartao Cartao { get; set; } = null!;

    /// <summary>Data em que o cartão foi incluído na sprint.</summary>
    public DateTime AdicionadoEm { get; set; }
}
