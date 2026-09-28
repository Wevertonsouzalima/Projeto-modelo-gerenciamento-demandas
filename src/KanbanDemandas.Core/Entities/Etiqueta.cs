namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Etiqueta colorida aplicável a cartões (N:N).
/// </summary>
public class Etiqueta : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;

    /// <summary>Cor em formato hex, ex.: #FF5733</summary>
    public string Cor { get; set; } = "#607D8B";

    public int QuadroId { get; set; }
    public Quadro Quadro { get; set; } = null!;

    // Navegação N:N
    public ICollection<CartaoEtiqueta> CartaoEtiquetas { get; set; } = [];
}
