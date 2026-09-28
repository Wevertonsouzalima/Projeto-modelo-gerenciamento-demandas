using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Entities;

/// <summary>Alteração proposta por uma automação em modo "sugerir", aguardando aprovação de uma pessoa.</summary>
public class SugestaoAutomacao
{
    public int Id { get; set; }

    public int RegraAutomacaoId { get; set; }
    public RegraAutomacao RegraAutomacao { get; set; } = null!;

    public int QuadroId { get; set; }
    public int CartaoId { get; set; }
    public Cartao Cartao { get; set; } = null!;

    public string Descricao { get; set; } = string.Empty;

    /// <summary>O que será aplicado ao aprovar (datas novas ou lista de destino).</summary>
    public string PropostaJson { get; set; } = string.Empty;

    public StatusSugestao Status { get; set; } = StatusSugestao.Pendente;
    public DateTime CriadoEm { get; set; }
    public DateTime? DecididoEm { get; set; }
    public int? DecididoPorId { get; set; }
}
