using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KanbanDemandas.Infrastructure.Data;

/// <summary>
/// Traduz o que foi gravado em eventos de automação (cartão criado, movido, campo alterado etc.).
/// Detectar no SaveChanges garante que qualquer tela ou serviço dispare as regras, sem cada um lembrar de avisar.
/// Os valores originais só existem antes de gravar; os IDs de registros novos, só depois — por isso a coleta
/// acontece em duas fases.
/// </summary>
public sealed class EventosAutomacaoInterceptor(SessaoUsuario sessao, IFilaEventosAutomacao? fila = null) : SaveChangesInterceptor
{
    private readonly List<Func<EventoAutomacao?>> _pendentes = [];

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

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Publicar();
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Publicar();
        return base.SavedChanges(eventData, result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _pendentes.Clear();
        base.SaveChangesFailed(eventData);
    }

    private void Publicar()
    {
        if (fila is null) return;
        foreach (var evento in _pendentes.Select(p => p()).OfType<EventoAutomacao>().Distinct())
            fila.Enfileirar(evento);
        _pendentes.Clear();
    }

    private void Coletar(DbContext? context)
    {
        if (context is null || fila is null) return;
        var entradas = context.ChangeTracker.Entries().ToList();

        // Um estorno (vai-e-volta desfeito) não é uma movimentação nova: não dispara regras de entrada/saída.
        var cartoesEstornados = entradas
            .Where(e => e.State == EntityState.Modified && e.Entity is HistoricoAtividade { Estornado: true })
            .Select(e => ((HistoricoAtividade)e.Entity).CartaoId)
            .ToHashSet();

        foreach (var entrada in entradas)
        {
            switch (entrada.Entity)
            {
                case Cartao cartao when entrada.State == EntityState.Added:
                    Adicionar(TipoGatilho.CartaoCriado, () => cartao.Id, listaId: () => cartao.ListaId);
                    if (cartao.Prazo.HasValue) Adicionar(TipoGatilho.ConflitoDatas, () => cartao.Id);
                    break;

                case Cartao cartao when entrada.State == EntityState.Modified && !cartao.Excluido:
                    ColetarAlteracoesCartao(entrada, cartao, cartoesEstornados.Contains(cartao.Id));
                    break;

                case CartaoDesenvolvedor cd when entrada.State == EntityState.Added:
                    var usuarioAlvo = cd.UsuarioId;
                    Adicionar(TipoGatilho.CartaoAtribuido, () => cd.CartaoId, usuarioAlvo: usuarioAlvo);
                    Adicionar(TipoGatilho.CampoAlterado, () => cd.CartaoId, campo: CampoMonitorado.Desenvolvedores);
                    Adicionar(TipoGatilho.ConflitoDatas, () => cd.CartaoId);
                    break;

                case CartaoDesenvolvedor cd when entrada.State == EntityState.Deleted:
                    Adicionar(TipoGatilho.CampoAlterado, () => cd.CartaoId, campo: CampoMonitorado.Desenvolvedores);
                    break;

                case CartaoEtiqueta ce when entrada.State is EntityState.Added or EntityState.Deleted:
                    Adicionar(TipoGatilho.CampoAlterado, () => ce.CartaoId, campo: CampoMonitorado.Etiquetas);
                    break;

                case ValorCampoCartao v when entrada.State == EntityState.Added
                    || entrada.State == EntityState.Modified && entrada.Property(nameof(ValorCampoCartao.Valor)).IsModified:
                    Adicionar(TipoGatilho.CampoAlterado, () => v.CartaoId, campo: CampoMonitorado.CampoPersonalizado);
                    break;

                case Comentario c when entrada.State == EntityState.Added && c.CartaoId.HasValue:
                    Adicionar(TipoGatilho.ComentarioAdicionado, () => c.CartaoId!.Value);
                    break;

                case ItemTarefa i when entrada.State == EntityState.Modified && i.Concluido
                    && entrada.Property(nameof(ItemTarefa.Concluido)).OriginalValue is false:
                    Adicionar(TipoGatilho.ChecklistConcluido, () => i.CartaoId);
                    break;

                case RelacaoCartao r when entrada.State == EntityState.Added:
                    if (r.Tipo == TipoRelacaoCartao.BloqueadoPor) Adicionar(TipoGatilho.CartaoBloqueado, () => r.CartaoOrigemId);
                    if (r.Tipo == TipoRelacaoCartao.Bloqueia) Adicionar(TipoGatilho.CartaoBloqueado, () => r.CartaoDestinoId);
                    break;
            }
        }
    }

    private void ColetarAlteracoesCartao(EntityEntry entrada, Cartao cartao, bool estornado)
    {
        var lista = entrada.Property(nameof(Cartao.ListaId));
        if (lista.IsModified && lista.OriginalValue is int origem && origem != cartao.ListaId && !estornado)
        {
            var destino = cartao.ListaId;
            Adicionar(TipoGatilho.CartaoSaiuDaLista, () => cartao.Id, listaId: () => origem);
            Adicionar(TipoGatilho.CartaoEntrouNaLista, () => cartao.Id, listaId: () => destino);
        }

        var campos = new (string Propriedade, CampoMonitorado Campo)[]
        {
            (nameof(Cartao.Prazo), CampoMonitorado.Prazo),
            (nameof(Cartao.DataInicio), CampoMonitorado.DataInicio),
            (nameof(Cartao.Prioridade), CampoMonitorado.Prioridade),
            (nameof(Cartao.SistemaId), CampoMonitorado.Sistema),
            (nameof(Cartao.SolicitanteId), CampoMonitorado.Solicitante),
            (nameof(Cartao.Estimativa), CampoMonitorado.Estimativa)
        };
        foreach (var (propriedade, campo) in campos)
        {
            var prop = entrada.Property(propriedade);
            if (prop.IsModified && !Equals(prop.OriginalValue, prop.CurrentValue))
                Adicionar(TipoGatilho.CampoAlterado, () => cartao.Id, campo: campo);
        }

        if (Alterou(entrada, nameof(Cartao.Prazo)) || Alterou(entrada, nameof(Cartao.DataInicio)))
            Adicionar(TipoGatilho.ConflitoDatas, () => cartao.Id);
    }

    private static bool Alterou(EntityEntry entrada, string propriedade)
    {
        var prop = entrada.Property(propriedade);
        return prop.IsModified && !Equals(prop.OriginalValue, prop.CurrentValue);
    }

    private void Adicionar(TipoGatilho gatilho, Func<int> cartaoId, Func<int>? listaId = null,
        CampoMonitorado campo = CampoMonitorado.Qualquer, int? usuarioAlvo = null)
    {
        var usuarioId = sessao.UsuarioId ?? 1;
        var profundidade = ContextoAutomacao.Profundidade;
        var cadeia = ContextoAutomacao.RegrasNaCadeia;
        _pendentes.Add(() =>
        {
            var id = cartaoId();
            return id <= 0 ? null : new EventoAutomacao(gatilho, id, usuarioId, profundidade, cadeia, listaId?.Invoke(), campo, usuarioAlvo);
        });
    }
}
