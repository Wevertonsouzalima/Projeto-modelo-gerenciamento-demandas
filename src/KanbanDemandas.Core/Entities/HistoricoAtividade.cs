using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Trilha de auditoria de um cartão. Base para cálculo de lead time, badges,
/// número de entrada em lista e indicadores do dashboard.
/// </summary>
public class HistoricoAtividade
{
    public int Id { get; set; }

    public int CartaoId { get; set; }
    public Cartao Cartao { get; set; } = null!;

    public TipoHistoricoAtividade Tipo { get; set; }

    /// <summary>Lista de origem (para CartaoMovido/CartaoSaiu).</summary>
    public int? ListaOrigemId { get; set; }
    public Lista? ListaOrigem { get; set; }

    /// <summary>Lista de destino (para CartaoMovido/CartaoEntrou).</summary>
    public int? ListaDestinoId { get; set; }
    public Lista? ListaDestino { get; set; }

    /// <summary>Descrição legível da atividade.</summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>Valor anterior (para CampoAlterado).</summary>
    public string? ValorAnterior { get; set; }

    /// <summary>Valor novo (para CampoAlterado).</summary>
    public string? ValorNovo { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public DateTime OcorridoEm { get; set; }

    /// <summary>
    /// Movimento desfeito logo em seguida (cartão voltou para a lista de origem dentro da janela de correção).
    /// Fica gravado para auditoria, mas é ignorado por métricas, linha do tempo e contagem de entradas.
    /// </summary>
    public bool Estornado { get; set; }

    /// <summary>Indica se este movimento alterou a Data de início do cartão (para desfazer no estorno).</summary>
    public bool AlterouDataInicio { get; set; }

    public DateTime? DataInicioAnterior { get; set; }
}
