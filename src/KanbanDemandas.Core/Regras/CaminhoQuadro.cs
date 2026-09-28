using KanbanDemandas.Core.Entities;

namespace KanbanDemandas.Core.Regras;

/// <summary>
/// Regras do caminho (fluxo) do quadro. Uma lista-pai assume a menor posição entre ela e suas sublistas,
/// para que um grupo (ex.: Homologação) seja ordenado pela primeira etapa que contém.
/// </summary>
public static class CaminhoQuadro
{
    public static int? FluxoEfetivo(int listaId, IReadOnlyCollection<Lista> todas)
    {
        var menor = todas.FirstOrDefault(l => l.Id == listaId)?.OrdemFluxo;
        foreach (var filha in todas.Where(l => l.ListaPaiId == listaId))
        {
            var fluxo = FluxoEfetivo(filha.Id, todas);
            if (fluxo.HasValue && (!menor.HasValue || fluxo < menor)) menor = fluxo;
        }
        return menor;
    }

    public static bool RespeitaCaminho(IEnumerable<int> ordemIrmas, IReadOnlyCollection<Lista> todas)
    {
        int? anterior = null;
        foreach (var id in ordemIrmas)
        {
            var fluxo = FluxoEfetivo(id, todas);
            if (!fluxo.HasValue) continue;
            if (anterior.HasValue && fluxo < anterior) return false;
            anterior = fluxo;
        }
        return true;
    }

    /// <summary>Reposiciona as listas do caminho na ordem do fluxo, mantendo as demais onde estão.</summary>
    public static List<int> OrdenarPeloCaminho(IReadOnlyList<int> ordemIrmas, IReadOnlyCollection<Lista> todas)
    {
        var fila = new Queue<int>(ordemIrmas
            .Where(id => FluxoEfetivo(id, todas).HasValue)
            .OrderBy(id => FluxoEfetivo(id, todas)));
        return ordemIrmas.Select(id => FluxoEfetivo(id, todas).HasValue ? fila.Dequeue() : id).ToList();
    }

    /// <summary>Aviso quando o cartão vai para uma lista que não é a próxima etapa do caminho; null se estiver ok.</summary>
    public static string? AvisoMovimentacao(int origemId, int destinoId, IReadOnlyCollection<Lista> todas)
    {
        var origem = todas.FirstOrDefault(l => l.Id == origemId);
        var destino = todas.FirstOrDefault(l => l.Id == destinoId);
        if (origem is null || destino is null || origemId == destinoId) return null;

        var caminho = todas.Where(l => l.OrdemFluxo.HasValue).OrderBy(l => l.OrdemFluxo).ToList();
        if (caminho.Count == 0 || !origem.OrdemFluxo.HasValue) return null;

        if (!destino.OrdemFluxo.HasValue)
            return $"'{destino.Nome}' está fora do caminho configurado.";

        var proxima = caminho.FirstOrDefault(l => l.OrdemFluxo > origem.OrdemFluxo);
        if (proxima?.Id == destino.Id) return null;
        if (proxima is null) return $"Fora do caminho: '{origem.Nome}' é a última etapa.";
        return $"Fora do caminho: a próxima etapa depois de '{origem.Nome}' é '{proxima.Nome}'.";
    }
}
