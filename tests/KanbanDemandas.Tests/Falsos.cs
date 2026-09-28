using System.Net;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure.Data;
using KanbanDemandas.Web.Services.Automacoes;
using KanbanDemandas.Web.Services.Portal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace KanbanDemandas.Tests;

/// <summary>Dublês compartilhados pelos testes.</summary>
internal static class Falsos
{
    public static MensagemTeams Teams(HttpFalso? http = null) => new(new FabricaHttp(http ?? new HttpFalso()));

    public static NotificadorSolicitante Notificador(KanbanDbContext db, IEmailSender email, IConfiguration configuracao)
        => new(db, email, configuracao, NullLogger<NotificadorSolicitante>.Instance);

    private sealed class FabricaHttp(HttpFalso handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}

/// <summary>Registra as requisições e responde com o status configurado.</summary>
internal sealed class HttpFalso(HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
{
    public List<(Uri Url, string Corpo)> Requisicoes { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requisicoes.Add((request.RequestUri!, request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken)));
        return new HttpResponseMessage(status);
    }
}

/// <summary>Armazenamento de anexos em memória.</summary>
internal sealed class ArmazenamentoFalso : IAnexoStorage
{
    public Dictionary<string, byte[]> Arquivos { get; } = [];

    public async Task<ArquivoArmazenado> SalvarAsync(Stream conteudo, string nomeArmazenado, string contentType, CancellationToken ct = default)
    {
        using var memoria = new MemoryStream();
        await conteudo.CopyToAsync(memoria, ct);
        Arquivos[nomeArmazenado] = memoria.ToArray();
        return new ArquivoArmazenado(nomeArmazenado, false, memoria.Length);
    }

    public Task<Stream> ObterAsync(string caminho, CancellationToken ct = default) => Task.FromResult<Stream>(new MemoryStream(Arquivos[caminho]));

    public Task ExcluirAsync(string caminho, CancellationToken ct = default)
    {
        Arquivos.Remove(caminho);
        return Task.CompletedTask;
    }

    public Task<ArquivoArmazenado?> CompactarAsync(string caminho, CancellationToken ct = default) => Task.FromResult<ArquivoArmazenado?>(null);
}
