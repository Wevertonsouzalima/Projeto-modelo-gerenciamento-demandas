namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Comentário na linha do tempo de um cartão ou item de tarefa.
/// Suporta menção @usuario no texto.
/// </summary>
public class Comentario : EntidadeBase
{
    public string Texto { get; set; } = string.Empty;

    public int AutorId { get; set; }
    public Usuario Autor { get; set; } = null!;

    public DateTime DataHora { get; set; }

    /// <summary>Marcado como verdadeiro quando o texto foi editado após criação.</summary>
    public bool Editado { get; set; } = false;

    /// <summary>Visível ao solicitante no portal. Comentários internos do time ficam ocultos.</summary>
    public bool Publico { get; set; }

    // Pertence a um cartão OU a um item de tarefa (um dos dois null)
    public int? CartaoId { get; set; }
    public Cartao? Cartao { get; set; }

    public int? ItemTarefaId { get; set; }
    public ItemTarefa? ItemTarefa { get; set; }
}
