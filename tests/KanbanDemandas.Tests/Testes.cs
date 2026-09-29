using System.Net;
using System.Security.Cryptography;
using System.Text;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Core.Regras;
using KanbanDemandas.Infrastructure.Data;
using KanbanDemandas.Infrastructure.Services;
using KanbanDemandas.Portal.Services;
using KanbanDemandas.Web.Configuracao;
using KanbanDemandas.Web.Services.Automacoes;
using KanbanDemandas.Web.Services.Integracoes;
using KanbanDemandas.Web.Services.Portal;
using KanbanDemandas.Web.Services.Relatorios;
using KanbanDemandas.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace KanbanDemandas.Tests
{
    #region CamposBloqueadosTests.cs

    /// <summary>Campos que uma lista não permite (ex.: desenvolvedor e prazo no Backlog).</summary>
    public class CamposBloqueadosTests
    {
        private sealed class UsuarioFixo : IUsuarioAtualProvider
        {
            public Usuario ObterUsuarioAtual() => new() { Id = 1, Nome = "Ana" };
            public int ObterIdUsuarioAtual() => 1;
        }

        private const CamposFixosCartao BloqueadosBacklog = CamposFixosCartao.Desenvolvedor | CamposFixosCartao.Prazo | CamposFixosCartao.DataInicio;

        // Backlog(10, bloqueia desenvolvedor/prazo/início) | A Fazer(11)
        private static KanbanDbContext Banco()
        {
            var db = new KanbanDbContext(new DbContextOptionsBuilder<KanbanDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            db.Usuarios.AddRange(new Usuario { Id = 1, Nome = "Ana" }, new Usuario { Id = 2, Nome = "Bruno" });
            db.Quadros.Add(new Quadro { Id = 1, Nome = "Quadro" });
            db.Listas.AddRange(
                new Lista { Id = 10, QuadroId = 1, Nome = "Backlog", Ordem = 0, EhBacklog = true, CamposFixosBloqueados = BloqueadosBacklog },
                new Lista { Id = 11, QuadroId = 1, Nome = "A Fazer", Ordem = 1 });
            var cartao = new Cartao
            {
                Id = 100, ListaId = 11, Titulo = "Com dono", Prazo = DateTime.UtcNow.AddDays(3), DataInicio = DateTime.UtcNow,
                Estimativa = 5, SolicitanteId = 2, CriadoEm = DateTime.UtcNow
            };
            cartao.Desenvolvedores.Add(new CartaoDesenvolvedor { UsuarioId = 2, Principal = true });
            db.Cartoes.Add(cartao);
            db.SaveChanges();
            return db;
        }

        private static MovimentacaoCartaoService Servico(KanbanDbContext db)
            => new(db, new UsuarioFixo(), new ConfigurationBuilder().Build());

        [Fact]
        public void BloqueadosPreenchidos_SoApontaOQueEstaPreenchido()
        {
            var lista = new Lista { CamposFixosBloqueados = BloqueadosBacklog | CamposFixosCartao.Sistema };
            var cartao = new Cartao { Prazo = DateTime.UtcNow };
            cartao.Desenvolvedores.Add(new CartaoDesenvolvedor { UsuarioId = 1 });

            var preenchidos = RegrasEtapa.BloqueadosPreenchidos(cartao, lista);

            Assert.Equal(CamposFixosCartao.Desenvolvedor | CamposFixosCartao.Prazo, preenchidos);
            Assert.Equal("Desenvolvedor, Prazo", RegrasEtapa.Descrever(preenchidos));
        }

        [Fact]
        public async Task Pendencias_AvisamOQueSeraRemovidoAoEntrar()
        {
            using var db = Banco();

            var pendencias = await Servico(db).ObterPendenciasAsync(100, 10);

            Assert.Equal(BloqueadosBacklog, pendencias!.BloqueadosPreenchidos);
        }

        [Fact]
        public async Task Mover_ParaListaQueBloqueia_RemoveDadosERegistraNoHistorico()
        {
            using var db = Banco();

            var resultado = await Servico(db).MoverAsync(100, 10, null);

            Assert.Equal(BloqueadosBacklog, resultado!.CamposLimpos);
            var cartao = await db.Cartoes.SingleAsync(c => c.Id == 100);
            Assert.Null(cartao.Prazo);
            Assert.Null(cartao.DataInicio);
            Assert.Equal(5, cartao.Estimativa); // não bloqueado: continua
            Assert.Equal(2, cartao.SolicitanteId);
            Assert.Empty(await db.CartaoDesenvolvedores.Where(d => d.CartaoId == 100).ToListAsync());
            Assert.Contains(await db.HistoricoAtividades.ToListAsync(),
                h => h.Tipo == TipoHistoricoAtividade.CampoAlterado && h.Descricao.Contains("Desenvolvedor, Prazo, Data de início"));
        }

        [Fact]
        public async Task Mover_ParaListaSemBloqueio_NaoMexeEmNada()
        {
            using var db = Banco();
            var cartao = await db.Cartoes.SingleAsync();
            cartao.ListaId = 10;
            await db.SaveChangesAsync();
            (await db.Listas.SingleAsync(l => l.Id == 10)).CamposFixosBloqueados = CamposFixosCartao.Nenhum;
            await db.SaveChangesAsync();

            var resultado = await Servico(db).MoverAsync(100, 11, null);

            Assert.Equal(CamposFixosCartao.Nenhum, resultado!.CamposLimpos);
            Assert.NotNull((await db.Cartoes.SingleAsync()).Prazo);
        }

        [Fact]
        public async Task LimparLista_AjustaCartoesQueJaEstavamLa()
        {
            using var db = Banco();
            var cartao = await db.Cartoes.SingleAsync();
            cartao.ListaId = 10; // já estava no Backlog antes da regra existir
            await db.SaveChangesAsync();
            var servico = Servico(db);

            Assert.Equal(1, await servico.ContarComBloqueadosAsync(10));
            Assert.Equal(1, await servico.LimparBloqueadosDaListaAsync(10));
            Assert.Equal(0, await servico.ContarComBloqueadosAsync(10));
        }

        [Fact]
        public async Task SolicitanteDoPortal_NuncaERemovido()
        {
            using var db = Banco();
            (await db.Listas.SingleAsync(l => l.Id == 10)).CamposFixosBloqueados = CamposFixosCartao.Solicitante;
            (await db.Cartoes.SingleAsync()).OrigemPortal = true;
            await db.SaveChangesAsync();

            var resultado = await Servico(db).MoverAsync(100, 10, null);

            Assert.Equal(CamposFixosCartao.Nenhum, resultado!.CamposLimpos);
            Assert.Equal(2, (await db.Cartoes.SingleAsync()).SolicitanteId);
        }
    }

    #endregion

    #region Falsos.cs

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

    #endregion

    #region MotorAutomacoesTests.cs

    public class MotorAutomacoesTests
    {
        private sealed class UsuarioFixo : IUsuarioAtualProvider
        {
            public Usuario ObterUsuarioAtual() => new() { Id = 1, Nome = "Admin" };
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

        private static readonly IConfiguration Configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Automacao:ProfundidadeMaxima"] = "3",
                ["Automacao:PontosPorDia"] = "1",
                ["Aplicacao:UrlBase"] = "https://kanban.teste"
            }).Build();

        // Quadro 1: A Fazer(10) | Em Andamento(11) | Concluído(12). Usuários: Ana(1), Bruno(2), Carla(3).
        private static KanbanDbContext CriarBanco(FilaFalsa? fila = null)
        {
            var construtor = new DbContextOptionsBuilder<KanbanDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString());
            if (fila is not null) construtor.AddInterceptors(new EventosAutomacaoInterceptor(new SessaoUsuario { UsuarioId = 1 }, fila));
            var db = new KanbanDbContext(construtor.Options);
            db.Usuarios.AddRange(
                new Usuario { Id = 1, Nome = "Ana", Email = "ana@teste.com" },
                new Usuario { Id = 2, Nome = "Bruno", Email = "bruno@teste.com" },
                new Usuario { Id = 3, Nome = "Carla", Email = "carla@teste.com", ReceberEmailsAutomacao = false });
            db.Quadros.Add(new Quadro { Id = 1, Nome = "Quadro" });
            db.Listas.AddRange(
                new Lista { Id = 10, QuadroId = 1, Nome = "A Fazer", Ordem = 0 },
                new Lista { Id = 11, QuadroId = 1, Nome = "Em Andamento", Ordem = 1 },
                new Lista { Id = 12, QuadroId = 1, Nome = "Concluído", Ordem = 2 });
            db.SaveChanges();
            return db;
        }

        private static (MotorAutomacoes Motor, EmailFalso Email) Motor(KanbanDbContext db)
        {
            var email = new EmailFalso();
            var movimentacao = new MovimentacaoCartaoService(db, new UsuarioFixo(), Configuracao);
            return (new MotorAutomacoes(db, movimentacao, email, Configuracao, new UrlBaseAplicacao(Configuracao),
                Falsos.Teams(), Falsos.Notificador(db, email, Configuracao), NullLogger<MotorAutomacoes>.Instance), email);
        }

        private static Cartao Cartao(int id, int listaId, string titulo, DateTime? inicio = null, DateTime? prazo = null, int[]? devs = null,
            Prioridade? prioridade = null, DateTime? criadoEm = null, decimal? estimativa = null) => new()
        {
            Id = id, ListaId = listaId, Titulo = titulo, DataInicio = inicio?.ToUniversalTime(), Prazo = prazo?.ToUniversalTime(),
            Prioridade = prioridade, Estimativa = estimativa, CriadoEm = criadoEm ?? DateTime.UtcNow,
            Desenvolvedores = (devs ?? []).Select((d, i) => new CartaoDesenvolvedor { UsuarioId = d, Principal = i == 0 }).ToList()
        };

        private static RegraAutomacao Regra(TipoGatilho gatilho, int? listaId, ParametrosGatilho? gatilhoParametros, List<Condicao>? condicoes,
            params (TipoAcaoAutomacao Tipo, ParametrosAcao Parametros)[] acoes)
        {
            var regra = new RegraAutomacao
            {
                QuadroId = 1, Nome = "Regra de teste", Gatilho = gatilho, ListaId = listaId,
                ParametrosGatilhoJson = gatilhoParametros is null ? null : JsonAutomacao.Serializar(gatilhoParametros),
                CondicoesJson = condicoes is null ? null : JsonAutomacao.Serializar(condicoes)
            };
            for (var i = 0; i < acoes.Length; i++)
                regra.Acoes.Add(new AcaoAutomacao { Tipo = acoes[i].Tipo, Ordem = i, ParametrosJson = JsonAutomacao.Serializar(acoes[i].Parametros) });
            return regra;
        }

        private static EventoAutomacao Evento(TipoGatilho gatilho, int cartaoId, int? listaId = null, int profundidade = 0, IReadOnlyList<int>? cadeia = null)
            => new(gatilho, cartaoId, 1, profundidade, cadeia ?? [], listaId);

        [Fact]
        public async Task EntrarNaLista_ExecutaAcoesERegistraExecucao()
        {
            using var db = CriarBanco();
            db.Cartoes.Add(Cartao(100, 11, "Tarefa"));
            db.RegrasAutomacao.Add(Regra(TipoGatilho.CartaoEntrouNaLista, 11, null, null,
                (TipoAcaoAutomacao.DefinirPrioridade, new ParametrosAcao { Prioridade = Prioridade.Alta }),
                (TipoAcaoAutomacao.CriarAlerta, new ParametrosAcao { Texto = "Começou: {titulo}", Severidade = SeveridadeAlerta.Info })));
            await db.SaveChangesAsync();

            await Motor(db).Motor.ProcessarEventoAsync(Evento(TipoGatilho.CartaoEntrouNaLista, 100, 11));

            Assert.Equal(Prioridade.Alta, (await db.Cartoes.FindAsync(100))!.Prioridade);
            Assert.Single(await db.AlertasCartao.Where(a => a.CartaoId == 100 && a.Mensagem == "Começou: Tarefa").ToListAsync());
            var execucao = await db.ExecucoesAutomacao.SingleAsync();
            Assert.Equal(StatusExecucaoAutomacao.Sucesso, execucao.Status);
            Assert.NotNull(execucao.DesfazerJson);
        }

        [Fact]
        public async Task EntrarEmOutraLista_NaoDispara()
        {
            using var db = CriarBanco();
            db.Cartoes.Add(Cartao(100, 12, "Tarefa"));
            db.RegrasAutomacao.Add(Regra(TipoGatilho.CartaoEntrouNaLista, 11, null, null,
                (TipoAcaoAutomacao.DefinirPrioridade, new ParametrosAcao { Prioridade = Prioridade.Alta })));
            await db.SaveChangesAsync();

            await Motor(db).Motor.ProcessarEventoAsync(Evento(TipoGatilho.CartaoEntrouNaLista, 100, 12));

            Assert.Null((await db.Cartoes.FindAsync(100))!.Prioridade);
            Assert.Empty(await db.ExecucoesAutomacao.ToListAsync());
        }

        [Fact]
        public async Task Condicoes_FiltramCartoes()
        {
            using var db = CriarBanco();
            db.Cartoes.AddRange(Cartao(100, 11, "Com dev", devs: [1]), Cartao(101, 11, "Sem dev"));
            db.RegrasAutomacao.Add(Regra(TipoGatilho.CartaoCriado, null, null,
                [new Condicao { Tipo = TipoCondicao.Desenvolvedor, Operador = OperadorCondicao.Vazio }],
                (TipoAcaoAutomacao.AtribuirDesenvolvedor, new ParametrosAcao { ModoAtribuicao = ModoAtribuicao.UsuarioEspecifico, UsuarioId = 2 })));
            await db.SaveChangesAsync();
            var (motor, _) = Motor(db);

            await motor.ProcessarEventoAsync(Evento(TipoGatilho.CartaoCriado, 100));
            await motor.ProcessarEventoAsync(Evento(TipoGatilho.CartaoCriado, 101));

            Assert.DoesNotContain(await db.CartaoDesenvolvedores.Where(d => d.CartaoId == 100).ToListAsync(), d => d.UsuarioId == 2);
            Assert.Contains(await db.CartaoDesenvolvedores.Where(d => d.CartaoId == 101).ToListAsync(), d => d.UsuarioId == 2 && d.Principal);
        }

        [Fact]
        public async Task Atribuir_EmListaQueNaoPermiteDesenvolvedor_FicaComoErro()
        {
            using var db = CriarBanco();
            (await db.Listas.FindAsync(10))!.CamposFixosBloqueados = CamposFixosCartao.Desenvolvedor;
            db.Cartoes.Add(Cartao(100, 10, "No backlog"));
            db.RegrasAutomacao.Add(Regra(TipoGatilho.CartaoCriado, null, null, null,
                (TipoAcaoAutomacao.AtribuirDesenvolvedor, new ParametrosAcao { ModoAtribuicao = ModoAtribuicao.UsuarioEspecifico, UsuarioId = 2 })));
            await db.SaveChangesAsync();

            await Motor(db).Motor.ProcessarEventoAsync(Evento(TipoGatilho.CartaoCriado, 100));

            Assert.Empty(await db.CartaoDesenvolvedores.Where(d => d.CartaoId == 100).ToListAsync());
            var execucao = await db.ExecucoesAutomacao.SingleAsync();
            Assert.NotEqual(StatusExecucaoAutomacao.Sucesso, execucao.Status);
            Assert.Contains("não permite", execucao.Erro ?? execucao.Resumo);
        }

        [Fact]
        public async Task AntiLoop_ProfundidadeExcedidaEMesmaRegraNaCadeia_SaoIgnoradas()
        {
            using var db = CriarBanco();
            db.Cartoes.Add(Cartao(100, 11, "Tarefa"));
            var regra = Regra(TipoGatilho.CartaoEntrouNaLista, 11, null, null,
                (TipoAcaoAutomacao.DefinirPrioridade, new ParametrosAcao { Prioridade = Prioridade.Alta }));
            db.RegrasAutomacao.Add(regra);
            await db.SaveChangesAsync();
            var (motor, _) = Motor(db);

            await motor.ProcessarEventoAsync(Evento(TipoGatilho.CartaoEntrouNaLista, 100, 11, profundidade: 4));
            await motor.ProcessarEventoAsync(Evento(TipoGatilho.CartaoEntrouNaLista, 100, 11, profundidade: 1, cadeia: [regra.Id]));

            Assert.Null((await db.Cartoes.FindAsync(100))!.Prioridade);
        }

        [Fact]
        public async Task ConflitoDatas_AplicarEmpurraOMaisRecenteParaDepoisDoOutro()
        {
            using var db = CriarBanco();
            var segunda = ProximaSegunda();
            db.Cartoes.AddRange(
                Cartao(100, 11, "Antigo", segunda, segunda.AddDays(2), [1], criadoEm: DateTime.UtcNow.AddDays(-10)),
                Cartao(101, 10, "Novo", segunda.AddDays(1), segunda.AddDays(1), [1], criadoEm: DateTime.UtcNow));
            db.RegrasAutomacao.Add(Regra(TipoGatilho.ConflitoDatas, null, null, null,
                (TipoAcaoAutomacao.ResolverConflitoDatas, new ParametrosAcao { Estrategia = EstrategiaConflito.MaisRecente, Resolucao = ResolucaoConflito.EmpurrarDatas, Modo = ModoAplicacao.Aplicar })));
            await db.SaveChangesAsync();

            await Motor(db).Motor.ProcessarEventoAsync(Evento(TipoGatilho.ConflitoDatas, 101));

            var novo = (await db.Cartoes.FindAsync(101))!;
            Assert.Equal(segunda.AddDays(3).Date, novo.DataInicio!.Value.ToLocalTime().Date);
            Assert.Equal(segunda.AddDays(3).Date, novo.Prazo!.Value.ToLocalTime().Date);
            Assert.Equal(segunda.Date, (await db.Cartoes.FindAsync(100))!.DataInicio!.Value.ToLocalTime().Date);
        }

        [Fact]
        public async Task ConflitoDatas_SugerirCriaSugestaoQueAoAprovarAplica()
        {
            using var db = CriarBanco();
            var segunda = ProximaSegunda();
            db.Cartoes.AddRange(
                Cartao(100, 11, "Antigo", segunda, segunda.AddDays(1), [1], criadoEm: DateTime.UtcNow.AddDays(-10)),
                Cartao(101, 10, "Novo", segunda, segunda, [1], criadoEm: DateTime.UtcNow));
            db.RegrasAutomacao.Add(Regra(TipoGatilho.ConflitoDatas, null, null, null,
                (TipoAcaoAutomacao.ResolverConflitoDatas, new ParametrosAcao { Estrategia = EstrategiaConflito.MaisRecente, Resolucao = ResolucaoConflito.EmpurrarDatas, Modo = ModoAplicacao.Sugerir, ListaId = 10 })));
            await db.SaveChangesAsync();
            var (motor, _) = Motor(db);

            await motor.ProcessarEventoAsync(Evento(TipoGatilho.ConflitoDatas, 101));

            var sugestao = await db.SugestoesAutomacao.SingleAsync();
            Assert.Equal(101, sugestao.CartaoId);
            Assert.Equal(segunda.Date, (await db.Cartoes.AsNoTracking().FirstAsync(c => c.Id == 101)).Prazo!.Value.ToLocalTime().Date);

            await motor.AprovarSugestaoAsync(sugestao.Id, 1);

            var novo = await db.Cartoes.AsNoTracking().FirstAsync(c => c.Id == 101);
            Assert.Equal(segunda.AddDays(2).Date, novo.Prazo!.Value.ToLocalTime().Date);
            Assert.Equal(StatusSugestao.Aprovada, (await db.SugestoesAutomacao.AsNoTracking().SingleAsync()).Status);
        }

        [Fact]
        public async Task Cedente_RespeitaEstrategia()
        {
            var alta = Cartao(1, 11, "Alta", prioridade: Prioridade.Alta, criadoEm: DateTime.UtcNow);
            var baixa = Cartao(2, 11, "Baixa", prioridade: Prioridade.Baixa, criadoEm: DateTime.UtcNow.AddDays(-5));
            Assert.Same(baixa, MotorAutomacoes.Cedente(alta, baixa, EstrategiaConflito.MenorPrioridade));
            Assert.Same(alta, MotorAutomacoes.Cedente(alta, baixa, EstrategiaConflito.MaisRecente));
            Assert.Same(baixa, MotorAutomacoes.Cedente(alta, baixa, EstrategiaConflito.MaisAntigo));
        }

        [Fact]
        public async Task PrazoProximo_ExecutaUmaVezPorPrazo()
        {
            using var db = CriarBanco();
            db.Cartoes.Add(Cartao(100, 11, "Vence logo", prazo: DateTime.Today.AddDays(1), devs: [1]));
            db.RegrasAutomacao.Add(Regra(TipoGatilho.PrazoProximo, null, new ParametrosGatilho { Dias = 2 }, null,
                (TipoAcaoAutomacao.NotificarResponsavel, new ParametrosAcao { Destinatarios = DestinatariosAutomacao.Desenvolvedores, Texto = "{titulo} vence {prazo}" })));
            await db.SaveChangesAsync();
            var (motor, _) = Motor(db);

            await motor.ProcessarGatilhosDeTempoAsync(DateTime.Now);
            await motor.ProcessarGatilhosDeTempoAsync(DateTime.Now);

            Assert.Single(await db.Notificacoes.Where(n => n.DestinatarioId == 1 && n.Tipo == TipoNotificacao.AutomacaoRegra).ToListAsync());
            Assert.Single(await db.ExecucoesAutomacao.ToListAsync());
        }

        [Fact]
        public async Task ParadoNaLista_DisparaQuandoCompletaOsDias()
        {
            using var db = CriarBanco();
            db.Cartoes.AddRange(Cartao(100, 11, "Parado", devs: [1]), Cartao(101, 11, "Recente", devs: [1]));
            db.HistoricoAtividades.AddRange(
                new HistoricoAtividade { CartaoId = 100, Tipo = TipoHistoricoAtividade.CartaoMovido, ListaDestinoId = 11, UsuarioId = 1, OcorridoEm = DateTime.UtcNow.AddDays(-6), Descricao = "x" },
                new HistoricoAtividade { CartaoId = 101, Tipo = TipoHistoricoAtividade.CartaoMovido, ListaDestinoId = 11, UsuarioId = 1, OcorridoEm = DateTime.UtcNow.AddDays(-1), Descricao = "x" });
            db.RegrasAutomacao.Add(Regra(TipoGatilho.ParadoNaLista, 11, new ParametrosGatilho { Dias = 5 }, null,
                (TipoAcaoAutomacao.CriarAlerta, new ParametrosAcao { Texto = "Parado há {dias_na_lista} dias" })));
            await db.SaveChangesAsync();

            await Motor(db).Motor.ProcessarGatilhosDeTempoAsync(DateTime.Now);

            var alerta = await db.AlertasCartao.SingleAsync();
            Assert.Equal(100, alerta.CartaoId);
            Assert.Equal("Parado há 6 dias", alerta.Mensagem);
        }

        [Fact]
        public async Task Desfazer_ReverteAlteracoes()
        {
            using var db = CriarBanco();
            db.Cartoes.Add(Cartao(100, 11, "Tarefa", prioridade: Prioridade.Baixa));
            db.CartaoEtiquetas.Add(new CartaoEtiqueta { CartaoId = 100, EtiquetaId = 50 });
            db.Etiquetas.Add(new Etiqueta { Id = 50, QuadroId = 1, Nome = "Bug", Cor = "#f00" });
            db.RegrasAutomacao.Add(Regra(TipoGatilho.CartaoEntrouNaLista, 11, null, null,
                (TipoAcaoAutomacao.DefinirPrioridade, new ParametrosAcao { AumentarUmNivel = true }),
                (TipoAcaoAutomacao.RemoverEtiqueta, new ParametrosAcao { EtiquetaId = 50 }),
                (TipoAcaoAutomacao.MoverCartao, new ParametrosAcao { ListaId = 12 })));
            await db.SaveChangesAsync();
            var (motor, _) = Motor(db);
            await motor.ProcessarEventoAsync(Evento(TipoGatilho.CartaoEntrouNaLista, 100, 11));
            var cartao = await db.Cartoes.AsNoTracking().FirstAsync(c => c.Id == 100);
            Assert.Equal((Prioridade.Media, 12), (cartao.Prioridade!.Value, cartao.ListaId));

            await motor.DesfazerAsync((await db.ExecucoesAutomacao.SingleAsync()).Id, 1);

            cartao = await db.Cartoes.AsNoTracking().FirstAsync(c => c.Id == 100);
            Assert.Equal(Prioridade.Baixa, cartao.Prioridade);
            Assert.Equal(11, cartao.ListaId);
            Assert.True(await db.CartaoEtiquetas.AnyAsync(e => e.CartaoId == 100 && e.EtiquetaId == 50));
            Assert.Equal(StatusExecucaoAutomacao.Desfeita, (await db.ExecucoesAutomacao.AsNoTracking().SingleAsync()).Status);
        }

        [Fact]
        public async Task MenorCarga_EscolheQuemTemMenosPontosAtivos()
        {
            using var db = CriarBanco();
            db.Cartoes.AddRange(
                Cartao(100, 11, "A1", devs: [1], estimativa: 8),
                Cartao(101, 11, "B1", devs: [2], estimativa: 2),
                Cartao(102, 12, "B concluído", devs: [2], estimativa: 50),
                Cartao(103, 10, "Novo"));
            await db.SaveChangesAsync();
            var foto = await FotoQuadro.CarregarAsync(db, 1);

            Assert.Equal(2, MotorAutomacoes.MenorCarga(103, new ParametrosAcao { UsuarioIds = [1, 2] }, foto));
        }

        [Fact]
        public async Task EnviarEmail_RespeitaPreferenciaERegistraNoCartao()
        {
            using var db = CriarBanco();
            db.Cartoes.Add(Cartao(100, 11, "Tarefa", prazo: DateTime.Today.AddDays(3), devs: [1, 3]));
            db.RegrasAutomacao.Add(Regra(TipoGatilho.CartaoEntrouNaLista, 11, null, null,
                (TipoAcaoAutomacao.EnviarEmail, new ParametrosAcao { Destinatarios = DestinatariosAutomacao.Desenvolvedores, Assunto = "Cobrança: {titulo}", Texto = "Veja {link}" })));
            await db.SaveChangesAsync();
            var (motor, email) = Motor(db);

            await motor.ProcessarEventoAsync(Evento(TipoGatilho.CartaoEntrouNaLista, 100, 11));

            var enviado = Assert.Single(email.Enviados);
            Assert.Equal("ana@teste.com", enviado.Para);
            Assert.Equal("Cobrança: Tarefa", enviado.Assunto);
            Assert.Contains("https://kanban.teste/quadros/1?cartao=100", enviado.Corpo);
            Assert.True(await db.EmailsCartao.AnyAsync(e => e.CartaoId == 100 && e.Enviado));
            Assert.Equal(1, (await db.ExecucoesAutomacao.SingleAsync()).EmailsEnviados);
        }

        [Fact]
        public async Task Agendado_EnviaResumoUmaVezPorDia()
        {
            using var db = CriarBanco();
            db.Cartoes.AddRange(
                Cartao(100, 11, "Atrasado", prazo: DateTime.Today.AddDays(-2), devs: [1]),
                Cartao(101, 10, "Do Bruno", devs: [2]));
            db.RegrasAutomacao.Add(Regra(TipoGatilho.Agendado, null, new ParametrosGatilho { Horario = "00:00", DiasSemana = [] }, null,
                (TipoAcaoAutomacao.EnviarResumo, new ParametrosAcao { Destinatarios = DestinatariosAutomacao.Desenvolvedores })));
            await db.SaveChangesAsync();
            var (motor, email) = Motor(db);

            await motor.ProcessarGatilhosDeTempoAsync(DateTime.Now);
            await motor.ProcessarGatilhosDeTempoAsync(DateTime.Now);

            Assert.Equal(2, email.Enviados.Count);
            Assert.Contains(email.Enviados, e => e.Para == "ana@teste.com" && e.Corpo.Contains("Atrasados"));
        }

        [Fact]
        public async Task Simular_ListaCartoesSemAlterarNada()
        {
            using var db = CriarBanco();
            db.Cartoes.AddRange(Cartao(100, 11, "Alta", prioridade: Prioridade.Alta), Cartao(101, 11, "Baixa", prioridade: Prioridade.Baixa));
            var regra = Regra(TipoGatilho.CartaoEntrouNaLista, 11, null,
                [new Condicao { Tipo = TipoCondicao.Prioridade, Operador = OperadorCondicao.MaiorOuIgual, Ids = [(int)Prioridade.Alta] }],
                (TipoAcaoAutomacao.CriarAlerta, new ParametrosAcao { Texto = "Atenção" }));
            db.RegrasAutomacao.Add(regra);
            await db.SaveChangesAsync();

            var alvos = await Motor(db).Motor.SimularAsync(regra.Id);

            var alvo = Assert.Single(alvos);
            Assert.Equal(100, alvo.CartaoId);
            Assert.Empty(await db.AlertasCartao.ToListAsync());
        }

        [Fact]
        public async Task Interceptor_MovimentacaoGeraEventosDeEntradaESaida()
        {
            var fila = new FilaFalsa();
            using var db = CriarBanco(fila);
            db.Cartoes.Add(Cartao(100, 10, "Tarefa"));
            await db.SaveChangesAsync();
            fila.Eventos.Clear();

            await new MovimentacaoCartaoService(db, new UsuarioFixo(), Configuracao).MoverAsync(100, 11, null);

            Assert.Contains(fila.Eventos, e => e.Gatilho == TipoGatilho.CartaoEntrouNaLista && e.CartaoId == 100 && e.ListaId == 11);
            Assert.Contains(fila.Eventos, e => e.Gatilho == TipoGatilho.CartaoSaiuDaLista && e.CartaoId == 100 && e.ListaId == 10);
        }

        [Fact]
        public async Task Interceptor_EventosDentroDeAutomacaoCarregamCadeia()
        {
            var fila = new FilaFalsa();
            using var db = CriarBanco(fila);
            db.Cartoes.Add(Cartao(100, 10, "Tarefa"));
            await db.SaveChangesAsync();
            fila.Eventos.Clear();

            using (ContextoAutomacao.Entrar(2, [7]))
            {
                (await db.Cartoes.FindAsync(100))!.Prioridade = Prioridade.Alta;
                await db.SaveChangesAsync();
            }

            var evento = Assert.Single(fila.Eventos, e => e.Gatilho == TipoGatilho.CampoAlterado);
            Assert.Equal(CampoMonitorado.Prioridade, evento.Campo);
            Assert.Equal(2, evento.Profundidade);
            Assert.Equal([7], evento.RegrasNaCadeia);
        }

        [Fact]
        public void DiasUteis_PulaFimDeSemana()
        {
            var sexta = new DateTime(2026, 10, 2);
            Assert.Equal(new DateTime(2026, 10, 5), DiasUteis.Adicionar(sexta, 1));
            Assert.Equal(5, DiasUteis.Contar(sexta, new DateTime(2026, 10, 8)));
        }

        private static DateTime ProximaSegunda()
        {
            var dia = DateTime.Today.AddDays(7);
            while (dia.DayOfWeek != DayOfWeek.Monday) dia = dia.AddDays(1);
            return dia;
        }
    }

    #endregion

    #region MovimentacaoCartaoServiceTests.cs

    public class MovimentacaoCartaoServiceTests
    {
        private sealed class UsuarioFixo : IUsuarioAtualProvider
        {
            public Usuario ObterUsuarioAtual() => new() { Id = 1, Nome = "Teste" };
            public int ObterIdUsuarioAtual() => 1;
        }

        // Quadro 100: Em Andamento(101) | Homologação(102) > [Code Review(103), Testes(104)] | Concluído(105)
        private static KanbanDbContext CriarBanco()
        {
            var options = new DbContextOptionsBuilder<KanbanDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new KanbanDbContext(options);
            db.Quadros.Add(new Quadro { Id = 100, Nome = "Quadro" });
            db.Listas.AddRange(
                new Lista { Id = 101, QuadroId = 100, Nome = "Em Andamento", Ordem = 0 },
                new Lista { Id = 102, QuadroId = 100, Nome = "Homologação", Ordem = 1 },
                new Lista { Id = 103, QuadroId = 100, Nome = "Code Review", Ordem = 0, ListaPaiId = 102 },
                new Lista { Id = 104, QuadroId = 100, Nome = "Testes", Ordem = 1, ListaPaiId = 102 },
                new Lista { Id = 105, QuadroId = 100, Nome = "Concluído", Ordem = 2 });
            db.Cartoes.AddRange(
                new Cartao { Id = 201, ListaId = 101, Titulo = "A", Ordem = 0 },
                new Cartao { Id = 202, ListaId = 101, Titulo = "B", Ordem = 1 },
                new Cartao { Id = 203, ListaId = 105, Titulo = "C", Ordem = 0 });
            db.SaveChanges();
            return db;
        }

        private static readonly IConfiguration Configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Movimentacao:JanelaCorrecaoMinutos"] = "10" })
            .Build();

        private static MovimentacaoCartaoService Servico(KanbanDbContext db)
            => new(db, new UsuarioFixo(), Configuracao);

        private static async Task<string?> ValorAtual(KanbanDbContext db, int cartaoId, int campoId)
            => CamposCartao.ValorMaisRecente(await db.ValoresCampoCartao.Where(v => v.CartaoId == cartaoId).ToListAsync(), campoId);

        [Fact]
        public async Task Mover_ComCampoDataEntrada_PreencheEDefineDataInicio()
        {
            using var db = CriarBanco();
            db.DefinicoesCampo.Add(new DefinicaoCampo
            {
                Id = 501, ListaId = 105, Nome = "Início", Tipo = TipoCampo.Data,
                Preenchimento = PreenchimentoAutomatico.DataEntrada, DefineDataInicioCartao = true
            });
            await db.SaveChangesAsync();

            await Servico(db).MoverAsync(201, 105, null);

            Assert.Equal(DateTime.Now.ToString("yyyy-MM-dd"), await ValorAtual(db, 201, 501));
            Assert.Equal(DateTime.Today.ToUniversalTime(), (await db.Cartoes.FindAsync(201))!.DataInicio);
        }

        [Fact]
        public async Task VaiEVoltaDentroDaJanela_EstornaMovimentoEDesfazPreenchimentos()
        {
            using var db = CriarBanco();
            db.DefinicoesCampo.Add(new DefinicaoCampo
            {
                Id = 501, ListaId = 105, Nome = "Início", Tipo = TipoCampo.Data,
                Preenchimento = PreenchimentoAutomatico.DataEntrada, DefineDataInicioCartao = true
            });
            await db.SaveChangesAsync();

            await Servico(db).MoverAsync(201, 105, null);
            var volta = await Servico(db).MoverAsync(201, 101, null);

            Assert.True(volta!.Estornado);
            Assert.Equal(101, (await db.Cartoes.FindAsync(201))!.ListaId);
            Assert.Null((await db.Cartoes.FindAsync(201))!.DataInicio);
            Assert.Null(await ValorAtual(db, 201, 501));
            Assert.False(await db.HistoricoAtividades.AnyAsync(h => h.CartaoId == 201 && h.Tipo == TipoHistoricoAtividade.CartaoMovido));
            Assert.True(await db.HistoricoAtividades.IgnoreQueryFilters().AnyAsync(h => h.CartaoId == 201 && h.Estornado));
        }

        [Fact]
        public async Task VoltarDepoisDaJanela_ContaComoMovimentoNormal()
        {
            using var db = CriarBanco();
            await Servico(db).MoverAsync(201, 105, null);
            var ida = await db.HistoricoAtividades.SingleAsync(h => h.CartaoId == 201 && h.Tipo == TipoHistoricoAtividade.CartaoMovido);
            ida.OcorridoEm = DateTime.UtcNow.AddHours(-1);
            await db.SaveChangesAsync();

            var volta = await Servico(db).MoverAsync(201, 101, null);

            Assert.False(volta!.Estornado);
            Assert.Equal(2, await db.HistoricoAtividades.CountAsync(h => h.CartaoId == 201 && h.Tipo == TipoHistoricoAtividade.CartaoMovido));
        }

        [Fact]
        public async Task ReentradaComManterPrimeiro_PreservaDataOriginal()
        {
            using var db = CriarBanco();
            db.DefinicoesCampo.Add(new DefinicaoCampo
            {
                Id = 501, ListaId = 105, Nome = "Iniciado em", Tipo = TipoCampo.Data,
                Preenchimento = PreenchimentoAutomatico.DataEntrada, RegraReentrada = RegraReentrada.ManterPrimeiro
            });
            db.ValoresCampoCartao.Add(new ValorCampoCartao { CartaoId = 201, DefinicaoCampoId = 501, Valor = "2026-01-10", NumeroEntradaNaLista = 1 });
            await db.SaveChangesAsync();

            await Servico(db).MoverAsync(201, 105, null);

            Assert.Equal("2026-01-10", await ValorAtual(db, 201, 501));
        }

        [Fact]
        public async Task ReentradaComNovoRegistro_CriaNovaOcorrencia()
        {
            using var db = CriarBanco();
            db.DefinicoesCampo.Add(new DefinicaoCampo
            {
                Id = 501, ListaId = 105, Nome = "Entrou em", Tipo = TipoCampo.Data,
                Preenchimento = PreenchimentoAutomatico.DataEntrada, RegraReentrada = RegraReentrada.NovoRegistro
            });
            db.ValoresCampoCartao.Add(new ValorCampoCartao { CartaoId = 201, DefinicaoCampoId = 501, Valor = "2026-01-10", NumeroEntradaNaLista = 1 });
            await db.SaveChangesAsync();

            await Servico(db).MoverAsync(201, 105, null);

            Assert.Equal(2, await db.ValoresCampoCartao.CountAsync(v => v.CartaoId == 201 && v.DefinicaoCampoId == 501));
            Assert.Equal(DateTime.Now.ToString("yyyy-MM-dd"), await ValorAtual(db, 201, 501));
        }

        [Fact]
        public async Task Pendencias_ListaQueExigeDesenvolvedor_ApontaFaltaEBloqueio()
        {
            using var db = CriarBanco();
            var concluido = (await db.Listas.FindAsync(105))!;
            concluido.CamposFixosExigidos = CamposFixosCartao.Desenvolvedor | CamposFixosCartao.Prazo;
            concluido.BloquearEntradaComPendencias = true;
            await db.SaveChangesAsync();

            var pendencias = await Servico(db).ObterPendenciasAsync(201, 105);

            Assert.True(pendencias!.Bloqueia);
            Assert.Equal(CamposFixosCartao.Desenvolvedor | CamposFixosCartao.Prazo, pendencias.FixosFaltando);
            Assert.True(pendencias.TemObrigatoriasFaltando);
        }

        [Fact]
        public async Task AplicarDadosEtapa_AtribuiDesenvolvedorEPrazo()
        {
            using var db = CriarBanco();
            var servico = new ValoresCampoService(db, new UsuarioFixo());

            await servico.AplicarDadosEtapaAsync(201, 101, new DadosEtapa { DesenvolvedorIds = [7], Prazo = new DateTime(2026, 10, 1) });

            var cartao = await db.Cartoes.Include(c => c.Desenvolvedores).FirstAsync(c => c.Id == 201);
            Assert.Single(cartao.Desenvolvedores, d => d.UsuarioId == 7 && d.Principal);
            Assert.NotNull(cartao.Prazo);
            Assert.True(await db.Notificacoes.AnyAsync(n => n.DestinatarioId == 7 && n.Tipo == TipoNotificacao.AtribuicaoCartao));
        }

        [Fact]
        public async Task Mover_ParaOutraLista_AtualizaListaOrdemEHistorico()
        {
            using var db = CriarBanco();
            var resultado = await Servico(db).MoverAsync(201, 105, 203);

            Assert.NotNull(resultado);
            Assert.True(resultado.MudouDeLista);
            var cartoesConcluido = await db.Cartoes.Where(c => c.ListaId == 105).OrderBy(c => c.Ordem).Select(c => c.Id).ToListAsync();
            Assert.Equal([201, 203], cartoesConcluido);
            Assert.Equal(0, (await db.Cartoes.FindAsync(202))!.Ordem);
            Assert.True(await db.HistoricoAtividades.AnyAsync(h => h.CartaoId == 201 && h.Tipo == TipoHistoricoAtividade.CartaoMovido
                && h.ListaOrigemId == 101 && h.ListaDestinoId == 105));
        }

        [Fact]
        public async Task Mover_ParaListaGrupo_VaiParaPrimeiraSublista()
        {
            using var db = CriarBanco();
            var resultado = await Servico(db).MoverAsync(201, 102, null);

            Assert.Equal(103, resultado!.ListaDestinoId);
            Assert.Equal(103, (await db.Cartoes.FindAsync(201))!.ListaId);
        }

        [Fact]
        public async Task Mover_ParaListaDeOutroQuadro_EhIgnorado()
        {
            using var db = CriarBanco();
            db.Quadros.Add(new Quadro { Id = 900, Nome = "Outro" });
            db.Listas.Add(new Lista { Id = 901, QuadroId = 900, Nome = "Fora" });
            await db.SaveChangesAsync();

            Assert.Null(await Servico(db).MoverAsync(201, 901, null));
            Assert.Equal(101, (await db.Cartoes.FindAsync(201))!.ListaId);
        }

        [Fact]
        public async Task Mover_ComCaminhoConfigurado_AvisaQuandoPulaEtapa()
        {
            using var db = CriarBanco();
            foreach (var (id, ordem) in new[] { (101, 1), (103, 2), (104, 3), (105, 4) })
                (await db.Listas.FindAsync(id))!.OrdemFluxo = ordem;
            await db.SaveChangesAsync();

            var resultado = await Servico(db).MoverAsync(201, 105, null);
            Assert.NotNull(resultado!.AvisoCaminho);
            Assert.Equal(105, (await db.Cartoes.FindAsync(201))!.ListaId);
        }

        [Fact]
        public async Task Mover_CartaoFilhoParaConcluido_ConcluiItemDeTarefaDeOrigem()
        {
            using var db = CriarBanco();
            db.ItensTarefa.Add(new ItemTarefa { Id = 301, CartaoId = 202, Titulo = "Item", CartaoPromovidoId = 201 });
            await db.SaveChangesAsync();

            await Servico(db).MoverAsync(201, 105, null);
            Assert.True((await db.ItensTarefa.FindAsync(301))!.Concluido);

            await Servico(db).MoverAsync(201, 101, null);
            Assert.False((await db.ItensTarefa.FindAsync(301))!.Concluido);
        }

        [Fact]
        public async Task Mover_ParaListaComCampoObrigatorioVazio_NotificaDesenvolvedores()
        {
            using var db = CriarBanco();
            db.DefinicoesCampo.Add(new DefinicaoCampo { Id = 401, ListaId = 105, Nome = "Data homologado", Tipo = TipoCampo.Data, Obrigatorio = true });
            db.CartaoDesenvolvedores.Add(new CartaoDesenvolvedor { CartaoId = 201, UsuarioId = 7, Principal = true });
            await db.SaveChangesAsync();

            await Servico(db).MoverAsync(201, 105, null);
            Assert.True(await db.Notificacoes.AnyAsync(n => n.DestinatarioId == 7 && n.Tipo == TipoNotificacao.CampoPendente));
        }

        [Fact]
        public async Task Excluir_MarcaComoExcluidoEReordenaLista()
        {
            using var db = CriarBanco();
            Assert.True(await Servico(db).ExcluirAsync(201));

            var cartao = await db.Cartoes.IgnoreQueryFilters().FirstAsync(c => c.Id == 201);
            Assert.True(cartao.Excluido);
            Assert.Equal(1, cartao.ExcluidoPorId);
            Assert.Equal(0, (await db.Cartoes.FindAsync(202))!.Ordem);
        }
    }

    #endregion

    #region PortalIntegracoesTests.cs

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

    #endregion

    #region RegrasQuadroTests.cs

    public class RegrasQuadroTests
    {
        // Quadro de exemplo: Backlog(1) | Em Andamento(2) | Homologação(3) > [Code Review(4), Testes(5)] | Concluído(6)
        private static List<Lista> Listas() =>
        [
            new() { Id = 1, Nome = "Backlog", Ordem = 0 },
            new() { Id = 2, Nome = "Em Andamento", Ordem = 1 },
            new() { Id = 3, Nome = "Homologação", Ordem = 2 },
            new() { Id = 4, Nome = "Code Review", Ordem = 0, ListaPaiId = 3 },
            new() { Id = 5, Nome = "Testes", Ordem = 1, ListaPaiId = 3 },
            new() { Id = 6, Nome = "Concluído", Ordem = 3 },
        ];

        private static List<Lista> ListasComCaminho()
        {
            var listas = Listas();
            listas.First(l => l.Id == 1).OrdemFluxo = 1;
            listas.First(l => l.Id == 2).OrdemFluxo = 2;
            listas.First(l => l.Id == 4).OrdemFluxo = 3;
            listas.First(l => l.Id == 5).OrdemFluxo = 4;
            listas.First(l => l.Id == 6).OrdemFluxo = 5;
            return listas;
        }

        [Fact]
        public void PrimeiraFolha_DeListaGrupo_RetornaPrimeiraSublista()
            => Assert.Equal(4, ListasQuadro.PrimeiraFolha(3, Listas()));

        [Fact]
        public void PrimeiraFolha_DeListaSemSublistas_RetornaElaMesma()
            => Assert.Equal(2, ListasQuadro.PrimeiraFolha(2, Listas()));

        [Fact]
        public void FolhasEmOrdem_ExcluiGruposEMantemOrdemVisual()
            => Assert.Equal([1, 2, 4, 5, 6], ListasQuadro.FolhasEmOrdem(Listas()).Select(l => l.Id));

        [Fact]
        public void NomeCompleto_IncluiListaPai()
        {
            var listas = Listas();
            Assert.Equal("Homologação › Code Review", ListasQuadro.NomeCompleto(listas.First(l => l.Id == 4), listas));
        }

        [Fact]
        public void FluxoEfetivo_DeGrupo_UsaMenorEtapaDasSublistas()
            => Assert.Equal(3, CaminhoQuadro.FluxoEfetivo(3, ListasComCaminho()));

        [Fact]
        public void RespeitaCaminho_BloqueiaGrupoAntesDeEtapaAnterior()
        {
            var listas = ListasComCaminho();
            Assert.True(CaminhoQuadro.RespeitaCaminho([1, 2, 3, 6], listas));
            Assert.False(CaminhoQuadro.RespeitaCaminho([1, 3, 2, 6], listas));
        }

        [Fact]
        public void RespeitaCaminho_IgnoraListasForaDoCaminho()
        {
            var listas = ListasComCaminho();
            listas.Add(new Lista { Id = 7, Nome = "Arquivo", Ordem = 4 });
            Assert.True(CaminhoQuadro.RespeitaCaminho([7, 1, 2, 3, 6], listas));
        }

        [Fact]
        public void OrdenarPeloCaminho_ReposicionaApenasListasDoCaminho()
        {
            var listas = ListasComCaminho();
            listas.Add(new Lista { Id = 7, Nome = "Arquivo" });
            Assert.Equal([1, 7, 2, 3, 6], CaminhoQuadro.OrdenarPeloCaminho([3, 7, 1, 2, 6], listas));
        }

        [Fact]
        public void AvisoMovimentacao_ProximaEtapa_NaoAvisa()
            => Assert.Null(CaminhoQuadro.AvisoMovimentacao(2, 4, ListasComCaminho()));

        [Fact]
        public void AvisoMovimentacao_PulandoEtapa_Avisa()
            => Assert.Contains("Code Review", CaminhoQuadro.AvisoMovimentacao(2, 6, ListasComCaminho()));

        [Fact]
        public void AvisoMovimentacao_SemCaminhoConfigurado_NaoAvisa()
            => Assert.Null(CaminhoQuadro.AvisoMovimentacao(2, 6, Listas()));

        [Fact]
        public void ValorMaisRecente_UsaUltimaEntradaNaLista()
        {
            ValorCampoCartao[] valores =
            [
                new() { DefinicaoCampoId = 10, Valor = "2026-01-01", NumeroEntradaNaLista = 1, DataPreenchimento = new DateTime(2026, 1, 1) },
                new() { DefinicaoCampoId = 10, Valor = "2026-03-01", NumeroEntradaNaLista = 2, DataPreenchimento = new DateTime(2026, 3, 1) },
                new() { DefinicaoCampoId = 11, Valor = "outro", NumeroEntradaNaLista = 5, DataPreenchimento = new DateTime(2026, 5, 1) },
            ];
            Assert.Equal("2026-03-01", CamposCartao.ValorMaisRecente(valores, 10));
        }

        [Theory]
        [InlineData(TipoCampo.Checkbox, "true", "Sim")]
        [InlineData(TipoCampo.Checkbox, "false", "Não")]
        [InlineData(TipoCampo.Data, "2026-09-27", "27/09/2026")]
        [InlineData(TipoCampo.Texto, "", "—")]
        public void Formatar_ExibeValorAmigavel(TipoCampo tipo, string valor, string esperado)
            => Assert.Equal(esperado, CamposCartao.Formatar(tipo, valor));
    }

    #endregion

    #region RodadaParametrizacaoTests.cs

    public class RodadaParametrizacaoTests
    {
        private sealed class UsuarioFixo : IUsuarioAtualProvider
        {
            public Usuario ObterUsuarioAtual() => new() { Id = 1, Nome = "Ana" };
            public int ObterIdUsuarioAtual() => 1;
        }

        private sealed class EmailNulo : IEmailSender
        {
            public Task EnviarAsync(string para, string? cc, string assunto, string corpoHtml, CancellationToken ct = default) => Task.CompletedTask;
        }

        private static KanbanDbContext Banco()
        {
            var db = new KanbanDbContext(new DbContextOptionsBuilder<KanbanDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            db.Usuarios.Add(new Usuario { Id = 1, Nome = "Ana", Email = "ana@teste.com" });
            db.Quadros.Add(new Quadro { Id = 1, Nome = "Quadro" });
            db.Listas.AddRange(
                new Lista { Id = 10, QuadroId = 1, Nome = "Backlog", Ordem = 0, EhBacklog = true },
                new Lista { Id = 11, QuadroId = 1, Nome = "Em Andamento", Ordem = 1 },
                new Lista { Id = 12, QuadroId = 1, Nome = "Concluído", Ordem = 2 });
            db.SaveChanges();
            return db;
        }

        // ── Feriados e dias úteis ──

        [Fact]
        public void Pascoa_E_FeriadosMoveis_De2026()
        {
            Assert.Equal(new DateTime(2026, 4, 5), FeriadosNacionais.Pascoa(2026));
            var feriados = FeriadosNacionais.Calcular(2026);
            Assert.Contains(feriados, f => f.Data == new DateTime(2026, 4, 3) && f.Nome == "Sexta-feira Santa");
            Assert.Contains(feriados, f => f.Data == new DateTime(2026, 6, 4) && f.Tipo == TipoFeriado.PontoFacultativo);
            Assert.Contains(feriados, f => f.Data == new DateTime(2026, 2, 17));
            Assert.Contains(feriados, f => f.Data == new DateTime(2026, 11, 20));
        }

        [Fact]
        public void DiasUteis_PulaFeriado()
        {
            var quinta = new DateTime(2026, 4, 2);
            var feriados = new HashSet<DateTime> { new(2026, 4, 3) };
            Assert.Equal(new DateTime(2026, 4, 6), DiasUteis.Adicionar(quinta, 1, feriados));
            Assert.Equal(1, DiasUteis.Contar(new DateTime(2026, 4, 3), new DateTime(2026, 4, 5), feriados));
        }

        [Fact]
        public async Task FeriadoRecorrente_ProjetaEmCadaAno()
        {
            using var db = Banco();
            db.Feriados.Add(new Feriado { Data = new DateTime(2000, 7, 9), Nome = "Aniversário da cidade", Tipo = TipoFeriado.Municipal, RecorrenteAnual = true });
            await db.SaveChangesAsync();

            var datas = await FeriadosService.CarregarDatasAsync(db, 2026, 2027);

            Assert.Contains(new DateTime(2026, 7, 9), datas);
            Assert.Contains(new DateTime(2027, 7, 9), datas);
        }

        // ── Espera das automações ──

        [Fact]
        public async Task Agenda_NovaAlteracaoReiniciaEsperaESemDuplicar()
        {
            using var db = Banco();
            var agenda = new AgendaEventosAutomacao(db);
            var evento = new EventoAutomacao(TipoGatilho.CartaoEntrouNaLista, 100, 1, 0, [], 11);

            await agenda.AgendarAsync(evento, TimeSpan.FromMinutes(10));
            var primeiro = (await db.EventosAutomacaoPendentes.SingleAsync()).ExecutarEm;
            await Task.Delay(20);
            await agenda.AgendarAsync(evento, TimeSpan.FromMinutes(10));
            await agenda.AgendarAsync(evento with { Gatilho = TipoGatilho.CampoAlterado, Campo = CampoMonitorado.Prazo, ListaId = null }, TimeSpan.FromMinutes(10));

            var pendentes = await db.EventosAutomacaoPendentes.ToListAsync();
            Assert.Equal(2, pendentes.Count);
            Assert.All(pendentes, p => Assert.True(p.ExecutarEm > primeiro));
            Assert.Empty(await agenda.RetirarVencidosAsync(DateTime.UtcNow));
            Assert.Equal(2, (await agenda.RetirarVencidosAsync(DateTime.UtcNow.AddMinutes(11))).Count);
            Assert.Empty(await db.EventosAutomacaoPendentes.ToListAsync());
        }

        [Fact]
        public async Task EventoAdiado_SemValidade_NaoExecuta()
        {
            using var db = Banco();
            db.Cartoes.Add(new Cartao { Id = 100, ListaId = 10, Titulo = "Voltou", CriadoEm = DateTime.UtcNow });
            var regra = new RegraAutomacao { QuadroId = 1, Nome = "Início", Gatilho = TipoGatilho.CartaoEntrouNaLista, ListaId = 11 };
            regra.Acoes.Add(new AcaoAutomacao { Tipo = TipoAcaoAutomacao.DefinirPrioridade, ParametrosJson = JsonAutomacao.Serializar(new ParametrosAcao { Prioridade = Prioridade.Alta }) });
            db.RegrasAutomacao.Add(regra);
            await db.SaveChangesAsync();
            var configuracao = new ConfigurationBuilder().Build();
            var motor = new MotorAutomacoes(db, new MovimentacaoCartaoService(db, new UsuarioFixo(), configuracao), new EmailNulo(), configuracao,
                new UrlBaseAplicacao(configuracao), Falsos.Teams(), Falsos.Notificador(db, new EmailNulo(), configuracao), NullLogger<MotorAutomacoes>.Instance);

            // O cartão entrou em "Em Andamento" mas voltou ao Backlog antes de a espera terminar.
            await motor.ProcessarEventoAsync(new EventoAutomacao(TipoGatilho.CartaoEntrouNaLista, 100, 1, 0, [], 11));

            Assert.Null((await db.Cartoes.FindAsync(100))!.Prioridade);
            Assert.Empty(await db.ExecucoesAutomacao.ToListAsync());
        }

        // ── Compactação de anexos ──

        private static (LocalDiskAnexoStorage Storage, string Pasta) Storage(bool comprimir = true)
        {
            var pasta = Path.Combine(Path.GetTempPath(), "kanban-testes-" + Guid.NewGuid().ToString("N"));
            var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Anexos:PastaBase"] = pasta, ["Anexos:Comprimir"] = comprimir.ToString(),
                ["Anexos:ExtensoesComprimir"] = ".txt,.csv", ["Anexos:GanhoMinimoPercentual"] = "10"
            }).Build();
            return (new LocalDiskAnexoStorage(configuracao, NullLogger<LocalDiskAnexoStorage>.Instance), pasta);
        }

        [Fact]
        public async Task Compactacao_TextoCompactaEDevolveOriginal()
        {
            var (storage, pasta) = Storage();
            try
            {
                var conteudo = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("linha repetida de log\n", 2000)));
                var salvo = await storage.SalvarAsync(new MemoryStream(conteudo), "log.txt", "text/plain");

                Assert.True(salvo.Comprimido);
                Assert.EndsWith(".txt.gz", salvo.Caminho);
                Assert.True(salvo.TamanhoArmazenado < conteudo.Length / 10);
                await using var lido = await storage.ObterAsync(salvo.Caminho);
                using var copia = new MemoryStream();
                await lido.CopyToAsync(copia);
                Assert.Equal(conteudo, copia.ToArray());
            }
            finally { Directory.Delete(pasta, true); }
        }

        [Fact]
        public async Task Compactacao_FormatoForaDaListaOuSemGanho_GuardaComoEsta()
        {
            var (storage, pasta) = Storage();
            try
            {
                var aleatorio = new byte[4096];
                new Random(1).NextBytes(aleatorio);
                var imagem = await storage.SalvarAsync(new MemoryStream(aleatorio), "foto.png", "image/png");
                var semGanho = await storage.SalvarAsync(new MemoryStream(aleatorio), "dados.csv", "text/csv");

                Assert.False(imagem.Comprimido);
                Assert.False(semGanho.Comprimido);
                Assert.EndsWith("dados.csv", semGanho.Caminho);
            }
            finally { Directory.Delete(pasta, true); }
        }

        [Fact]
        public async Task Compactacao_DeArquivoExistente()
        {
            var (semCompactar, pasta) = Storage(comprimir: false);
            try
            {
                var conteudo = Encoding.UTF8.GetBytes(new string('a', 10_000));
                var antigo = await semCompactar.SalvarAsync(new MemoryStream(conteudo), "antigo.txt", "text/plain");
                Assert.False(antigo.Comprimido);

                var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Anexos:PastaBase"] = pasta, ["Anexos:Comprimir"] = "true", ["Anexos:ExtensoesComprimir"] = ".txt"
                }).Build();
                var resultado = await new LocalDiskAnexoStorage(configuracao, NullLogger<LocalDiskAnexoStorage>.Instance).CompactarAsync(antigo.Caminho);

                Assert.NotNull(resultado);
                Assert.False(File.Exists(antigo.Caminho));
                Assert.True(File.Exists(resultado.Caminho));
            }
            finally { Directory.Delete(pasta, true); }
        }

        // ── Parametrização ──

        [Theory]
        [InlineData("Automacao:AtrasoMinutos", "10", true)]
        [InlineData("Automacao:AtrasoMinutos", "-1", false)]
        [InlineData("Automacao:AtrasoMinutos", "abc", false)]
        [InlineData("Automacao:PontosPorDia", "1.5", true)]
        [InlineData("Automacao:HorarioComercialInicio", "08:30", true)]
        [InlineData("Automacao:HorarioComercialInicio", "8h", false)]
        [InlineData("Anexos:EstrategiaNome", "Original", true)]
        [InlineData("Anexos:EstrategiaNome", "Qualquer", false)]
        [InlineData("Aplicacao:UrlBase", "https://kanban.empresa.com", true)]
        [InlineData("Aplicacao:UrlBase", "kanban.empresa.com", false)]
        public void Parametros_Validacao(string chave, string valor, bool valido)
            => Assert.Equal(valido, ParametrosService.Validar(CatalogoParametros.Buscar(chave)!, valor) is null);

        // ── Relatórios ──

        [Fact]
        public void Percentis_Interpolados()
        {
            var p = RelatoriosService.CalcularPercentis([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
            Assert.Equal(5.5, p.P50, 3);
            Assert.Equal(8.65, p.P85, 3);
            Assert.Equal(10, p.Amostras);
        }

        [Fact]
        public void Previsao_MonteCarlo_ComVazaoConstante()
        {
            var quando = RelatoriosService.PreverQuando([2, 2, 2, 2], 10)!;
            Assert.Equal(DateTime.Today.AddDays(35), quando.P85);
            var quantos = RelatoriosService.PreverQuantos([3, 3, 3], DateTime.Today.AddDays(28))!;
            Assert.Equal(12, quantos.P85);
            Assert.Null(RelatoriosService.PreverQuando([0, 0], 5));
        }

        [Fact]
        public async Task Relatorios_CiclosEVazaoPeloHistorico()
        {
            using var db = Banco();
            var criado = DateTime.UtcNow.AddDays(-10);
            db.Cartoes.Add(new Cartao { Id = 100, ListaId = 12, Titulo = "Entregue", CriadoEm = criado, Estimativa = 3 });
            db.HistoricoAtividades.AddRange(
                new HistoricoAtividade { CartaoId = 100, Tipo = TipoHistoricoAtividade.CartaoCriado, ListaDestinoId = 10, OcorridoEm = criado, UsuarioId = 1, Descricao = "x" },
                new HistoricoAtividade { CartaoId = 100, Tipo = TipoHistoricoAtividade.CartaoMovido, ListaDestinoId = 11, OcorridoEm = criado.AddDays(4), UsuarioId = 1, Descricao = "x" },
                new HistoricoAtividade { CartaoId = 100, Tipo = TipoHistoricoAtividade.CartaoMovido, ListaDestinoId = 12, OcorridoEm = criado.AddDays(6), UsuarioId = 1, Descricao = "x" });
            await db.SaveChangesAsync();

            var dados = await new RelatoriosService(db).CarregarAsync(new FiltroRelatorio(1, DateTime.Today.AddDays(-30), DateTime.Today, null, null));
            var ciclo = Assert.Single(RelatoriosService.Ciclos(dados));
            Assert.Equal(6, ciclo.LeadTimeDias, 1);
            Assert.Equal(2, ciclo.CicloDias!.Value, 1);
            Assert.Equal(1, RelatoriosService.Vazao(dados).Sum(v => v.Cartoes));
            Assert.Equal(3, RelatoriosService.Vazao(dados).Sum(v => v.Pontos));
        }
    }

    #endregion
}
