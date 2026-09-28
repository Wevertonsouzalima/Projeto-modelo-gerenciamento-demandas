using KanbanDemandas.Core.Regras;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KanbanDemandas.Web.Services.Automacoes;

/// <summary>
/// Retrato somente leitura de um quadro com tudo o que condições e gatilhos precisam avaliar
/// (listas, cartões, responsáveis, campos, relações e quando cada cartão entrou na lista atual).
/// </summary>
public sealed class FotoQuadro
{
    public int QuadroId { get; init; }
    public List<Lista> Listas { get; init; } = [];
    public List<Cartao> Cartoes { get; init; } = [];
    public List<Usuario> Usuarios { get; init; } = [];
    public List<Sprint> Sprints { get; init; } = [];
    public List<Etiqueta> Etiquetas { get; init; } = [];
    public List<Sistema> Sistemas { get; init; } = [];
    public Dictionary<int, DateTime> EntradaNaListaAtual { get; init; } = [];

    /// <summary>Datas não úteis (vazio quando o parâmetro "considerar feriados" está desligado).</summary>
    public IReadOnlySet<DateTime> Feriados { get; init; } = new HashSet<DateTime>();

    public static async Task<FotoQuadro> CarregarAsync(KanbanDbContext db, int quadroId, bool considerarFeriados = true)
    {
        var listas = await db.Listas.AsNoTracking().Include(l => l.DefinicoesCampo.Where(d => !d.Excluido))
            .Where(l => l.QuadroId == quadroId && !l.Excluido).ToListAsync();
        var idsListas = listas.Select(l => l.Id).ToList();
        var cartoes = await db.Cartoes.AsNoTracking().AsSplitQuery()
            .Include(c => c.Sistema)
            .Include(c => c.Desenvolvedores)
            .Include(c => c.Etiquetas)
            .Include(c => c.ValoresCampo)
            .Include(c => c.ItensTarefa.Where(i => !i.Excluido))
            .Include(c => c.RelacoesOrigem).ThenInclude(r => r.CartaoDestino)
            .Include(c => c.RelacoesDestino).ThenInclude(r => r.CartaoOrigem)
            .Where(c => idsListas.Contains(c.ListaId) && !c.Excluido)
            .ToListAsync();
        var idsCartoes = cartoes.Select(c => c.Id).ToList();

        // Estornos já ficam fora pelo filtro global do histórico.
        var entradas = await db.HistoricoAtividades.AsNoTracking()
            .Where(h => idsCartoes.Contains(h.CartaoId) && h.ListaDestinoId != null
                && (h.Tipo == TipoHistoricoAtividade.CartaoMovido || h.Tipo == TipoHistoricoAtividade.CartaoCriado))
            .GroupBy(h => new { h.CartaoId, h.ListaDestinoId })
            .Select(g => new { g.Key.CartaoId, g.Key.ListaDestinoId, Ultima = g.Max(h => h.OcorridoEm) })
            .ToListAsync();
        var entradaAtual = cartoes.ToDictionary(c => c.Id,
            c => entradas.FirstOrDefault(e => e.CartaoId == c.Id && e.ListaDestinoId == c.ListaId)?.Ultima ?? c.CriadoEm);

        return new FotoQuadro
        {
            QuadroId = quadroId,
            Listas = listas,
            Cartoes = cartoes,
            Usuarios = await db.Usuarios.AsNoTracking().Where(u => u.Ativo && !u.Excluido).OrderBy(u => u.Nome).ToListAsync(),
            Sprints = await db.Sprints.AsNoTracking().Include(s => s.Cartoes).Where(s => s.QuadroId == quadroId && !s.Excluido).ToListAsync(),
            EntradaNaListaAtual = entradaAtual,
            Feriados = considerarFeriados ? await FeriadosService.CarregarDatasAsync(db, DateTime.Today.Year - 2, DateTime.Today.Year + 3) : new HashSet<DateTime>(),
            Etiquetas = await db.Etiquetas.AsNoTracking().Where(e => e.QuadroId == quadroId && !e.Excluido).OrderBy(e => e.Nome).ToListAsync(),
            Sistemas = await db.Sistemas.AsNoTracking().Where(s => s.Ativo).OrderBy(s => s.Nome).ToListAsync()
        };
    }

