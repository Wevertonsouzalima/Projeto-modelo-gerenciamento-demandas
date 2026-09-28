using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Regras;

public sealed record MudancaStatusPortal(string Status, DateTime Em);

/// <summary>
/// Status que o solicitante vê no portal. Cada lista pode ter um status público; listas sem status mantêm
/// o último status público (o solicitante não enxerga movimentações internas entre colunas).
/// </summary>
public static class StatusPortal
{
    public const string StatusInicialPadrao = "Recebida";

    /// <summary>Linha do tempo de status públicos, sem repetições consecutivas. Entradas estornadas já vêm filtradas.</summary>
    public static List<MudancaStatusPortal> Historico(Cartao cartao, IEnumerable<HistoricoAtividade> entradas, IReadOnlyCollection<Lista> listas, string? statusInicial)
    {
        var resultado = new List<MudancaStatusPortal> { new(string.IsNullOrWhiteSpace(statusInicial) ? StatusInicialPadrao : statusInicial.Trim(), cartao.CriadoEm) };
        var ordenadas = entradas
            .Where(h => h.ListaDestinoId.HasValue && (h.Tipo == TipoHistoricoAtividade.CartaoMovido || h.Tipo == TipoHistoricoAtividade.CartaoCriado))
            .OrderBy(h => h.OcorridoEm).ThenBy(h => h.Id);
        foreach (var entrada in ordenadas)
        {
            var status = listas.FirstOrDefault(l => l.Id == entrada.ListaDestinoId)?.StatusPortal?.Trim();
            if (string.IsNullOrEmpty(status) || status == resultado[^1].Status) continue;
            resultado.Add(new MudancaStatusPortal(status, entrada.OcorridoEm));
        }
        return resultado;
    }

    public static string Atual(Cartao cartao, IEnumerable<HistoricoAtividade> entradas, IReadOnlyCollection<Lista> listas, string? statusInicial)
        => Historico(cartao, entradas, listas, statusInicial)[^1].Status;
}

/// <summary>Informações que o admin libera para o solicitante ver no portal (Parametrização › Portal).</summary>
[Flags]
public enum CamposPortal
{
    Nenhum = 0,
    Prazo = 1,
    DataInicio = 2,
    Prioridade = 4,
    Sistema = 8,
    Desenvolvedores = 16,
    Estimativa = 32,
    Etiquetas = 64,
    AnexosDoTime = 128,
    HistoricoStatus = 256,
    Comentarios = 512
}

public static class ConfiguracaoPortal
{
    public static CamposPortal LerCampos(string? valor)
        => (valor ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Aggregate(CamposPortal.Nenhum, (acc, nome) => Enum.TryParse<CamposPortal>(nome, true, out var campo) ? acc | campo : acc);

    public static string GravarCampos(CamposPortal campos)
        => string.Join(",", Enum.GetValues<CamposPortal>().Where(c => c != CamposPortal.Nenhum && campos.HasFlag(c)));

    public static string Nome(CamposPortal campo) => campo switch
    {
        CamposPortal.DataInicio => "Data de início",
        CamposPortal.Desenvolvedores => "Desenvolvedores responsáveis",
        CamposPortal.AnexosDoTime => "Anexos adicionados pelo time",
        CamposPortal.HistoricoStatus => "Histórico de status",
        CamposPortal.Comentarios => "Conversa (comentários públicos)",
        _ => campo.ToString()
    };

    public static List<string> LerTipos(string? valor)
        => (valor ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
