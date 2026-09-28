using KanbanDemandas.Core.Regras;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Web.Services;

namespace KanbanDemandas.Tests;

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
