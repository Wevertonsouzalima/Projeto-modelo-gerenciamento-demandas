namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Item de tarefa (checklist evoluído) de um cartão.
/// Dois modos: simples (título + checkbox) ou detalhado (+ descrição, comentários, reuniões).
/// Pode ser promovido a cartão filho via CartaoPromovidoId.
/// </summary>
public class ItemTarefa : EntidadeBase
{
    public string Titulo { get; set; } = string.Empty;
    public bool Concluido { get; set; } = false;

    public int CartaoId { get; set; }
    public Cartao Cartao { get; set; } = null!;

    public int Ordem { get; set; }

    // Modo detalhado
    public string? Descricao { get; set; }

    /// <summary>
    /// Desenvolvedor responsável por este item — restrito aos devs já vinculados ao cartão pai.
    /// </summary>
    public int? DesenvolvedorId { get; set; }
    public Usuario? Desenvolvedor { get; set; }

    /// <summary>
    /// Quando promovido a cartão filho, aponta para o novo cartão criado.
    /// O progresso do item passa a refletir o status do cartão filho.
    /// </summary>
    public int? CartaoPromovidoId { get; set; }
    public Cartao? CartaoPromovido { get; set; }

    // Modo detalhado: comentários e reuniões próprios
    public ICollection<Comentario> Comentarios { get; set; } = [];
    public ICollection<Reuniao> Reunioes { get; set; } = [];
}
