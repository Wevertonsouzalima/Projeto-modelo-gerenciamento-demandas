namespace KanbanDemandas.Web.Services.Automacoes;

/// <summary>Contas com dias úteis: segunda a sexta, descontando os feriados informados (datas sem hora).</summary>
public static class DiasUteis
{
    public static bool EhUtil(DateTime data, IReadOnlySet<DateTime>? feriados = null)
        => data.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && feriados?.Contains(data.Date) != true;

    public static DateTime ProximoUtil(DateTime data, IReadOnlySet<DateTime>? feriados = null)
    {
        var dia = data.Date;
        while (!EhUtil(dia, feriados)) dia = dia.AddDays(1);
        return dia;
    }

    /// <summary>Soma (ou subtrai) dias úteis; 0 devolve o próprio dia (ajustado para dia útil).</summary>
    public static DateTime Adicionar(DateTime data, int dias, IReadOnlySet<DateTime>? feriados = null)
    {
        var dia = data.Date;
        var passo = dias >= 0 ? 1 : -1;
        var restantes = Math.Abs(dias);
        while (restantes > 0)
        {
            dia = dia.AddDays(passo);
            if (EhUtil(dia, feriados)) restantes--;
        }
        return EhUtil(dia, feriados) ? dia : ProximoUtil(dia, feriados);
    }

    /// <summary>Quantidade de dias úteis no intervalo fechado [inicio, fim]; mínimo 1.</summary>
    public static int Contar(DateTime inicio, DateTime fim, IReadOnlySet<DateTime>? feriados = null)
    {
        if (fim.Date < inicio.Date) return 1;
        var total = 0;
        for (var dia = inicio.Date; dia <= fim.Date; dia = dia.AddDays(1))
            if (EhUtil(dia, feriados)) total++;
        return Math.Max(1, total);
    }
}
