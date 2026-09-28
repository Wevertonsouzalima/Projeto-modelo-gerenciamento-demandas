namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Tabela de junção N:N entre Cartão e Etiqueta.
/// </summary>
public class CartaoEtiqueta
{
    public int CartaoId { get; set; }
    public Cartao Cartao { get; set; } = null!;

    public int EtiquetaId { get; set; }
    public Etiqueta Etiqueta { get; set; } = null!;
}
