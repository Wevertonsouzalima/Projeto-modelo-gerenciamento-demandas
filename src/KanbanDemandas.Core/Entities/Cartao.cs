using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Cartão Kanban. Entidade central do sistema.
/// </summary>
public class Cartao : EntidadeBase
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    public int ListaId { get; set; }

    /// <summary>Cartão aberto pelo solicitante no portal de solicitações.</summary>
    public bool OrigemPortal { get; set; }

    /// <summary>Tipo escolhido no portal (ex.: Melhoria, Erro, Dúvida).</summary>
    public string? TipoSolicitacao { get; set; }
    public Lista Lista { get; set; } = null!;

    public int Ordem { get; set; }

    // Datas e prazo
    public DateTime? DataInicio { get; set; }
    public DateTime? Prazo { get; set; }

    // Estimativa em horas/pontos (unidade definida pelo time)
    public decimal? Estimativa { get; set; }

    public Prioridade? Prioridade { get; set; }

    // Papéis fixos
    public int? SistemaId { get; set; }
    public Sistema? Sistema { get; set; }

    public int? SolicitanteId { get; set; }
    public Usuario? Solicitante { get; set; }

    // Hierarquia pai/filho
    public int? CartaoPaiId { get; set; }
    public Cartao? CartaoPai { get; set; }

    // Navegação
    public ICollection<Cartao> CartosFilhos { get; set; } = [];
    public ICollection<CartaoDesenvolvedor> Desenvolvedores { get; set; } = [];
    public ICollection<CartaoEtiqueta> Etiquetas { get; set; } = [];
    public ICollection<ItemTarefa> ItensTarefa { get; set; } = [];
    public ICollection<ValorCampoCartao> ValoresCampo { get; set; } = [];
    public ICollection<Comentario> Comentarios { get; set; } = [];
    public ICollection<Reuniao> Reunioes { get; set; } = [];
    public ICollection<Anexo> Anexos { get; set; } = [];
    public ICollection<HistoricoAtividade> Historico { get; set; } = [];
    public ICollection<RelacaoCartao> RelacoesOrigem { get; set; } = [];
    public ICollection<RelacaoCartao> RelacoesDestino { get; set; } = [];
    public ICollection<SprintCartao> Sprints { get; set; } = [];
    public ICollection<EmailCartao> Emails { get; set; } = [];
    public ICollection<AlertaCartao> Alertas { get; set; } = [];
    public ICollection<VinculoGit> VinculosGit { get; set; } = [];
}
