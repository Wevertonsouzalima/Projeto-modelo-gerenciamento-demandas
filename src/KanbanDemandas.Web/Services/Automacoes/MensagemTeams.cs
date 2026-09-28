using System.Net.Http.Json;
using System.Text.Json;

namespace KanbanDemandas.Web.Services.Automacoes;

/// <summary>
/// Envio de mensagens para canais do Teams via webhook (fluxo "Postar em um canal quando uma solicitação de
/// webhook for recebida" do app Workflows, ou o antigo conector "Incoming Webhook"). Não usa Entra ID:
/// quem tem a URL posta no canal. A mensagem é um Adaptive Card, formato aceito pelos dois tipos de webhook.
/// </summary>
public sealed class MensagemTeams(IHttpClientFactory httpClientFactory)
{
    public sealed record Fato(string Titulo, string Valor);

    public static bool UrlValida(string? url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;

    public static object Montar(string titulo, string? texto, IEnumerable<Fato> fatos, string? link, string? textoBotao, IEnumerable<string>? itens = null)
    {
        var corpo = new List<object>
        {
            new { type = "TextBlock", text = titulo, weight = "Bolder", size = "Medium", wrap = true }
        };
        if (!string.IsNullOrWhiteSpace(texto)) corpo.Add(new { type = "TextBlock", text = texto, wrap = true });
        var listaFatos = fatos.Where(f => !string.IsNullOrWhiteSpace(f.Valor)).Select(f => new { title = f.Titulo, value = f.Valor }).ToList();
        if (listaFatos.Count > 0) corpo.Add(new { type = "FactSet", facts = listaFatos });
        foreach (var item in itens ?? []) corpo.Add(new { type = "TextBlock", text = item, wrap = true, spacing = "None" });

        var cartao = new Dictionary<string, object>
        {
            ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
            ["type"] = "AdaptiveCard",
            ["version"] = "1.4",
            ["body"] = corpo
        };
        if (!string.IsNullOrWhiteSpace(link) && Uri.TryCreate(link, UriKind.Absolute, out _))
            cartao["actions"] = new[] { new { type = "Action.OpenUrl", title = textoBotao ?? "Abrir", url = link } };

        return new
        {
            type = "message",
            attachments = new[] { new { contentType = "application/vnd.microsoft.card.adaptive", contentUrl = (string?)null, content = cartao } }
        };
    }

    public async Task EnviarAsync(string url, object mensagem, CancellationToken ct = default)
    {
        if (!UrlValida(url)) throw new InvalidOperationException("a URL do webhook do Teams deve começar com https://.");
        var cliente = httpClientFactory.CreateClient("Teams");
        using var resposta = await cliente.PostAsJsonAsync(url, mensagem, new JsonSerializerOptions(), ct);
        if (!resposta.IsSuccessStatusCode)
        {
            var detalhe = await resposta.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"o Teams recusou a mensagem ({(int)resposta.StatusCode}): {(detalhe.Length > 200 ? detalhe[..200] : detalhe)}");
        }
    }
}
