using System.Security.Cryptography;
using System.Text;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Core.Regras;
using KanbanDemandas.Infrastructure.Data;
using KanbanDemandas.Infrastructure.Services;
using KanbanDemandas.Portal.Services;
using KanbanDemandas.Web.Services.Automacoes;
using KanbanDemandas.Web.Services.Integracoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace KanbanDemandas.Tests;

public class PortalIntegracoesTests
{
    private sealed class UsuarioFixo : IUsuarioAtualProvider
    {
        public Usuario ObterUsuarioAtual() => new() { Id = 1, Nome = "Ana" };
        public int ObterIdUsuarioAtual() => 1;
    }

    private sealed class EmailFalso : IEmailSender
    {
        public List<(string Para, string Assunto, string Corpo)> Enviados { get; } = [];
        public Task EnviarAsync(string para, string? cc, string assunto, string corpoHtml, CancellationToken ct = default)
        {
            Enviados.Add((para, assunto, corpoHtml));
            return Task.CompletedTask;
        }
    }

    private sealed class FilaFalsa : IFilaEventosAutomacao
    {
        public List<EventoAutomacao> Eventos { get; } = [];
        public void Enfileirar(EventoAutomacao evento) => Eventos.Add(evento);
    }

    private static IConfiguration Configuracao(Dictionary<string, string?>? extras = null)
    {
        var valores = new Dictionary<string, string?>
        {
            ["Portal:QuadroId"] = "1",
            ["Portal:ListaEntradaId"] = "10",
            ["Portal:UrlBase"] = "https://portal.teste",
            ["Portal:CamposVisiveis"] = "Prazo,HistoricoStatus,Comentarios",
            ["Anexos:ExtensoesPermitidas"] = ".pdf,.txt",
            ["Cartao:PrefixoCodigo"] = "KB"
        };
        foreach (var (chave, valor) in extras ?? []) valores[chave] = valor;
        return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
    }

