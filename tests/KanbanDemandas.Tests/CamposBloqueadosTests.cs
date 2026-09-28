using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Core.Regras;
using KanbanDemandas.Infrastructure.Data;
using KanbanDemandas.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KanbanDemandas.Tests;

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
