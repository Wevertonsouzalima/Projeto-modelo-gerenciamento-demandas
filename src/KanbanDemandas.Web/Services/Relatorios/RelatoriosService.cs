using KanbanDemandas.Core.Regras;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KanbanDemandas.Web.Services.Relatorios;

public sealed record FiltroRelatorio(int QuadroId, DateTime De, DateTime Ate, int? SistemaId, int? DesenvolvedorId);

public sealed record LinhaFluxo(DateTime Dia, IReadOnlyDictionary<string, int> PorLista);
public sealed record LinhaVazao(DateTime Semana, int Cartoes, decimal Pontos);
public sealed record LinhaCiclo(int CartaoId, string Titulo, string Sistema, string Desenvolvedores, DateTime CriadoEm, DateTime? IniciadoEm, DateTime ConcluidoEm, double LeadTimeDias, double? CicloDias);
public sealed record LinhaEnvelhecimento(int CartaoId, string Titulo, string Lista, string Desenvolvedores, int DiasNaLista, int IdadeDias, DateTime? Prazo, bool Atrasado);
public sealed record LinhaPrazo(string Grupo, int Entregues, int NoPrazo, int Atrasados, double PercentualNoPrazo, double AtrasoMedioDias);
public sealed record LinhaDesenvolvedor(string Desenvolvedor, int Concluidos, decimal Pontos, double LeadTimeMedioDias, int Ativos, int AtivosAtrasados);
public sealed record Percentis(double P50, double P85, double P95, int Amostras);
public sealed record PrevisaoQuando(DateTime P50, DateTime P85, DateTime P95, int Restantes, int SemanasDeHistorico);
public sealed record PrevisaoQuantos(int P50, int P85, int P95, DateTime DataAlvo);

/// <summary>Dados de um quadro prontos para os relatórios, reconstruídos a partir do histórico de movimentações.</summary>
public sealed class DadosRelatorio
{
    public required FiltroRelatorio Filtro { get; init; }
    public required List<Lista> Listas { get; init; }
    public required List<Cartao> Cartoes { get; init; }
    public required ILookup<int, HistoricoAtividade> Entradas { get; init; }
    public required Dictionary<int, string> Usuarios { get; init; }

    /// <summary>Cartões não excluídos (o fluxo acumulado usa todos, pois os excluídos existiam antes da exclusão).</summary>
    public IEnumerable<Cartao> Validos => Cartoes.Where(c => !c.Excluido);

    public bool EhConcluida(int listaId) => ListasQuadro.EhConcluido(listaId, Listas);
    public bool EhBacklog(int listaId) => Listas.FirstOrDefault(l => l.Id == listaId)?.EhBacklog == true;

    /// <summary>Primeira entrada numa lista "Concluído" (se o cartão estiver lá sem histórico, usa a última alteração).</summary>
    public DateTime? ConcluidoEm(Cartao cartao)
    {
        var entrada = Entradas[cartao.Id].Where(h => h.ListaDestinoId.HasValue && EhConcluida(h.ListaDestinoId.Value)).MinBy(h => h.OcorridoEm);
        if (entrada is not null) return entrada.OcorridoEm;
        return EhConcluida(cartao.ListaId) ? cartao.AlteradoEm ?? cartao.CriadoEm : null;
    }

    /// <summary>Quando o trabalho começou: primeira entrada numa lista que não é backlog nem conclusão.</summary>
    public DateTime? IniciadoEm(Cartao cartao)
        => Entradas[cartao.Id]
            .Where(h => h.ListaDestinoId.HasValue && !EhBacklog(h.ListaDestinoId.Value) && !EhConcluida(h.ListaDestinoId.Value)
                && h.Tipo == TipoHistoricoAtividade.CartaoMovido)
            .MinBy(h => h.OcorridoEm)?.OcorridoEm;

    /// <summary>Lista em que o cartão estava no fim do dia (pela última entrada registrada até então).</summary>
    public int? ListaNoDia(Cartao cartao, DateTime fimDoDia)
    {
        if (cartao.CriadoEm > fimDoDia) return null;
        if (cartao.Excluido && cartao.ExcluidoEm.HasValue && cartao.ExcluidoEm <= fimDoDia) return null;
        var ultima = Entradas[cartao.Id].Where(h => h.OcorridoEm <= fimDoDia && h.ListaDestinoId.HasValue).MaxBy(h => h.OcorridoEm);
        return ultima?.ListaDestinoId ?? cartao.ListaId;
    }

    public string Devs(Cartao cartao) => string.Join(", ", cartao.Desenvolvedores.OrderByDescending(d => d.Principal)
        .Select(d => Usuarios.GetValueOrDefault(d.UsuarioId)).OfType<string>());

