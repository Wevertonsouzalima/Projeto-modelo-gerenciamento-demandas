using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Lista (coluna) ou sublista dentro de um quadro.
/// Auto-referência ListaPaiId suporta sublistas com configuração própria.
/// </summary>
public class Lista : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }

    public int QuadroId { get; set; }
    public Quadro Quadro { get; set; } = null!;

    /// <summary>Null = lista raiz; preenchido = é sublista de ListaPaiId.</summary>
    public int? ListaPaiId { get; set; }
    public Lista? ListaPai { get; set; }

    /// <summary>
    /// Limite WIP (Work In Progress). Null = sem limite.
    /// Quando ultrapassado, exibe sinalização visual — não bloqueia.
    /// </summary>
    public int? LimiteWip { get; set; }

    /// <summary>
    /// Indica que esta lista representa o backlog do quadro.
    /// </summary>
    public bool EhBacklog { get; set; } = false;

    /// <summary>
    /// Posição da lista no caminho (fluxo) do quadro. Null = fora do caminho.
    /// Impede reordenar colunas contra o fluxo; mover cartão fora do caminho apenas gera aviso.
    /// </summary>
    public int? OrdemFluxo { get; set; }

    /// <summary>Campos fixos do cartão que esta etapa exige (ex.: desenvolvedor atribuído em "A Fazer").</summary>
    public CamposFixosCartao CamposFixosExigidos { get; set; } = CamposFixosCartao.Nenhum;

    /// <summary>
    /// Campos fixos que o cartão não pode ter nesta etapa (ex.: desenvolvedor e prazo no Backlog).
    /// Ficam desabilitados na edição, as automações não os preenchem e são limpos quando o cartão entra na lista.
    /// </summary>
    public CamposFixosCartao CamposFixosBloqueados { get; set; } = CamposFixosCartao.Nenhum;

    /// <summary>
    /// Quando verdadeiro, o cartão só entra na lista depois de preencher o que a etapa exige.
    /// Quando falso (padrão), entra e fica sinalizado como pendente.
    /// </summary>
    public bool BloquearEntradaComPendencias { get; set; } = false;

    /// <summary>Status mostrado ao solicitante no portal quando o cartão está nesta lista (null = mantém o status anterior).</summary>
    public string? StatusPortal { get; set; }

    // Navegação
    public ICollection<Lista> Sublistas { get; set; } = [];
    public ICollection<Cartao> Cartoes { get; set; } = [];
    public ICollection<DefinicaoCampo> DefinicoesCampo { get; set; } = [];
    public ICollection<RegraAutomacao> RegrasAutomacao { get; set; } = [];
}