    public Cartao? Cartao(int id) => Cartoes.FirstOrDefault(c => c.Id == id);
    public Lista? Lista(int id) => Listas.FirstOrDefault(l => l.Id == id);
    public string NomeLista(int id) => Lista(id) is { } lista ? ListasQuadro.NomeCompleto(lista, Listas) : "";
    public string? NomeUsuario(int? id) => Usuarios.FirstOrDefault(u => u.Id == id)?.Nome;

    public bool EstaConcluido(Cartao cartao) => ListasQuadro.EhConcluido(cartao.ListaId, Listas);

    public int DiasNaLista(Cartao cartao)
        => EntradaNaListaAtual.TryGetValue(cartao.Id, out var entrada) ? (DateTime.Today - entrada.ToLocalTime().Date).Days : 0;

    public List<Cartao> Conflitos(Cartao cartao) => ConflitosAgenda.Encontrar(cartao, Cartoes, EstaConcluido);

    public bool EstaBloqueado(Cartao cartao)
        => cartao.RelacoesOrigem.Any(r => r.Tipo == TipoRelacaoCartao.BloqueadoPor && !ListasQuadro.EhConcluido(r.CartaoDestino.ListaId, Listas))
        || cartao.RelacoesDestino.Any(r => r.Tipo == TipoRelacaoCartao.Bloqueia && !ListasQuadro.EhConcluido(r.CartaoOrigem.ListaId, Listas));

    /// <summary>A lista do cartão ou alguma das listas-pai dela está no conjunto (condição por grupo).</summary>
    public bool ListaOuPaiEm(int listaId, IReadOnlyCollection<int> ids)
    {
        for (var atual = Lista(listaId); atual is not null; atual = atual.ListaPaiId.HasValue ? Lista(atual.ListaPaiId.Value) : null)
            if (ids.Contains(atual.Id)) return true;
        return false;
    }

    /// <summary>Valor mais recente de um campo personalizado pelo nome, preferindo a definição da lista atual.</summary>
    public (DefinicaoCampo? Definicao, string? Valor) ValorCampo(Cartao cartao, string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) return (null, null);
        var definicoes = Listas.SelectMany(l => l.DefinicoesCampo)
            .Where(d => d.Nome.Trim().Equals(nome.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        var daLista = definicoes.FirstOrDefault(d => d.ListaId == cartao.ListaId);
        if (daLista is not null)
        {
            var valor = CamposCartao.ValorMaisRecente(cartao.ValoresCampo.Where(v => !v.Excluido), daLista.Id);
            if (!string.IsNullOrWhiteSpace(valor)) return (daLista, valor);
        }
        var ids = definicoes.Select(d => d.Id).ToHashSet();
        var ultimo = cartao.ValoresCampo.Where(v => !v.Excluido && ids.Contains(v.DefinicaoCampoId) && !string.IsNullOrWhiteSpace(v.Valor))
            .OrderByDescending(v => v.DataPreenchimento).FirstOrDefault();
        return ultimo is null ? (daLista ?? definicoes.FirstOrDefault(), null) : (definicoes.First(d => d.Id == ultimo.DefinicaoCampoId), ultimo.Valor);
    }

    /// <summary>A estimativa (em dias úteis, via pontos por dia) não cabe nos dias úteis até o prazo.</summary>
    public bool PrazoEmRisco(Cartao cartao, decimal pontosPorDia)
    {
        if (!cartao.Prazo.HasValue || !cartao.Estimativa.HasValue || cartao.Estimativa <= 0 || EstaConcluido(cartao)) return false;
        var prazo = cartao.Prazo.Value.ToLocalTime().Date;
        if (prazo < DateTime.Today) return true;
        var inicio = cartao.DataInicio?.ToLocalTime().Date is { } dataInicio && dataInicio > DateTime.Today ? dataInicio : DateTime.Today;
        var necessarios = (int)Math.Ceiling(cartao.Estimativa.Value / Math.Max(0.1m, pontosPorDia));
        return necessarios > DiasUteis.Contar(inicio, prazo, Feriados);
    }
}