    public string NomeLista(int listaId) => Listas.FirstOrDefault(l => l.Id == listaId) is { } lista ? ListasQuadro.NomeCompleto(lista, Listas) : "?";
}

/// <summary>Relatórios de fluxo (CFD, vazão, tempos, envelhecimento, previsão, prazos e pessoas).</summary>
public sealed class RelatoriosService(KanbanDbContext db)
{
    public const int MaximoSeriesFluxo = 8;

    public async Task<DadosRelatorio> CarregarAsync(FiltroRelatorio filtro)
    {
        var listas = await db.Listas.AsNoTracking().IgnoreQueryFilters().Where(l => l.QuadroId == filtro.QuadroId).ToListAsync();
        var idsListas = listas.Select(l => l.Id).ToList();
        listas = listas.Where(l => !l.Excluido).ToList();

        var consulta = db.Cartoes.AsNoTracking().IgnoreQueryFilters().Include(c => c.Desenvolvedores).Include(c => c.Sistema)
            .Where(c => idsListas.Contains(c.ListaId));
        if (filtro.SistemaId.HasValue) consulta = consulta.Where(c => c.SistemaId == filtro.SistemaId);
        if (filtro.DesenvolvedorId.HasValue) consulta = consulta.Where(c => c.Desenvolvedores.Any(d => d.UsuarioId == filtro.DesenvolvedorId));
        var cartoes = await consulta.AsSplitQuery().ToListAsync();
        var idsCartoes = cartoes.Select(c => c.Id).ToList();

        var entradas = await db.HistoricoAtividades.AsNoTracking()
            .Where(h => idsCartoes.Contains(h.CartaoId) && h.ListaDestinoId != null
                && (h.Tipo == TipoHistoricoAtividade.CartaoMovido || h.Tipo == TipoHistoricoAtividade.CartaoCriado))
            .ToListAsync();

        return new DadosRelatorio
        {
            Filtro = filtro,
            Listas = listas,
            Cartoes = cartoes,
            Entradas = entradas.ToLookup(h => h.CartaoId),
            Usuarios = await db.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.Nome)
        };
    }

    /// <summary>
    /// Fluxo acumulado: cartões por lista no fim de cada dia do período. Com mais de 60 dias, amostra por semana.
    /// Listas além do limite de cores seguras viram "Outras".
    /// </summary>
    public static (List<string> Series, List<LinhaFluxo> Linhas) Fluxo(DadosRelatorio dados)
    {
        var folhas = ListasQuadro.FolhasEmOrdem(dados.Listas);
        var nomes = folhas.Take(MaximoSeriesFluxo - (folhas.Count > MaximoSeriesFluxo ? 1 : 0)).ToDictionary(l => l.Id, l => dados.NomeLista(l.Id));
        var series = nomes.Values.ToList();
        if (folhas.Count > MaximoSeriesFluxo) series.Add("Outras");

        var passo = (dados.Filtro.Ate - dados.Filtro.De).TotalDays > 60 ? 7 : 1;
        var linhas = new List<LinhaFluxo>();
        for (var dia = dados.Filtro.De.Date; dia <= dados.Filtro.Ate.Date; dia = dia.AddDays(passo))
        {
            var fim = dia.AddDays(1).ToUniversalTime();
            var contagem = series.ToDictionary(s => s, _ => 0);
            foreach (var cartao in dados.Cartoes)
            {
                if (dados.ListaNoDia(cartao, fim) is not int listaId) continue;
                var serie = nomes.TryGetValue(listaId, out var nome) ? nome : folhas.Any(f => f.Id == listaId) ? "Outras" : null;
                if (serie is not null && contagem.ContainsKey(serie)) contagem[serie]++;
            }
            linhas.Add(new LinhaFluxo(dia, contagem));
        }
        return (series, linhas);
    }

    private static DateTime InicioDaSemana(DateTime data) => data.Date.AddDays(-(((int)data.DayOfWeek + 6) % 7));

    public static List<LinhaVazao> Vazao(DadosRelatorio dados)
    {
        var concluidos = dados.Validos.Select(c => (Cartao: c, Em: dados.ConcluidoEm(c)?.ToLocalTime()))
            .Where(x => x.Em.HasValue && x.Em.Value.Date >= dados.Filtro.De.Date && x.Em.Value.Date <= dados.Filtro.Ate.Date).ToList();
        var linhas = new List<LinhaVazao>();
        for (var semana = InicioDaSemana(dados.Filtro.De); semana <= dados.Filtro.Ate.Date; semana = semana.AddDays(7))
        {
            var daSemana = concluidos.Where(x => InicioDaSemana(x.Em!.Value) == semana).ToList();
            linhas.Add(new LinhaVazao(semana, daSemana.Count, daSemana.Sum(x => x.Cartao.Estimativa ?? 0)));
        }
        return linhas;
    }

    public static List<LinhaCiclo> Ciclos(DadosRelatorio dados)
        => dados.Validos
            .Select(c => (Cartao: c, Concluido: dados.ConcluidoEm(c), Iniciado: dados.IniciadoEm(c)))
            .Where(x => x.Concluido.HasValue && x.Concluido.Value.ToLocalTime().Date >= dados.Filtro.De.Date && x.Concluido.Value.ToLocalTime().Date <= dados.Filtro.Ate.Date)
            .Select(x => new LinhaCiclo(x.Cartao.Id, x.Cartao.Titulo, x.Cartao.Sistema?.Nome ?? "", dados.Devs(x.Cartao),
                x.Cartao.CriadoEm.ToLocalTime(), x.Iniciado?.ToLocalTime(), x.Concluido!.Value.ToLocalTime(),
                Math.Max(0, (x.Concluido.Value - x.Cartao.CriadoEm).TotalDays),
                x.Iniciado.HasValue && x.Iniciado <= x.Concluido ? (x.Concluido.Value - x.Iniciado.Value).TotalDays : null))
            .OrderByDescending(l => l.LeadTimeDias).ToList();

    public static Percentis CalcularPercentis(IReadOnlyCollection<double> valores)
    {
        if (valores.Count == 0) return new Percentis(0, 0, 0, 0);
        var ordenados = valores.OrderBy(v => v).ToArray();
        double P(double p)
        {
            var posicao = (ordenados.Length - 1) * p;
            var baixo = (int)Math.Floor(posicao);
            var alto = (int)Math.Ceiling(posicao);
            return ordenados[baixo] + (ordenados[alto] - ordenados[baixo]) * (posicao - baixo);
        }
        return new Percentis(P(0.5), P(0.85), P(0.95), ordenados.Length);
    }

    /// <summary>Distribuição em faixas de dias (para o histograma do tempo de ciclo).</summary>
    public static List<(string Faixa, int Quantidade)> Histograma(IReadOnlyCollection<double> dias)
    {
        (string Rotulo, double Ate)[] faixas = [("até 1 d", 1), ("1–3 d", 3), ("3–7 d", 7), ("7–14 d", 14), ("14–30 d", 30), ("30–60 d", 60), ("60+ d", double.MaxValue)];
        var resultado = new List<(string, int)>();
        double anterior = -1;
        foreach (var (rotulo, ate) in faixas)
        {
            resultado.Add((rotulo, dias.Count(d => d > anterior && d <= ate)));
            anterior = ate;
        }
        return resultado;
    }

    public static List<LinhaEnvelhecimento> Envelhecimento(DadosRelatorio dados)
    {
        var hoje = DateTime.Today;
        return dados.Cartoes.Where(c => !c.Excluido && !dados.EhConcluida(c.ListaId) && dados.Listas.Any(l => l.Id == c.ListaId))
            .Select(c =>
            {
                var entrada = dados.Entradas[c.Id].Where(h => h.ListaDestinoId == c.ListaId).MaxBy(h => h.OcorridoEm)?.OcorridoEm ?? c.CriadoEm;
                var prazo = c.Prazo?.ToLocalTime().Date;
                return new LinhaEnvelhecimento(c.Id, c.Titulo, dados.NomeLista(c.ListaId), dados.Devs(c),
                    (hoje - entrada.ToLocalTime().Date).Days, (hoje - c.CriadoEm.ToLocalTime().Date).Days, prazo, prazo < hoje);
            })
            .OrderByDescending(l => l.DiasNaLista).ToList();
    }

    public static List<LinhaPrazo> Prazos(DadosRelatorio dados, Func<Cartao, IEnumerable<string>> agrupar)
    {
        var entregues = dados.Validos
            .Select(c => (Cartao: c, Concluido: dados.ConcluidoEm(c)))
            .Where(x => x.Cartao.Prazo.HasValue && x.Concluido.HasValue
                && x.Concluido.Value.ToLocalTime().Date >= dados.Filtro.De.Date && x.Concluido.Value.ToLocalTime().Date <= dados.Filtro.Ate.Date)
            .Select(x => (x.Cartao, Atraso: (x.Concluido!.Value.ToLocalTime().Date - x.Cartao.Prazo!.Value.ToLocalTime().Date).TotalDays))
            .ToList();
        return entregues
            .SelectMany(x => agrupar(x.Cartao).Select(grupo => (Grupo: grupo, x.Atraso)))
            .GroupBy(x => x.Grupo)
            .Select(g =>
            {
                var atrasados = g.Where(x => x.Atraso > 0).ToList();
                return new LinhaPrazo(g.Key, g.Count(), g.Count() - atrasados.Count, atrasados.Count,
                    100.0 * (g.Count() - atrasados.Count) / g.Count(), atrasados.Count == 0 ? 0 : atrasados.Average(x => x.Atraso));
            })
            .OrderBy(l => l.PercentualNoPrazo).ToList();
    }

    public static List<LinhaDesenvolvedor> Desenvolvedores(DadosRelatorio dados)
    {
        var ciclos = Ciclos(dados).ToDictionary(c => c.CartaoId);
        var hoje = DateTime.Today;
        return dados.Validos.SelectMany(c => c.Desenvolvedores.Select(d => (UsuarioId: d.UsuarioId, Cartao: c)))
            .GroupBy(x => x.UsuarioId)
            .Select(g =>
            {
                var concluidos = g.Where(x => ciclos.ContainsKey(x.Cartao.Id)).ToList();
                var ativos = g.Where(x => !x.Cartao.Excluido && !dados.EhConcluida(x.Cartao.ListaId)).ToList();
                return new LinhaDesenvolvedor(dados.Usuarios.GetValueOrDefault(g.Key) ?? $"#{g.Key}", concluidos.Count,
                    concluidos.Sum(x => x.Cartao.Estimativa ?? 0),
                    concluidos.Count == 0 ? 0 : concluidos.Average(x => ciclos[x.Cartao.Id].LeadTimeDias),
                    ativos.Count, ativos.Count(x => x.Cartao.Prazo.HasValue && x.Cartao.Prazo.Value.ToLocalTime().Date < hoje));
            })
            .OrderByDescending(l => l.Concluidos).ThenBy(l => l.Desenvolvedor).ToList();
    }

    /// <summary>Entregas por semana nas últimas <paramref name="semanas"/> semanas completas (base da previsão).</summary>
    public static int[] AmostrasSemanais(DadosRelatorio dados, int semanas)
    {
        var atual = InicioDaSemana(DateTime.Today);
        var concluidos = dados.Validos.Select(dados.ConcluidoEm).OfType<DateTime>().Select(d => InicioDaSemana(d.ToLocalTime())).ToList();
        return Enumerable.Range(1, semanas).Select(i => concluidos.Count(s => s == atual.AddDays(-7 * i))).ToArray();
    }

    /// <summary>
    /// Monte Carlo: sorteia semanas passadas (com reposição) até zerar os cartões restantes.
    /// P85 = em 85% das simulações terminou até essa data.
    /// </summary>
    public static PrevisaoQuando? PreverQuando(int[] amostras, int restantes, int simulacoes = 10_000)
    {
        if (restantes <= 0 || amostras.Length == 0 || amostras.All(a => a == 0)) return null;
        var aleatorio = new Random(42);
        var semanas = new double[simulacoes];
        for (var i = 0; i < simulacoes; i++)
        {
            var falta = restantes;
            var n = 0;
            while (falta > 0 && n < 520)
            {
                falta -= amostras[aleatorio.Next(amostras.Length)];
                n++;
            }
            semanas[i] = n;
        }
        var p = CalcularPercentis(semanas);
        var hoje = DateTime.Today;
        return new PrevisaoQuando(hoje.AddDays(7 * Math.Ceiling(p.P50)), hoje.AddDays(7 * Math.Ceiling(p.P85)), hoje.AddDays(7 * Math.Ceiling(p.P95)), restantes, amostras.Length);
    }

    /// <summary>Quantos cartões ficam prontos até a data. P85 = quantidade atingida ou superada em 85% das simulações.</summary>
    public static PrevisaoQuantos? PreverQuantos(int[] amostras, DateTime dataAlvo, int simulacoes = 10_000)
    {
        var semanas = (int)Math.Floor((dataAlvo.Date - DateTime.Today).TotalDays / 7);
        if (semanas <= 0 || amostras.Length == 0 || amostras.All(a => a == 0)) return null;
        var aleatorio = new Random(42);
        var totais = new double[simulacoes];
        for (var i = 0; i < simulacoes; i++)
        {
            var soma = 0;
            for (var s = 0; s < semanas; s++) soma += amostras[aleatorio.Next(amostras.Length)];
            totais[i] = soma;
        }
        // Para "quantos", a leitura conservadora é o percentil baixo: 85% das simulações entregaram pelo menos isso.
        var ordenados = totais.OrderBy(t => t).ToArray();
        int Q(double p) => (int)ordenados[(int)Math.Floor((ordenados.Length - 1) * p)];
        return new PrevisaoQuantos(Q(0.5), Q(0.15), Q(0.05), dataAlvo);
    }
}
