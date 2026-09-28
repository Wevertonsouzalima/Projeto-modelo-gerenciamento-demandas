using KanbanDemandas.Core.Entities;

namespace KanbanDemandas.Web.Services.Automacoes;

/// <summary>
/// Dois cartões conflitam quando compartilham algum desenvolvedor e os intervalos [início, prazo] se sobrepõem.
/// Cartão sem início conta como um dia (o do prazo). Cartões concluídos não conflitam.
/// </summary>
public static class ConflitosAgenda
{
    public static (DateTime Inicio, DateTime Fim)? Intervalo(Cartao cartao)
    {
        if (!cartao.Prazo.HasValue) return null;
        var fim = cartao.Prazo.Value.ToLocalTime().Date;
        var inicio = (cartao.DataInicio ?? cartao.Prazo.Value).ToLocalTime().Date;
        return (inicio <= fim ? inicio : fim, fim);
    }

    public static bool Sobrepoe((DateTime Inicio, DateTime Fim) a, (DateTime Inicio, DateTime Fim) b)
        => a.Inicio <= b.Fim && b.Inicio <= a.Fim;

    /// <summary>Cartões que conflitam com <paramref name="cartao"/>. Requer Desenvolvedores carregados.</summary>
    public static List<Cartao> Encontrar(Cartao cartao, IEnumerable<Cartao> outros, Func<Cartao, bool>? estaConcluido = null)
    {
        var intervalo = Intervalo(cartao);
        if (intervalo is null || cartao.Desenvolvedores.Count == 0) return [];
        if (estaConcluido?.Invoke(cartao) == true) return [];
        var devs = cartao.Desenvolvedores.Select(d => d.UsuarioId).ToHashSet();
        return outros
            .Where(o => o.Id != cartao.Id && !o.Excluido && estaConcluido?.Invoke(o) != true)
            .Where(o => devs.Overlaps(o.Desenvolvedores.Select(d => d.UsuarioId)))
            .Where(o => Intervalo(o) is { } outro && Sobrepoe(intervalo.Value, outro))
            .ToList();
    }
}
