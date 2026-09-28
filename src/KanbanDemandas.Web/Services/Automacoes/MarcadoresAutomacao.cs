using System.Net;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Regras;

namespace KanbanDemandas.Web.Services.Automacoes;

/// <summary>Endereço público da aplicação, para links em e-mails gerados fora de uma requisição.</summary>
public sealed class UrlBaseAplicacao(IConfiguration configuracao)
{
    private string? _detectada;

    public string Valor
    {
        get
        {
            var configurada = configuracao["Aplicacao:UrlBase"]?.TrimEnd('/');
            return string.IsNullOrWhiteSpace(configurada) ? _detectada ?? "" : configurada;
        }
    }

    /// <summary>Guarda o endereço da primeira requisição recebida quando nenhum foi configurado.</summary>
    public void Detectar(HttpRequest requisicao) => _detectada ??= $"{requisicao.Scheme}://{requisicao.Host}{requisicao.PathBase}";

    public string LinkCartao(int quadroId, int cartaoId) => $"{Valor}/quadros/{quadroId}?cartao={cartaoId}";
}

/// <summary>Substitui marcadores como {titulo} e {prazo} pelos dados do cartão.</summary>
public static class MarcadoresAutomacao
{
    public static readonly (string Marcador, string Descricao)[] Disponiveis =
    [
        ("{codigo}", "código do cartão (ex.: KB-123)"), ("{titulo}", "título do cartão"), ("{lista}", "lista atual"), ("{prazo}", "prazo"), ("{inicio}", "data de início"),
        ("{prioridade}", "prioridade"), ("{desenvolvedores}", "desenvolvedores"), ("{solicitante}", "solicitante"),
        ("{sistema}", "sistema"), ("{dias_na_lista}", "dias na lista atual"), ("{conflitos}", "cartões em conflito"),
        ("{hoje}", "data de hoje"), ("{regra}", "nome da regra"), ("{link}", "link para o cartão")
    ];

    public static string Aplicar(string? texto, Cartao? cartao, FotoQuadro foto, string nomeRegra, string link, bool html, string? prefixoCodigo = null)
    {
        if (string.IsNullOrEmpty(texto)) return string.Empty;
        string V(string? valor) => html ? WebUtility.HtmlEncode(valor ?? "") : valor ?? "";
        var resultado = html ? WebUtility.HtmlEncode(texto).Replace("\n", "<br>", StringComparison.Ordinal) : texto;

        resultado = resultado
            .Replace("{hoje}", V(DateTime.Today.ToString("dd/MM/yyyy")), StringComparison.OrdinalIgnoreCase)
            .Replace("{regra}", V(nomeRegra), StringComparison.OrdinalIgnoreCase);
        if (cartao is null) return resultado;

        var linkFormatado = html ? $"<a href=\"{WebUtility.HtmlEncode(link)}\">{WebUtility.HtmlEncode(link)}</a>" : link;
        var desenvolvedores = string.Join(", ", cartao.Desenvolvedores.OrderByDescending(d => d.Principal).Select(d => foto.NomeUsuario(d.UsuarioId)).OfType<string>());
        var conflitos = string.Join(", ", foto.Conflitos(cartao).Select(c => c.Titulo));
        var sistema = cartao.Sistema?.Nome;

        return resultado
            .Replace("{codigo}", V(CodigoCartao.Formatar(cartao.Id, prefixoCodigo)), StringComparison.OrdinalIgnoreCase)
            .Replace("{titulo}", V(cartao.Titulo), StringComparison.OrdinalIgnoreCase)
            .Replace("{lista}", V(foto.NomeLista(cartao.ListaId)), StringComparison.OrdinalIgnoreCase)
            .Replace("{prazo}", V(cartao.Prazo?.ToLocalTime().ToString("dd/MM/yyyy") ?? "sem prazo"), StringComparison.OrdinalIgnoreCase)
            .Replace("{inicio}", V(cartao.DataInicio?.ToLocalTime().ToString("dd/MM/yyyy") ?? "não iniciado"), StringComparison.OrdinalIgnoreCase)
            .Replace("{prioridade}", V(cartao.Prioridade?.ToString() ?? "sem prioridade"), StringComparison.OrdinalIgnoreCase)
            .Replace("{desenvolvedores}", V(desenvolvedores.Length > 0 ? desenvolvedores : "ninguém"), StringComparison.OrdinalIgnoreCase)
            .Replace("{solicitante}", V(foto.NomeUsuario(cartao.SolicitanteId) ?? "sem solicitante"), StringComparison.OrdinalIgnoreCase)
            .Replace("{sistema}", V(sistema ?? "sem sistema"), StringComparison.OrdinalIgnoreCase)
            .Replace("{dias_na_lista}", V(foto.DiasNaLista(cartao).ToString()), StringComparison.OrdinalIgnoreCase)
            .Replace("{conflitos}", V(conflitos.Length > 0 ? conflitos : "nenhum"), StringComparison.OrdinalIgnoreCase)
            .Replace("{link}", linkFormatado, StringComparison.OrdinalIgnoreCase);
    }
}
