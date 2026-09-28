namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Participante de uma reunião. Usado para busca por reuniões com determinado participante.
/// </summary>
public class ReuniaoParticipante
{
    public int ReuniaoId { get; set; }
    public Reuniao Reuniao { get; set; } = null!;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
}
