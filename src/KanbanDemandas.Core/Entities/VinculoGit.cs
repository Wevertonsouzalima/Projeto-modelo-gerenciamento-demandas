namespace KanbanDemandas.Core.Entities;

public enum TipoVinculoGit { Branch = 1, Commit = 2, PullRequest = 3 }

public enum EstadoPullRequest { Aberto = 1, Fechado = 2, Mergeado = 3 }

/// <summary>Branch, commit ou pull request do GitHub que cita o código do cartão (ex.: KB-123).</summary>
public class VinculoGit
{
    public int Id { get; set; }

    public int CartaoId { get; set; }
    public Cartao Cartao { get; set; } = null!;

    public TipoVinculoGit Tipo { get; set; }
    public string Repositorio { get; set; } = string.Empty;

    /// <summary>Nome da branch, SHA do commit ou número do pull request.</summary>
    public string Identificador { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Autor { get; set; }
    public EstadoPullRequest? Estado { get; set; }

    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
