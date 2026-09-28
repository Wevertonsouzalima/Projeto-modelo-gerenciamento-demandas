using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace KanbanDemandas.Infrastructure.Data;

/// <summary>
/// Após cada SaveChanges, identifica quais quadros e quais destinatários de notificação foram afetados
/// e avisa o <see cref="IPublicadorAlteracoes"/>, para que toda alteração chegue em tempo real a quem
/// está com o quadro aberto — sem depender de cada tela lembrar de disparar o aviso.
/// </summary>
public sealed class AlteracoesTempoRealInterceptor(ILogger<AlteracoesTempoRealInterceptor> logger, IPublicadorAlteracoes? publicador = null)
    : SaveChangesInterceptor
{
    private readonly HashSet<int> _quadroIds = [];
    private readonly HashSet<int> _listaIds = [];
    private readonly HashSet<int> _cartaoIds = [];
    private readonly HashSet<int> _itemTarefaIds = [];
    private readonly HashSet<int> _destinatarios = [];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Coletar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Coletar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        await PublicarAsync(eventData.Context);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        PublicarAsync(eventData.Context).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        Limpar();
        base.SaveChangesFailed(eventData);
    }

    private void Coletar(DbContext? context)
    {
        if (context is null || publicador is null) return;
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;
            switch (entry.Entity)
            {
                case Quadro q: _quadroIds.Add(q.Id); break;
                case Lista l: _quadroIds.Add(l.QuadroId); break;
                case Etiqueta e: _quadroIds.Add(e.QuadroId); break;
                case Sprint s: _quadroIds.Add(s.QuadroId); break;
                case DefinicaoCampo d: _listaIds.Add(d.ListaId); break;
                case RegraAutomacao r: _quadroIds.Add(r.QuadroId); break;
                case AlertaCartao al: _cartaoIds.Add(al.CartaoId); break;
                case VinculoGit vg: _cartaoIds.Add(vg.CartaoId); break;
                case SugestaoAutomacao sa: _cartaoIds.Add(sa.CartaoId); break;
                case Cartao c:
                    _listaIds.Add(c.ListaId);
                    if (entry.State == EntityState.Modified && entry.Property(nameof(Cartao.ListaId)).OriginalValue is int listaOriginal)
                        _listaIds.Add(listaOriginal);
                    break;
                case ItemTarefa i: _cartaoIds.Add(i.CartaoId); break;
                case Anexo a: _cartaoIds.Add(a.CartaoId); break;
                case EmailCartao m: _cartaoIds.Add(m.CartaoId); break;
                case ValorCampoCartao v: _cartaoIds.Add(v.CartaoId); break;
                case CartaoDesenvolvedor cd: _cartaoIds.Add(cd.CartaoId); break;
                case CartaoEtiqueta ce: _cartaoIds.Add(ce.CartaoId); break;
                case SprintCartao sc: _cartaoIds.Add(sc.CartaoId); break;
                case RelacaoCartao rc: _cartaoIds.Add(rc.CartaoOrigemId); _cartaoIds.Add(rc.CartaoDestinoId); break;
                case Comentario co:
                    if (co.CartaoId.HasValue) _cartaoIds.Add(co.CartaoId.Value);
                    if (co.ItemTarefaId.HasValue) _itemTarefaIds.Add(co.ItemTarefaId.Value);
                    break;
                case Reuniao re:
                    if (re.CartaoId.HasValue) _cartaoIds.Add(re.CartaoId.Value);
                    if (re.ItemTarefaId.HasValue) _itemTarefaIds.Add(re.ItemTarefaId.Value);
                    break;
                case Notificacao n: _destinatarios.Add(n.DestinatarioId); break;
            }
        }
    }

    private async Task PublicarAsync(DbContext? context)
    {
        if (context is null || publicador is null) return;
        try
        {
            if (_itemTarefaIds.Count > 0)
                _cartaoIds.UnionWith(await context.Set<ItemTarefa>().IgnoreQueryFilters()
                    .Where(i => _itemTarefaIds.Contains(i.Id)).Select(i => i.CartaoId).ToListAsync());
            if (_cartaoIds.Count > 0)
                _listaIds.UnionWith(await context.Set<Cartao>().IgnoreQueryFilters()
                    .Where(c => _cartaoIds.Contains(c.Id)).Select(c => c.ListaId).ToListAsync());
            if (_listaIds.Count > 0)
                _quadroIds.UnionWith(await context.Set<Lista>().IgnoreQueryFilters()
                    .Where(l => _listaIds.Contains(l.Id)).Select(l => l.QuadroId).ToListAsync());

            if (_quadroIds.Count > 0 || _destinatarios.Count > 0)
                await publicador.PublicarAsync(_quadroIds.ToList(), _destinatarios.ToList());
        }
        catch (Exception ex)
        {
            // Os dados já foram gravados; falhar o aviso em tempo real não deve quebrar a operação do usuário.
            logger.LogWarning(ex, "Falha ao publicar alterações em tempo real.");
        }
        finally
        {
            Limpar();
        }
    }

    private void Limpar()
    {
        _quadroIds.Clear();
        _listaIds.Clear();
        _cartaoIds.Clear();
        _itemTarefaIds.Clear();
        _destinatarios.Clear();
    }
}
