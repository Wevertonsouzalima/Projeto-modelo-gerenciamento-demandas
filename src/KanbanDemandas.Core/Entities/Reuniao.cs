namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Registro estruturado de reunião vinculado a um cartão ou item de tarefa.
/// Visualmente diferenciado de comentários na linha do tempo.
/// </summary>
public class Reuniao : EntidadeBase
{
    public DateTime Data { get; set; }

    /// <summary>Ata/decisões da reunião (texto livre).</summary>
    public string Ata { get; set; } = string.Empty;

    public int AutorId { get; set; }
    public Usuario Autor { get; set; } = null!;

    // Pertence a um cartão OU a um item de tarefa
    public int? CartaoId { get; set; }
    public Cartao? Cartao { get; set; }

    public int? ItemTarefaId { get; set; }
    public ItemTarefa? ItemTarefa { get; set; }

    // Participantes
    public ICollection<ReuniaoParticipante> Participantes { get; set; } = [];
}
