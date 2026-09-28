using System.Globalization;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Web.Services.Automacoes;

public static class AvaliadorCondicoes
{
    public static bool Atende(IReadOnlyCollection<Condicao> condicoes, bool exigirTodas, Cartao cartao, FotoQuadro foto, decimal pontosPorDia)
    {
        if (condicoes.Count == 0) return true;
        return exigirTodas
            ? condicoes.All(c => Avaliar(c, cartao, foto, pontosPorDia))
            : condicoes.Any(c => Avaliar(c, cartao, foto, pontosPorDia));
    }

    public static bool Avaliar(Condicao condicao, Cartao cartao, FotoQuadro foto, decimal pontosPorDia)
    {
        var op = condicao.Operador;
        return condicao.Tipo switch
        {
            TipoCondicao.Lista => Conjunto(op, condicao.Ids.Count > 0 && foto.ListaOuPaiEm(cartao.ListaId, condicao.Ids), true),
            TipoCondicao.Prioridade => op switch
            {
                OperadorCondicao.Vazio => !cartao.Prioridade.HasValue,
                OperadorCondicao.NaoVazio => cartao.Prioridade.HasValue,
                OperadorCondicao.MaiorOuIgual => cartao.Prioridade.HasValue && condicao.Ids.Count > 0 && (int)cartao.Prioridade.Value >= condicao.Ids[0],
                OperadorCondicao.MenorOuIgual => cartao.Prioridade.HasValue && condicao.Ids.Count > 0 && (int)cartao.Prioridade.Value <= condicao.Ids[0],
                _ => Conjunto(op, cartao.Prioridade.HasValue && condicao.Ids.Contains((int)cartao.Prioridade.Value), cartao.Prioridade.HasValue)
            },
            TipoCondicao.Etiqueta => Conjunto(op, cartao.Etiquetas.Any(e => condicao.Ids.Contains(e.EtiquetaId)), cartao.Etiquetas.Count > 0),
            TipoCondicao.Sistema => Conjunto(op, cartao.SistemaId.HasValue && condicao.Ids.Contains(cartao.SistemaId.Value), cartao.SistemaId.HasValue),
            TipoCondicao.Desenvolvedor => Conjunto(op, cartao.Desenvolvedores.Any(d => condicao.Ids.Contains(d.UsuarioId)), cartao.Desenvolvedores.Count > 0),
            TipoCondicao.Solicitante => Conjunto(op, cartao.SolicitanteId.HasValue && condicao.Ids.Contains(cartao.SolicitanteId.Value), cartao.SolicitanteId.HasValue),
            TipoCondicao.CampoPersonalizado => AvaliarCampo(condicao, cartao, foto),
            TipoCondicao.TemPrazo => SimNao(op, cartao.Prazo.HasValue),
            TipoCondicao.Atrasado => SimNao(op, cartao.Prazo.HasValue && cartao.Prazo.Value.ToLocalTime().Date < DateTime.Today && !foto.EstaConcluido(cartao)),
            TipoCondicao.Bloqueado => SimNao(op, foto.EstaBloqueado(cartao)),
            TipoCondicao.TemConflito => SimNao(op, foto.Conflitos(cartao).Count > 0),
            TipoCondicao.PrazoEmRisco => SimNao(op, foto.PrazoEmRisco(cartao, pontosPorDia)),
            TipoCondicao.ChecklistCompleto => SimNao(op, cartao.ItensTarefa.Count > 0 && cartao.ItensTarefa.All(i => i.Concluido)),
            TipoCondicao.DiasNaLista => Comparar(op, foto.DiasNaLista(cartao), condicao.Numero),
            TipoCondicao.Estimativa => op switch
            {
                OperadorCondicao.Vazio => !cartao.Estimativa.HasValue,
                OperadorCondicao.NaoVazio => cartao.Estimativa.HasValue,
                _ => cartao.Estimativa.HasValue && Comparar(op, cartao.Estimativa.Value, condicao.Numero)
            },
            TipoCondicao.DiasParaPrazo => cartao.Prazo.HasValue
                && Comparar(op, (cartao.Prazo.Value.ToLocalTime().Date - DateTime.Today).Days, condicao.Numero),
            TipoCondicao.Titulo => Texto(op, cartao.Titulo, condicao.Texto),
            _ => false
        };
    }

    // EstaEm/NaoEstaEm comparam com os IDs escolhidos; Vazio/NaoVazio olham se o cartão tem algum valor.
    private static bool Conjunto(OperadorCondicao op, bool contido, bool temValor) => op switch
    {
        OperadorCondicao.NaoEstaEm or OperadorCondicao.Diferente => !contido,
        OperadorCondicao.Vazio => !temValor,
        OperadorCondicao.NaoVazio => temValor,
        _ => contido
    };

    private static bool SimNao(OperadorCondicao op, bool valor) => op == OperadorCondicao.Nao ? !valor : valor;

    private static bool Comparar(OperadorCondicao op, decimal valor, decimal? referencia)
    {
        if (!referencia.HasValue) return false;
        return op switch
        {
            OperadorCondicao.MenorOuIgual => valor <= referencia,
            OperadorCondicao.Igual => valor == referencia,
            OperadorCondicao.Diferente => valor != referencia,
            _ => valor >= referencia
        };
    }

    private static bool Texto(OperadorCondicao op, string? valor, string? referencia)
    {
        valor ??= string.Empty;
        referencia ??= string.Empty;
        return op switch
        {
            OperadorCondicao.Vazio => string.IsNullOrWhiteSpace(valor),
            OperadorCondicao.NaoVazio => !string.IsNullOrWhiteSpace(valor),
            OperadorCondicao.Igual => valor.Trim().Equals(referencia.Trim(), StringComparison.OrdinalIgnoreCase),
            OperadorCondicao.Diferente => !valor.Trim().Equals(referencia.Trim(), StringComparison.OrdinalIgnoreCase),
            OperadorCondicao.NaoContem => !valor.Contains(referencia, StringComparison.OrdinalIgnoreCase),
            _ => valor.Contains(referencia, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static bool AvaliarCampo(Condicao condicao, Cartao cartao, FotoQuadro foto)
    {
        var (definicao, valor) = foto.ValorCampo(cartao, condicao.CampoNome);
        if (definicao is null) return condicao.Operador == OperadorCondicao.Vazio;
        if (condicao.Operador is OperadorCondicao.MaiorOuIgual or OperadorCondicao.MenorOuIgual)
        {
            if (string.IsNullOrWhiteSpace(valor)) return false;
            if (definicao.Tipo == TipoCampo.Data && DateTime.TryParse(valor, out var data) && DateTime.TryParse(condicao.Texto, out var referenciaData))
                return condicao.Operador == OperadorCondicao.MaiorOuIgual ? data.Date >= referenciaData.Date : data.Date <= referenciaData.Date;
            if (decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var numero))
                return Comparar(condicao.Operador, numero, condicao.Numero);
            return false;
        }
        if (definicao.Tipo == TipoCampo.Checkbox && condicao.Operador is OperadorCondicao.Sim or OperadorCondicao.Nao)
            return SimNao(condicao.Operador, bool.TryParse(valor, out var marcado) && marcado);
        return Texto(condicao.Operador, valor, condicao.Texto);
    }
}
