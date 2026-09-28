using System.Net.Http.Json;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KanbanDemandas.Web.Services;

public sealed record FeriadoCalculado(DateTime Data, string Nome, TipoFeriado Tipo);

/// <summary>Feriados nacionais brasileiros calculados localmente (fixos e móveis, a partir da Páscoa).</summary>
public static class FeriadosNacionais
{
    /// <summary>Domingo de Páscoa pelo algoritmo de Meeus/Jones/Butcher (calendário gregoriano).</summary>
    public static DateTime Pascoa(int ano)
    {
        var a = ano % 19;
        var b = ano / 100;
        var c = ano % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var mes = (h + l - 7 * m + 114) / 31;
        var dia = (h + l - 7 * m + 114) % 31 + 1;
        return new DateTime(ano, mes, dia);
    }

    public static List<FeriadoCalculado> Calcular(int ano)
    {
        var pascoa = Pascoa(ano);
        var lista = new List<FeriadoCalculado>
        {
            new(new DateTime(ano, 1, 1), "Confraternização Universal", TipoFeriado.Nacional),
            new(pascoa.AddDays(-48), "Carnaval (segunda-feira)", TipoFeriado.PontoFacultativo),
            new(pascoa.AddDays(-47), "Carnaval (terça-feira)", TipoFeriado.PontoFacultativo),
            new(pascoa.AddDays(-2), "Sexta-feira Santa", TipoFeriado.Nacional),
            new(new DateTime(ano, 4, 21), "Tiradentes", TipoFeriado.Nacional),
            new(new DateTime(ano, 5, 1), "Dia do Trabalho", TipoFeriado.Nacional),
            new(pascoa.AddDays(60), "Corpus Christi", TipoFeriado.PontoFacultativo),
            new(new DateTime(ano, 9, 7), "Independência do Brasil", TipoFeriado.Nacional),
            new(new DateTime(ano, 10, 12), "Nossa Senhora Aparecida", TipoFeriado.Nacional),
            new(new DateTime(ano, 11, 2), "Finados", TipoFeriado.Nacional),
            new(new DateTime(ano, 11, 15), "Proclamação da República", TipoFeriado.Nacional),
            new(new DateTime(ano, 12, 25), "Natal", TipoFeriado.Nacional)
        };
        // Dia Nacional de Zumbi e da Consciência Negra é feriado nacional desde 2024 (Lei 14.759/2023).
        if (ano >= 2024) lista.Add(new(new DateTime(ano, 11, 20), "Dia Nacional de Zumbi e da Consciência Negra", TipoFeriado.Nacional));
        return lista.OrderBy(f => f.Data).ToList();
    }
}

/// <summary>Cadastro de feriados e conjunto de datas usado nos cálculos de dias úteis.</summary>
public sealed class FeriadosService(KanbanDbContext db, IHttpClientFactory httpClientFactory, IUsuarioAtualProvider usuarioProvider,
    ILogger<FeriadosService> logger)
{
    private sealed record FeriadoBrasilApi(string Date, string Name, string Type);

    /// <summary>Datas não úteis no período, com os recorrentes projetados em cada ano.</summary>
    public static async Task<HashSet<DateTime>> CarregarDatasAsync(KanbanDbContext db, int anoInicial, int anoFinal)
    {
        var feriados = await db.Feriados.AsNoTracking().ToListAsync();
        var datas = new HashSet<DateTime>();
        foreach (var feriado in feriados)
        {
            if (!feriado.RecorrenteAnual)
            {
                datas.Add(feriado.Data.Date);
                continue;
            }
            for (var ano = anoInicial; ano <= anoFinal; ano++)
                if (DateTime.DaysInMonth(ano, feriado.Data.Month) >= feriado.Data.Day)
                    datas.Add(new DateTime(ano, feriado.Data.Month, feriado.Data.Day));
        }
        return datas;
    }

    /// <summary>
    /// Traz os feriados nacionais do ano pela BrasilAPI; se ela não responder, usa o cálculo local.
    /// Não duplica datas já cadastradas.
    /// </summary>
    public async Task<(int Inseridos, string Fonte)> ImportarNacionaisAsync(int ano, bool usarApi)
    {
        List<FeriadoCalculado> feriados;
        var fonte = "cálculo local";
        if (usarApi)
        {
            try
            {
                var cliente = httpClientFactory.CreateClient("BrasilAPI");
                var resposta = await cliente.GetFromJsonAsync<List<FeriadoBrasilApi>>($"api/feriados/v1/{ano}") ?? [];
                feriados = resposta.Select(f => new FeriadoCalculado(DateTime.Parse(f.Date, System.Globalization.CultureInfo.InvariantCulture), f.Name, TipoFeriado.Nacional)).ToList();
                // A BrasilAPI não traz os pontos facultativos (Carnaval e Corpus Christi); eles vêm do cálculo local.
                feriados.AddRange(FeriadosNacionais.Calcular(ano).Where(f => f.Tipo == TipoFeriado.PontoFacultativo));
                fonte = "BrasilAPI";
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "BrasilAPI indisponível; usando cálculo local dos feriados de {Ano}.", ano);
                feriados = FeriadosNacionais.Calcular(ano);
                fonte = "cálculo local (BrasilAPI indisponível)";
            }
        }
        else
        {
            feriados = FeriadosNacionais.Calcular(ano);
        }

        var inicio = new DateTime(ano, 1, 1);
        var fim = new DateTime(ano, 12, 31);
        var existentes = (await db.Feriados.Where(f => f.Data >= inicio && f.Data <= fim).Select(f => f.Data).ToListAsync())
            .Select(d => d.Date).ToHashSet();
        var usuarioId = usuarioProvider.ObterIdUsuarioAtual();
        var novos = feriados.Where(f => existentes.Add(f.Data.Date)).Select(f => new Feriado
        {
            Data = f.Data.Date, Nome = f.Nome, Tipo = f.Tipo, CriadoPorId = usuarioId, CriadoEm = DateTime.UtcNow
        }).ToList();
        db.Feriados.AddRange(novos);
        await db.SaveChangesAsync();
        return (novos.Count, fonte);
    }
}
