using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Entities;

/// <summary>Registro de cada disparo de regra: o que foi feito, se deu erro e como desfazer.</summary>
public class ExecucaoAutomacao
{
    public int Id { get; set; }

    public int RegraAutomacaoId { get; set; }
    public RegraAutomacao RegraAutomacao { get; set; } = null!;

    public int QuadroId { get; set; }

    /// <summary>Cartão alvo; null em execuções de quadro (ex.: resumo por e-mail).</summary>
    public int? CartaoId { get; set; }
    public Cartao? Cartao { get; set; }

    public TipoGatilho Gatilho { get; set; }

    /// <summary>Identifica o disparo de gatilhos de tempo para não repetir (ex.: "prazo:2026-10-01").</summary>
    public string? ChaveDisparo { get; set; }

    public StatusExecucaoAutomacao Status { get; set; }
    public string Resumo { get; set; } = string.Empty;
    public string? Erro { get; set; }

    /// <summary>Operações inversas das alterações feitas, usadas para desfazer a execução.</summary>
    public string? DesfazerJson { get; set; }

    public int EmailsEnviados { get; set; }
    public int MensagensTeams { get; set; }
    public int UsuarioId { get; set; }
    public DateTime OcorridoEm { get; set; }
    public DateTime? DesfeitaEm { get; set; }
    public int? DesfeitaPorId { get; set; }
}
