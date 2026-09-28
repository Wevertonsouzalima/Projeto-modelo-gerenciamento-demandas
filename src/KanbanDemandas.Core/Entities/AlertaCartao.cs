using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Entities;

/// <summary>Alerta visível no cartão, criado por automação, até alguém (ou outra regra) resolvê-lo.</summary>
public class AlertaCartao
{
    public int Id { get; set; }

    public int CartaoId { get; set; }
    public Cartao Cartao { get; set; } = null!;

    public int? RegraAutomacaoId { get; set; }
    public RegraAutomacao? RegraAutomacao { get; set; }

    public string Mensagem { get; set; } = string.Empty;
    public SeveridadeAlerta Severidade { get; set; } = SeveridadeAlerta.Aviso;

    public bool Resolvido { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? ResolvidoEm { get; set; }
    public int? ResolvidoPorId { get; set; }
}
