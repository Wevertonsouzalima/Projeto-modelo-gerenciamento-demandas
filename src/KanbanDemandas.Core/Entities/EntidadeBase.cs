namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Classe base com auditoria automática e soft-delete para todas as entidades principais.
/// </summary>
public abstract class EntidadeBase
{
    public int Id { get; set; }

    // Auditoria de criação
    public DateTime CriadoEm { get; set; }
    public int CriadoPorId { get; set; }

    // Auditoria de alteração
    public DateTime? AlteradoEm { get; set; }
    public int? AlteradoPorId { get; set; }

    // Soft-delete
    public bool Excluido { get; set; }
    public DateTime? ExcluidoEm { get; set; }
    public int? ExcluidoPorId { get; set; }
}
