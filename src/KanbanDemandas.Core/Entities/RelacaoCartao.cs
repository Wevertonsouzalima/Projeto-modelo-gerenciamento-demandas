using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Vínculo horizontal entre dois cartões (não hierárquico).
/// Tipos: BloqueadoPor, Bloqueia, RelacionadoA, DuplicadoDe.
/// </summary>
public class RelacaoCartao : EntidadeBase
{
    public int CartaoOrigemId { get; set; }
    public Cartao CartaoOrigem { get; set; } = null!;

    public int CartaoDestinoId { get; set; }
    public Cartao CartaoDestino { get; set; } = null!;

    public TipoRelacaoCartao Tipo { get; set; }
}
