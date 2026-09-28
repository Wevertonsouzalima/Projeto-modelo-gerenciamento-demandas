namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Armazena o valor de um campo configurável para um cartão.
/// Quando PedirNovamenteACadaEntrada=true, há múltiplos registros por (CartaoId, DefinicaoCampoId),
/// diferenciados por NumeroEntradaNaLista.
/// </summary>
public class ValorCampoCartao : EntidadeBase
{
    public int CartaoId { get; set; }
    public Cartao Cartao { get; set; } = null!;

    public int DefinicaoCampoId { get; set; }
    public DefinicaoCampo DefinicaoCampo { get; set; } = null!;

    /// <summary>
    /// Valor armazenado como string. A interpretação depende do TipoCampo da definição.
    /// Data: ISO 8601 | Número: invariant culture | Checkbox: "true"/"false"
    /// </summary>
    public string? Valor { get; set; }

    /// <summary>
    /// Número ordinal de entrada do cartão nesta lista.
    /// 1 para a primeira entrada, 2 para a segunda, etc.
    /// Derivado do HistoricoAtividade — armazenado aqui para evitar recálculo constante.
    /// </summary>
    public int NumeroEntradaNaLista { get; set; } = 1;

    public DateTime DataPreenchimento { get; set; }

    /// <summary>Movimento que preencheu este valor automaticamente (null = preenchido pelo usuário).</summary>
    public int? HistoricoOrigemId { get; set; }
    public HistoricoAtividade? HistoricoOrigem { get; set; }

    /// <summary>Verdadeiro quando o registro foi criado pelo preenchimento automático (e não só atualizado).</summary>
    public bool CriadoAutomaticamente { get; set; }

    /// <summary>Valor antes da atualização automática, usado para desfazer em caso de estorno.</summary>
    public string? ValorAnteriorAutomatico { get; set; }

    public int PreenchidoPorId { get; set; }
    public Usuario PreenchidoPor { get; set; } = null!;
}
