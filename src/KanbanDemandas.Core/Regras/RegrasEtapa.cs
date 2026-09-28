using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Regras;

/// <summary>O que cada lista (etapa) exige do cartão e o que falta para ele cumprir.</summary>
public static class RegrasEtapa
{
    public static readonly CamposFixosCartao[] CamposFixos =
        [CamposFixosCartao.Desenvolvedor, CamposFixosCartao.Prazo, CamposFixosCartao.Estimativa, CamposFixosCartao.Sistema, CamposFixosCartao.Solicitante];

    /// <summary>Campos que uma lista pode proibir (inclui a Data de início, que não é exigível: ela vem da etapa de início).</summary>
    public static readonly CamposFixosCartao[] CamposBloqueaveis =
        [CamposFixosCartao.Desenvolvedor, CamposFixosCartao.Prazo, CamposFixosCartao.DataInicio, CamposFixosCartao.Estimativa, CamposFixosCartao.Sistema, CamposFixosCartao.Solicitante];

    public static string Nome(CamposFixosCartao campo) => campo switch
    {
        CamposFixosCartao.Desenvolvedor => "Desenvolvedor",
        CamposFixosCartao.Prazo => "Prazo",
        CamposFixosCartao.Estimativa => "Estimativa",
        CamposFixosCartao.Sistema => "Sistema",
        CamposFixosCartao.Solicitante => "Solicitante",
        CamposFixosCartao.DataInicio => "Data de início",
        _ => campo.ToString()
    };

    /// <summary>Campos fixos exigidos pela lista que o cartão ainda não tem. Requer Desenvolvedores carregados.</summary>
    public static CamposFixosCartao FixosFaltando(Cartao cartao, Lista lista)
    {
        var faltando = CamposFixosCartao.Nenhum;
        var exigidos = lista.CamposFixosExigidos;
        if (exigidos.HasFlag(CamposFixosCartao.Desenvolvedor) && cartao.Desenvolvedores.Count == 0) faltando |= CamposFixosCartao.Desenvolvedor;
        if (exigidos.HasFlag(CamposFixosCartao.Prazo) && !cartao.Prazo.HasValue) faltando |= CamposFixosCartao.Prazo;
        if (exigidos.HasFlag(CamposFixosCartao.Estimativa) && !cartao.Estimativa.HasValue) faltando |= CamposFixosCartao.Estimativa;
        if (exigidos.HasFlag(CamposFixosCartao.Sistema) && !cartao.SistemaId.HasValue) faltando |= CamposFixosCartao.Sistema;
        if (exigidos.HasFlag(CamposFixosCartao.Solicitante) && !cartao.SolicitanteId.HasValue) faltando |= CamposFixosCartao.Solicitante;
        return faltando;
    }

    /// <summary>Campos que a lista proíbe e o cartão tem preenchidos. Requer Desenvolvedores carregados.</summary>
    public static CamposFixosCartao BloqueadosPreenchidos(Cartao cartao, Lista lista)
    {
        var preenchidos = CamposFixosCartao.Nenhum;
        var bloqueados = lista.CamposFixosBloqueados;
        if (bloqueados.HasFlag(CamposFixosCartao.Desenvolvedor) && cartao.Desenvolvedores.Count > 0) preenchidos |= CamposFixosCartao.Desenvolvedor;
        if (bloqueados.HasFlag(CamposFixosCartao.Prazo) && cartao.Prazo.HasValue) preenchidos |= CamposFixosCartao.Prazo;
        if (bloqueados.HasFlag(CamposFixosCartao.DataInicio) && cartao.DataInicio.HasValue) preenchidos |= CamposFixosCartao.DataInicio;
        if (bloqueados.HasFlag(CamposFixosCartao.Estimativa) && cartao.Estimativa.HasValue) preenchidos |= CamposFixosCartao.Estimativa;
        if (bloqueados.HasFlag(CamposFixosCartao.Sistema) && cartao.SistemaId.HasValue) preenchidos |= CamposFixosCartao.Sistema;
        if (bloqueados.HasFlag(CamposFixosCartao.Solicitante) && cartao.SolicitanteId.HasValue) preenchidos |= CamposFixosCartao.Solicitante;
        return preenchidos;
    }

    public static IEnumerable<CamposFixosCartao> Separar(CamposFixosCartao campos)
        => CamposBloqueaveis.Where(c => campos.HasFlag(c));

    public static string Descrever(CamposFixosCartao campos) => string.Join(", ", Separar(campos).Select(Nome));

    /// <summary>Campos que o usuário digita (os automáticos são preenchidos pelo sistema ao entrar na lista).</summary>
    public static bool EhManual(DefinicaoCampo campo) => campo.Preenchimento == PreenchimentoAutomatico.Manual;

    public static string DescreverAutomatico(DefinicaoCampo campo)
    {
        var origem = campo.Preenchimento == PreenchimentoAutomatico.DataEntrada ? "data de entrada na lista" : "usuário que moveu";
        var reentrada = campo.RegraReentrada switch
        {
            RegraReentrada.AtualizarUltimo => "atualiza a cada entrada",
            RegraReentrada.NovoRegistro => "novo registro a cada entrada",
            _ => "mantém a primeira entrada"
        };
        return $"Automático: {origem} ({reentrada})";
    }
}

/// <summary>Pendências para o cartão cumprir a etapa de uma lista.</summary>
public sealed record PendenciasEtapa(
    int ListaId,
    string ListaNome,
    bool Bloqueia,
    CamposFixosCartao FixosFaltando,
    IReadOnlyList<DefinicaoCampo> ObrigatoriosVazios,
    IReadOnlyList<DefinicaoCampo> Recorrentes,
    CamposFixosCartao BloqueadosPreenchidos = CamposFixosCartao.Nenhum)
{
    public bool TemObrigatoriasFaltando => FixosFaltando != CamposFixosCartao.Nenhum || ObrigatoriosVazios.Count > 0;
    public bool TemAlgoParaPreencher => TemObrigatoriasFaltando || Recorrentes.Count > 0;

    public IEnumerable<DefinicaoCampo> CamposParaPreencher
        => ObrigatoriosVazios.Concat(Recorrentes).DistinctBy(c => c.Id).OrderBy(c => c.Ordem);
}

/// <summary>Dados informados pelo usuário ao entrar em uma etapa.</summary>
public sealed class DadosEtapa
{
    public List<int> DesenvolvedorIds { get; set; } = [];
    public DateTime? Prazo { get; set; }
    public decimal? Estimativa { get; set; }
    public int? SistemaId { get; set; }
    public int? SolicitanteId { get; set; }
    public Dictionary<int, string?> ValoresCampo { get; set; } = [];
}