    // Quadro 1: Triagem(10) com sublistas Nova(13) e Análise(14) | Em Andamento(11) | Concluído(12).
    // Usuários: Ana(1, time), Bruno(2, solicitante), Carla(3, outra solicitante).
    private static KanbanDbContext Banco()
    {
        var db = new KanbanDbContext(new DbContextOptionsBuilder<KanbanDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Usuarios.AddRange(
            new Usuario { Id = 1, Nome = "Ana", Email = "ana@teste.com" },
            new Usuario { Id = 2, Nome = "Bruno", Email = "bruno@teste.com" },
            new Usuario { Id = 3, Nome = "Carla", Email = "carla@teste.com" });
        db.Quadros.Add(new Quadro { Id = 1, Nome = "Quadro" });
        db.Listas.AddRange(
            new Lista { Id = 10, QuadroId = 1, Nome = "Triagem", Ordem = 0 },
            new Lista { Id = 13, QuadroId = 1, Nome = "Nova", Ordem = 0, ListaPaiId = 10 },
            new Lista { Id = 14, QuadroId = 1, Nome = "Análise", Ordem = 1, ListaPaiId = 10, StatusPortal = "Em análise" },
            new Lista { Id = 11, QuadroId = 1, Nome = "Em Andamento", Ordem = 1, StatusPortal = "Em desenvolvimento" },
            new Lista { Id = 12, QuadroId = 1, Nome = "Concluído", Ordem = 2, StatusPortal = "Entregue" });
        db.SaveChanges();
        return db;
    }

    private static PortalService Portal(KanbanDbContext db, IConfiguration configuracao, IAnexoStorage? armazenamento = null)
        => new(db, new MovimentacaoCartaoService(db, new UsuarioFixo(), configuracao), armazenamento ?? new ArmazenamentoFalso(), configuracao);

    private static ArquivoEnviado Arquivo(string nome, string conteudo = "abc")
        => new(nome, "application/octet-stream", Encoding.UTF8.GetByteCount(conteudo), () => new MemoryStream(Encoding.UTF8.GetBytes(conteudo)));

    // ---------- Código do cartão ----------

    [Fact]
    public void CodigoCartao_ExtraiVariacoesEIgnoraPalavrasColadas()
    {
        Assert.Equal("KB-42", CodigoCartao.Formatar(42));
        Assert.Equal("DEM-42", CodigoCartao.Formatar(42, "dem"));
        Assert.Equal([12, 7, 3], CodigoCartao.Extrair("Corrige KB-12, kb_7 e KB 3; ignora ABKB-9", "KB").Order().Reverse().ToArray());
        Assert.Equal([5], CodigoCartao.Extrair("feature/DEM-5-login", "DEM"));
        Assert.Empty(CodigoCartao.Extrair("sem código aqui", "KB"));
    }

    // ---------- Status público ----------

    [Fact]
    public void StatusPortal_ListasSemStatusMantemOAnteriorESemRepeticoes()
    {
        var listas = new List<Lista>
        {
            new() { Id = 1, Nome = "A" }, new() { Id = 2, Nome = "B", StatusPortal = "Em análise" },
            new() { Id = 3, Nome = "C" }, new() { Id = 4, Nome = "D", StatusPortal = "Em análise" }, new() { Id = 5, Nome = "E", StatusPortal = "Entregue" }
        };
        var inicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var cartao = new Cartao { Id = 1, CriadoEm = inicio };
        var entradas = new[] { 1, 2, 3, 4, 5 }.Select((lista, i) => new HistoricoAtividade
        {
            Id = i + 1, CartaoId = 1, Tipo = i == 0 ? TipoHistoricoAtividade.CartaoCriado : TipoHistoricoAtividade.CartaoMovido,
            ListaDestinoId = lista, OcorridoEm = inicio.AddHours(i)
        }).ToList();

        var historico = StatusPortal.Historico(cartao, entradas, listas, "Recebida");

        Assert.Equal(["Recebida", "Em análise", "Entregue"], historico.Select(h => h.Status).ToArray());
        Assert.Equal(inicio.AddHours(1), historico[1].Em);
        Assert.Equal("Entregue", StatusPortal.Atual(cartao, entradas, listas, null));
    }

    [Fact]
    public void ConfiguracaoPortal_LeEGravaCampos()
    {
        var campos = ConfiguracaoPortal.LerCampos("Prazo, comentarios,Inexistente");
        Assert.Equal(CamposPortal.Prazo | CamposPortal.Comentarios, campos);
        Assert.Equal("Prazo,Comentarios", ConfiguracaoPortal.GravarCampos(campos));
        Assert.Equal(["Bug", "Melhoria"], ConfiguracaoPortal.LerTipos("Bug, Melhoria, bug"));
    }

    // ---------- Portal: abrir, listar e ver ----------

    [Fact]
    public async Task Criar_AbreNaPrimeiraSublistaDaEntradaComOrigemPortal()
    {
        using var db = Banco();
        var armazenamento = new ArmazenamentoFalso();
        var portal = Portal(db, Configuracao(), armazenamento);

        var id = await portal.CriarAsync(2, "  Relatório quebrado ", "Ao abrir dá erro", "Bug", null, Prioridade.Alta, [Arquivo("print.txt")]);

        var cartao = await db.Cartoes.Include(c => c.Anexos).SingleAsync(c => c.Id == id);
        Assert.Equal(13, cartao.ListaId);
        Assert.True(cartao.OrigemPortal);
        Assert.Equal(2, cartao.SolicitanteId);
        Assert.Equal("Relatório quebrado", cartao.Titulo);
        Assert.Equal("Bug", cartao.TipoSolicitacao);
        Assert.Single(cartao.Anexos);
        Assert.Single(armazenamento.Arquivos);
    }

    [Fact]
    public async Task Criar_RecusaExtensaoNaoPermitidaSemCriarCartao()
    {
        using var db = Banco();
        var portal = Portal(db, Configuracao());

        var erro = await Assert.ThrowsAsync<InvalidOperationException>(() => portal.CriarAsync(2, "T", "D", null, null, null, [Arquivo("virus.exe")]));

        Assert.Contains(".exe", erro.Message);
        Assert.Empty(db.Cartoes);
    }

    [Fact]
    public async Task Criar_SemConfiguracaoFalhaComMensagemClara()
    {
        using var db = Banco();
        var portal = Portal(db, Configuracao(new() { ["Portal:QuadroId"] = "0" }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => portal.CriarAsync(2, "T", "D", null, null, null, []));
    }

    [Fact]
    public async Task Listar_SoTrazAsSolicitacoesDoUsuarioComStatusPublico()
    {
        using var db = Banco();
        var agora = DateTime.UtcNow;
        db.Cartoes.AddRange(
            new Cartao { Id = 100, Titulo = "Minha", ListaId = 11, SolicitanteId = 2, CriadoEm = agora.AddDays(-2) },
            new Cartao { Id = 101, Titulo = "Da Carla", ListaId = 11, SolicitanteId = 3, CriadoEm = agora },
            new Cartao { Id = 102, Titulo = "Excluída", ListaId = 11, SolicitanteId = 2, CriadoEm = agora, Excluido = true });
        db.HistoricoAtividades.Add(new HistoricoAtividade { CartaoId = 100, Tipo = TipoHistoricoAtividade.CartaoMovido, ListaDestinoId = 11, OcorridoEm = agora.AddDays(-1), UsuarioId = 1 });
        await db.SaveChangesAsync();

        var lista = await Portal(db, Configuracao()).ListarAsync(2);

        var item = Assert.Single(lista);
        Assert.Equal("KB-100", item.Codigo);
        Assert.Equal("Em desenvolvimento", item.Status);
        Assert.False(item.Concluida);
    }

    [Fact]
    public async Task Obter_RespeitaDonoCamposLiberadosEComentariosPublicos()
    {
        using var db = Banco();
        var prazo = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);
        db.Sistemas.Add(new Sistema { Id = 1, Nome = "ERP" });
        db.Cartoes.Add(new Cartao { Id = 100, Titulo = "Minha", ListaId = 13, SolicitanteId = 2, SistemaId = 1, Prazo = prazo, Prioridade = Prioridade.Alta, CriadoEm = DateTime.UtcNow });
        db.Comentarios.AddRange(
            new Comentario { CartaoId = 100, AutorId = 1, Texto = "Interno: rever query", DataHora = DateTime.UtcNow },
            new Comentario { CartaoId = 100, AutorId = 1, Texto = "Já estamos olhando", DataHora = DateTime.UtcNow, Publico = true });
        db.Anexos.AddRange(
            new Anexo { Id = 1, CartaoId = 100, NomeOriginal = "log-interno.txt", Caminho = "x", EnviadoPorId = 1, CriadoEm = DateTime.UtcNow },
            new Anexo { Id = 2, CartaoId = 100, NomeOriginal = "meu.txt", Caminho = "y", EnviadoPorId = 2, CriadoEm = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var portal = Portal(db, Configuracao());

        Assert.Null(await portal.ObterAsync(100, 3));

        var detalhe = await portal.ObterAsync(100, 2);
        Assert.NotNull(detalhe);
        Assert.Contains(detalhe.Informacoes, i => i.Nome == "Previsão de entrega");
        Assert.DoesNotContain(detalhe.Informacoes, i => i.Nome is "Sistema" or "Prioridade");
        var mensagem = Assert.Single(detalhe.Mensagens);
        Assert.Equal("Já estamos olhando", mensagem.Texto);
        var anexo = Assert.Single(detalhe.Anexos);
        Assert.Equal("meu.txt", anexo.Nome);
        Assert.Null(await portal.AbrirAnexoAsync(1, 2));
        Assert.Null(await portal.AbrirAnexoAsync(2, 3));
    }

    [Fact]
    public async Task EnviarMensagem_CriaComentarioPublicoENotificaResponsaveis()
    {
        using var db = Banco();
        var cartao = new Cartao { Id = 100, Titulo = "Minha", ListaId = 11, SolicitanteId = 2, CriadoEm = DateTime.UtcNow };
        cartao.Desenvolvedores.Add(new CartaoDesenvolvedor { UsuarioId = 1, Principal = true });
        db.Cartoes.Add(cartao);
        await db.SaveChangesAsync();

        await Portal(db, Configuracao()).EnviarMensagemAsync(100, 2, " Alguma novidade? ");

        var comentario = await db.Comentarios.SingleAsync();
        Assert.True(comentario.Publico);
        Assert.Equal("Alguma novidade?", comentario.Texto);
        var notificacao = await db.Notificacoes.SingleAsync();
        Assert.Equal(1, notificacao.DestinatarioId);
        Assert.Equal(TipoNotificacao.MensagemSolicitante, notificacao.Tipo);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Portal(db, Configuracao()).EnviarMensagemAsync(100, 3, "intrometido"));

        // Concluída: não aceita mais mensagens nem anexos.
        var concluido = await db.Cartoes.SingleAsync();
        concluido.ListaId = 12;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Portal(db, Configuracao()).EnviarMensagemAsync(100, 2, "mais uma"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Portal(db, Configuracao()).AnexarAsync(100, 2, [Arquivo("a.txt")]));
    }

    // ---------- Aviso ao solicitante ----------

    [Fact]
    public async Task Notificador_AvisaSoQuandoOStatusPublicoMuda()
    {
        using var db = Banco();
        var agora = DateTime.UtcNow;
        db.Cartoes.Add(new Cartao { Id = 100, Titulo = "Minha", ListaId = 14, SolicitanteId = 2, CriadoEm = agora.AddHours(-1) });
        db.HistoricoAtividades.Add(new HistoricoAtividade { CartaoId = 100, Tipo = TipoHistoricoAtividade.CartaoMovido, ListaDestinoId = 14, OcorridoEm = agora, UsuarioId = 1 });
        await db.SaveChangesAsync();
        var email = new EmailFalso();
        var notificador = Falsos.Notificador(db, email, Configuracao());

        await notificador.AoEntrarNaListaAsync(new EventoAutomacao(TipoGatilho.CartaoEntrouNaLista, 100, 1, 0, [], 14));

        var enviado = Assert.Single(email.Enviados);
        Assert.Equal("bruno@teste.com", enviado.Para);
        Assert.Contains("Em análise", enviado.Assunto);
        Assert.Contains("https://portal.teste/solicitacoes/100", enviado.Corpo);

        // Entrada em lista sem status público (Nova) não gera aviso.
        var cartao = await db.Cartoes.SingleAsync();
        cartao.ListaId = 13;
        db.HistoricoAtividades.Add(new HistoricoAtividade { CartaoId = 100, Tipo = TipoHistoricoAtividade.CartaoMovido, ListaDestinoId = 13, OcorridoEm = agora.AddMinutes(5), UsuarioId = 1 });
        await db.SaveChangesAsync();
        await notificador.AoEntrarNaListaAsync(new EventoAutomacao(TipoGatilho.CartaoEntrouNaLista, 100, 1, 0, [], 13));
        Assert.Single(email.Enviados);
    }

    // ---------- GitHub ----------

    private static string Assinar(string corpo, string segredo)
        => "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(segredo), Encoding.UTF8.GetBytes(corpo))).ToLowerInvariant();

    [Fact]
    public void GitHub_ConfereAssinatura()
    {
        const string corpo = "{\"zen\":\"ok\"}";
        Assert.True(GitHubWebhookService.AssinaturaValida(corpo, Assinar(corpo, "segredo"), "segredo"));
        Assert.False(GitHubWebhookService.AssinaturaValida(corpo, Assinar(corpo, "outro"), "segredo"));
        Assert.False(GitHubWebhookService.AssinaturaValida(corpo, null, "segredo"));
        Assert.False(GitHubWebhookService.AssinaturaValida(corpo, "sha256=zz", "segredo"));
    }

    [Fact]
    public async Task GitHub_PushVinculaCommitsSemDuplicarNaReentrega()
    {
        using var db = Banco();
        db.Cartoes.Add(new Cartao { Id = 7, Titulo = "Cartão", ListaId = 11, CriadoEm = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var fila = new FilaFalsa();
        var servico = new GitHubWebhookService(db, fila, Configuracao(), NullLogger<GitHubWebhookService>.Instance);
        const string corpo = """
            {"ref":"refs/heads/main","repository":{"full_name":"org/app","html_url":"https://github.empresa/org/app"},"pusher":{"name":"ana"},
             "commits":[{"id":"abcdef1234567","message":"KB-7 corrige cálculo\n\ndetalhes","url":"https://github.empresa/org/app/commit/abcdef1","author":{"username":"ana"}},
                        {"id":"9999999999999","message":"KB-999 cartão que não existe","url":"u","author":{"name":"x"}}]}
            """;

        var resultado = await servico.ProcessarAsync("push", corpo);
        await servico.ProcessarAsync("push", corpo);

        Assert.Equal(1, resultado.Vinculos);
        var vinculo = await db.VinculosGit.SingleAsync();
        Assert.Equal(TipoVinculoGit.Commit, vinculo.Tipo);
        Assert.Equal("abcdef1 KB-7 corrige cálculo", vinculo.Titulo);
        Assert.All(fila.Eventos, e => Assert.Equal(TipoGatilho.CommitVinculado, e.Gatilho));
        Assert.Equal(2, fila.Eventos.Count);
    }

    [Fact]
    public async Task GitHub_PullRequestMergeadoAtualizaEstadoEDisparaGatilho()
    {
        using var db = Banco();
        db.Cartoes.Add(new Cartao { Id = 7, Titulo = "Cartão", ListaId = 11, CriadoEm = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var fila = new FilaFalsa();
        var servico = new GitHubWebhookService(db, fila, Configuracao(), NullLogger<GitHubWebhookService>.Instance);
        static string Pr(string acao, bool merged, string estado) => $$$"""
            {"action":"{{{acao}}}","repository":{"full_name":"org/app"},
             "pull_request":{"number":15,"title":"Ajuste do relatório","body":"Resolve KB-7","state":"{{{estado}}}","merged":{{{(merged ? "true" : "false")}}},
                             "html_url":"https://github.empresa/org/app/pull/15","user":{"login":"ana"},"head":{"ref":"feature/x"}}
            }
            """;

        await servico.ProcessarAsync("pull_request", Pr("opened", false, "open"));
        await servico.ProcessarAsync("pull_request", Pr("closed", true, "closed"));

        var vinculo = await db.VinculosGit.SingleAsync();
        Assert.Equal(EstadoPullRequest.Mergeado, vinculo.Estado);
        Assert.Equal([TipoGatilho.PullRequestAberto, TipoGatilho.PullRequestMergeado], fila.Eventos.Select(e => e.Gatilho).ToArray());
    }

    // ---------- Teams ----------

    [Fact]
    public async Task Teams_EnviaCartaoAdaptavelSoParaHttps()
    {
        Assert.False(MensagemTeams.UrlValida("http://inseguro.teste/webhook"));
        Assert.False(MensagemTeams.UrlValida("nao é url"));
        Assert.True(MensagemTeams.UrlValida("https://empresa.webhook.office.com/abc"));

        var http = new HttpFalso();
        var teams = Falsos.Teams(http);
        var mensagem = MensagemTeams.Montar("KB-7 atrasado", "O prazo venceu.", [new MensagemTeams.Fato("Lista", "Em Andamento")],
            "https://kanban.teste/quadros/1", "Abrir cartão", ["item 1"]);
        await teams.EnviarAsync("https://empresa.webhook.office.com/abc", mensagem);

        var (url, corpo) = Assert.Single(http.Requisicoes);
        Assert.Equal("empresa.webhook.office.com", url.Host);
        Assert.Contains("application/vnd.microsoft.card.adaptive", corpo);
        Assert.Contains("KB-7 atrasado", corpo);
        Assert.Contains("Em Andamento", corpo);

        var falha = Falsos.Teams(new HttpFalso(System.Net.HttpStatusCode.BadRequest));
        await Assert.ThrowsAnyAsync<Exception>(() => falha.EnviarAsync("https://empresa.webhook.office.com/abc", mensagem));
    }
}
