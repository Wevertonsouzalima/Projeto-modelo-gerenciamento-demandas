using System.Text;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure.Data;
using KanbanDemandas.Infrastructure.Services;
using KanbanDemandas.Web.Configuracao;
using KanbanDemandas.Web.Services;
using KanbanDemandas.Web.Services.Automacoes;
using KanbanDemandas.Web.Services.Relatorios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace KanbanDemandas.Tests;

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
