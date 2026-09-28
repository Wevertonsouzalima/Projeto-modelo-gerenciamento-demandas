using KanbanDemandas.Core.Entities;

namespace KanbanDemandas.Core.Regras;

/// <summary>
/// Uma lista que possui sublistas é apenas um agrupador: cartões só ficam em listas "folha".
/// </summary>
public static class ListasQuadro
{
    public static bool EhGrupo(int listaId, IEnumerable<Lista> todas)
        => todas.Any(l => l.ListaPaiId == listaId && !l.Excluido);

    /// <summary>Convenção do sistema: a etapa final é a lista chamada "Concluído".</summary>
    public static bool EhConcluido(int listaId, IEnumerable<Lista> todas)
        => todas.FirstOrDefault(l => l.Id == listaId)?.Nome.Equals("Concluído", StringComparison.OrdinalIgnoreCase) ?? false;

    /// <summary>Desce pela primeira sublista até chegar a uma lista que aceita cartões.</summary>
    public static int PrimeiraFolha(int listaId, IReadOnlyCollection<Lista> todas)
    {
        var primeira = todas.Where(l => l.ListaPaiId == listaId && !l.Excluido).OrderBy(l => l.Ordem).FirstOrDefault();
        return primeira is null ? listaId : PrimeiraFolha(primeira.Id, todas);
    }

    /// <summary>Nome com o caminho dos pais, ex.: "Homologação › Code Review".</summary>
    public static string NomeCompleto(Lista lista, IReadOnlyCollection<Lista> todas)
    {
        var pai = lista.ListaPaiId.HasValue ? todas.FirstOrDefault(l => l.Id == lista.ListaPaiId.Value) : null;
        return pai is null ? lista.Nome : $"{NomeCompleto(pai, todas)} › {lista.Nome}";
    }

    /// <summary>Folhas em ordem visual do quadro (pais antes, sublistas na sequência).</summary>
    public static List<Lista> FolhasEmOrdem(IReadOnlyCollection<Lista> todas)
    {
        var resultado = new List<Lista>();
        void Visitar(int? paiId)
        {
            foreach (var lista in todas.Where(l => l.ListaPaiId == paiId && !l.Excluido).OrderBy(l => l.Ordem))
            {
                if (EhGrupo(lista.Id, todas)) Visitar(lista.Id);
                else resultado.Add(lista);
            }
        }
        Visitar(null);
        return resultado;
    }
}
