using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Define um campo configurável para uma lista/sublista.
/// Criado pelo usuário em tempo de uso — não fixo no código.
/// </summary>
public class DefinicaoCampo : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public TipoCampo Tipo { get; set; }

    public int ListaId { get; set; }
    public Lista Lista { get; set; } = null!;

    /// <summary>
    /// Se verdadeiro, gera badge de alerta no cartão quando não preenchido.
    /// Não bloqueia a movimentação do cartão.
    /// </summary>
    public bool Obrigatorio { get; set; } = false;

    /// <summary>
    /// Quando verdadeiro: a cada nova entrada do cartão na lista, pede o valor novamente
    /// sem sobrescrever o anterior — cria novo ValorCampoCartao com NumeroEntradaNaLista++.
    /// Quando falso: valor único por cartão, persiste entre entradas.
    /// </summary>
    public bool PedirNovamenteACadaEntrada { get; set; } = false;

    /// <summary>
    /// Opções disponíveis para campos do tipo Selecao (separadas por |).
    /// </summary>
    public string? OpcoesSelecao { get; set; }

    public int Ordem { get; set; }

    /// <summary>Manual ou preenchido automaticamente quando o cartão entra na lista.</summary>
    public PreenchimentoAutomatico Preenchimento { get; set; } = PreenchimentoAutomatico.Manual;

    /// <summary>Para campos automáticos: o que fazer quando o cartão volta a entrar na lista.</summary>
    public RegraReentrada RegraReentrada { get; set; } = RegraReentrada.ManterPrimeiro;

    /// <summary>Campo de data cujo valor também passa a ser a Data de início do cartão (conflitos, filtros e indicadores).</summary>
    public bool DefineDataInicioCartao { get; set; } = false;

    // Navegação
    public ICollection<ValorCampoCartao> Valores { get; set; } = [];

    /// <summary>Retorna as opções de seleção como lista.</summary>
    public List<string> ObterOpcoesSelecao()
        => string.IsNullOrWhiteSpace(OpcoesSelecao)
            ? []
            : [.. OpcoesSelecao.Split('|', StringSplitOptions.RemoveEmptyEntries)];
}
