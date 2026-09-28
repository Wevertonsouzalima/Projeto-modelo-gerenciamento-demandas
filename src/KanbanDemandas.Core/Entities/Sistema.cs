namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Cadastro global de sistemas/produtos. Campo fixo do cartão (não parte do motor configurável).
/// </summary>
public class Sistema : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;

    // Navegação
    public ICollection<Cartao> Cartoes { get; set; } = [];
}
