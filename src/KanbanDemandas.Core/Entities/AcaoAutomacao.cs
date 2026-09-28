using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Entities;

/// <summary>Ação de uma regra de automação, executada na ordem definida. Parâmetros em JSON conforme o tipo.</summary>
public class AcaoAutomacao : EntidadeBase
{
    public int RegraAutomacaoId { get; set; }
    public RegraAutomacao RegraAutomacao { get; set; } = null!;

    public TipoAcaoAutomacao Tipo { get; set; }
    public int Ordem { get; set; }
    public string? ParametrosJson { get; set; }
}
