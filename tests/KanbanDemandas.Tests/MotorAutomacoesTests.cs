using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure.Data;
using KanbanDemandas.Infrastructure.Services;
using KanbanDemandas.Web.Services;
using KanbanDemandas.Web.Services.Automacoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace KanbanDemandas.Tests;

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
