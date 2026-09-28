using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Core.Regras;
using KanbanDemandas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KanbanDemandas.Web.Services.Integracoes;

public sealed record ResultadoWebhookGitHub(int Vinculos, int Eventos, string Mensagem);

/// <summary>
/// Recebe webhooks do GitHub (push, create e pull_request) e vincula aos cartões citados pelo código
/// (ex.: KB-123) em mensagens de commit, nomes de branch, títulos e descrições de pull requests.
/// </summary>
public sealed class GitHubWebhookService(KanbanDbContext db, IFilaEventosAutomacao fila, IConfiguration configuracao, ILogger<GitHubWebhookService> logger)
{
    private string? Prefixo => configuracao["Cartao:PrefixoCodigo"];
    private int UsuarioSistemaId => configuracao.GetValue("Automacao:UsuarioSistemaId", 1);

    /// <summary>Confere o cabeçalho X-Hub-Signature-256 (HMAC-SHA256 do corpo com o segredo configurado).</summary>
    public static bool AssinaturaValida(string corpo, string? assinatura, string segredo)
    {
        if (string.IsNullOrWhiteSpace(assinatura) || !assinatura.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)) return false;
        var esperado = HMACSHA256.HashData(Encoding.UTF8.GetBytes(segredo), Encoding.UTF8.GetBytes(corpo));
        byte[] recebido;
        try { recebido = Convert.FromHexString(assinatura["sha256=".Length..]); }
        catch (FormatException) { return false; }
        return CryptographicOperations.FixedTimeEquals(esperado, recebido);
    }

    public async Task<ResultadoWebhookGitHub> ProcessarAsync(string tipoEvento, string corpo)
    {
        using var documento = JsonDocument.Parse(corpo);
        var raiz = documento.RootElement;
        var repositorio = Texto(raiz, "repository", "full_name") ?? "?";
        var urlRepositorio = Texto(raiz, "repository", "html_url") ?? "";
        var novos = new List<(VinculoGit Vinculo, TipoGatilho? Gatilho)>();

        switch (tipoEvento)
        {
            case "ping":
                return new ResultadoWebhookGitHub(0, 0, "pong");

            case "create" when Texto(raiz, "ref_type") == "branch":
            {
                var branch = Texto(raiz, "ref") ?? "";
                foreach (var id in CodigoCartao.Extrair(branch, Prefixo))
                    novos.Add((await RegistrarAsync(id, TipoVinculoGit.Branch, repositorio, branch, $"Branch {branch}",
                        $"{urlRepositorio}/tree/{Uri.EscapeDataString(branch)}", Texto(raiz, "sender", "login"), null), null));
                break;
            }

            case "push":
            {
                var branch = (Texto(raiz, "ref") ?? "").Replace("refs/heads/", "");
                var idsBranch = CodigoCartao.Extrair(branch, Prefixo);
                foreach (var id in idsBranch)
                    novos.Add((await RegistrarAsync(id, TipoVinculoGit.Branch, repositorio, branch, $"Branch {branch}",
                        $"{urlRepositorio}/tree/{Uri.EscapeDataString(branch)}", Texto(raiz, "pusher", "name"), null), null));
                if (raiz.TryGetProperty("commits", out var commits) && commits.ValueKind == JsonValueKind.Array)
                {
                    foreach (var commit in commits.EnumerateArray())
                    {
                        var mensagem = Texto(commit, "message") ?? "";
                        var sha = Texto(commit, "id") ?? "";
                        var titulo = mensagem.Split('\n')[0];
                        foreach (var id in CodigoCartao.Extrair(mensagem, Prefixo).Union(idsBranch))
                            novos.Add((await RegistrarAsync(id, TipoVinculoGit.Commit, repositorio, sha, $"{sha[..Math.Min(7, sha.Length)]} {titulo}",
                                Texto(commit, "url") ?? "", Texto(commit, "author", "username") ?? Texto(commit, "author", "name"), null), TipoGatilho.CommitVinculado));
                    }
                }
                break;
            }

            case "pull_request":
            {
                var acao = Texto(raiz, "action") ?? "";
                if (!raiz.TryGetProperty("pull_request", out var pr)) break;
                var numero = pr.TryGetProperty("number", out var n) ? n.GetInt32().ToString() : "?";
                var mergeado = pr.TryGetProperty("merged", out var m) && m.ValueKind == JsonValueKind.True;
                var estado = mergeado ? EstadoPullRequest.Mergeado : Texto(pr, "state") == "closed" ? EstadoPullRequest.Fechado : EstadoPullRequest.Aberto;
                var textoBusca = $"{Texto(pr, "title")}\n{Texto(pr, "body")}\n{Texto(pr, "head", "ref")}";
                TipoGatilho? gatilho = acao switch
                {
                    "opened" or "reopened" => TipoGatilho.PullRequestAberto,
                    "closed" when mergeado => TipoGatilho.PullRequestMergeado,
                    _ => null
                };
                foreach (var id in CodigoCartao.Extrair(textoBusca, Prefixo))
                    novos.Add((await RegistrarAsync(id, TipoVinculoGit.PullRequest, repositorio, numero, $"#{numero} {Texto(pr, "title")}",
                        Texto(pr, "html_url") ?? "", Texto(pr, "user", "login"), estado), gatilho));
                break;
            }

            default:
                return new ResultadoWebhookGitHub(0, 0, $"evento '{tipoEvento}' ignorado");
        }

        var validos = novos.Where(x => x.Vinculo.CartaoId > 0).ToList();
        await db.SaveChangesAsync();
        var eventos = 0;
        foreach (var (vinculo, gatilho) in validos.Where(x => x.Gatilho.HasValue).DistinctBy(x => (x.Vinculo.CartaoId, x.Gatilho)))
        {
            fila.Enfileirar(new EventoAutomacao(gatilho!.Value, vinculo.CartaoId, UsuarioSistemaId, 0, []));
            eventos++;
        }
        logger.LogInformation("Webhook GitHub {Evento} de {Repositorio}: {Vinculos} vínculo(s), {Eventos} evento(s).", tipoEvento, repositorio, validos.Count, eventos);
        return new ResultadoWebhookGitHub(validos.Count, eventos, "ok");
    }

    /// <summary>Cria ou atualiza o vínculo; cartão inexistente devolve um vínculo com CartaoId 0 (descartado).</summary>
    private async Task<VinculoGit> RegistrarAsync(int cartaoId, TipoVinculoGit tipo, string repositorio, string identificador, string titulo,
        string url, string? autor, EstadoPullRequest? estado)
    {
        if (!await db.Cartoes.AnyAsync(c => c.Id == cartaoId && !c.Excluido)) return new VinculoGit();
        var vinculo = db.VinculosGit.Local.FirstOrDefault(v => v.CartaoId == cartaoId && v.Tipo == tipo && v.Repositorio == repositorio && v.Identificador == identificador)
            ?? await db.VinculosGit.FirstOrDefaultAsync(v => v.CartaoId == cartaoId && v.Tipo == tipo && v.Repositorio == repositorio && v.Identificador == identificador);
        if (vinculo is null)
        {
            vinculo = new VinculoGit
            {
                CartaoId = cartaoId, Tipo = tipo, Repositorio = Limitar(repositorio, 300), Identificador = Limitar(identificador, 300), CriadoEm = DateTime.UtcNow
            };
            db.VinculosGit.Add(vinculo);
        }
        vinculo.Titulo = Limitar(titulo, 500);
        vinculo.Url = Limitar(url, 1000);
        vinculo.Autor = autor is null ? null : Limitar(autor, 200);
        vinculo.Estado = estado ?? vinculo.Estado;
        vinculo.AtualizadoEm = DateTime.UtcNow;
        return vinculo;
    }

    private static string Limitar(string texto, int maximo) => texto.Length <= maximo ? texto : texto[..maximo];

    private static string? Texto(JsonElement elemento, params string[] caminho)
    {
        foreach (var parte in caminho)
        {
            if (elemento.ValueKind != JsonValueKind.Object || !elemento.TryGetProperty(parte, out elemento)) return null;
        }
        return elemento.ValueKind == JsonValueKind.String ? elemento.GetString() : null;
    }
}
