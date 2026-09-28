using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Evento de automação aguardando o tempo de espera. Cada nova alteração no mesmo cartão adia a execução,
/// então as regras só rodam depois que o cartão "assenta" — um vai-e-volta dentro da espera não dispara nada.
/// </summary>
public class EventoAutomacaoPendente
{
    public int Id { get; set; }
    public int CartaoId { get; set; }
    public TipoGatilho Gatilho { get; set; }
    public int? ListaId { get; set; }
    public CampoMonitorado Campo { get; set; }
    public int? UsuarioAlvoId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime ExecutarEm { get; set; }
}
