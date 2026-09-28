using KanbanDemandas.Core.Regras;
using KanbanDemandas.Infrastructure.Services;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure.Data;
using KanbanDemandas.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KanbanDemandas.Tests;

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
