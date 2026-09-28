using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Regra de automação de um quadro: QUANDO (gatilho) + SE (condições) → ENTÃO (ações).
/// Parâmetros do gatilho e condições ficam em JSON para permitir tipos variados sem uma tabela por tipo.
/// </summary>
public class RegraAutomacao : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Ativa { get; set; } = true;

    public int QuadroId { get; set; }
    public Quadro Quadro { get; set; } = null!;

    public TipoGatilho Gatilho { get; set; } = TipoGatilho.CartaoEntrouNaLista;

    /// <summary>Lista observada pelo gatilho (entrar/sair/parado). Null = qualquer lista.</summary>
    public int? ListaId { get; set; }
    public Lista? Lista { get; set; }

    /// <summary>Demais parâmetros do gatilho (dias, horário, campo monitorado...).</summary>
    public string? ParametrosGatilhoJson { get; set; }

    public string? CondicoesJson { get; set; }

    /// <summary>Verdadeiro = todas as condições (E); falso = qualquer uma (OU).</summary>
    public bool ExigirTodasCondicoes { get; set; } = true;

    /// <summary>Gatilhos de tempo só executam em dias úteis dentro do horário comercial configurado.</summary>
    public bool SomenteHorarioComercial { get; set; }

    public DateTime? UltimaExecucaoEm { get; set; }

    public ICollection<AcaoAutomacao> Acoes { get; set; } = [];
}
