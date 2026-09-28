using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KanbanDemandas.Infrastructure.Services;

/// <summary>
/// Espera das automações: eventos de alterações feitas por pessoas ficam gravados até o cartão passar
/// "Automacao:AtrasoMinutos" sem nova alteração. Cada alteração no cartão reinicia a espera de todos os eventos dele.
/// </summary>
public sealed class AgendaEventosAutomacao(KanbanDbContext db)
{
    public async Task AgendarAsync(EventoAutomacao evento, TimeSpan atraso)
    {
        var executarEm = DateTime.UtcNow.Add(atraso);
        var pendentes = await db.EventosAutomacaoPendentes.Where(p => p.CartaoId == evento.CartaoId).ToListAsync();
        foreach (var pendente in pendentes) pendente.ExecutarEm = executarEm;

        var repetido = pendentes.Any(p => p.Gatilho == evento.Gatilho && p.ListaId == evento.ListaId
            && p.Campo == evento.Campo && p.UsuarioAlvoId == evento.UsuarioAlvoId);
        if (!repetido)
        {
            db.EventosAutomacaoPendentes.Add(new EventoAutomacaoPendente
            {
                CartaoId = evento.CartaoId, Gatilho = evento.Gatilho, ListaId = evento.ListaId, Campo = evento.Campo,
                UsuarioAlvoId = evento.UsuarioAlvoId, UsuarioId = evento.UsuarioId, CriadoEm = DateTime.UtcNow, ExecutarEm = executarEm
            });
        }
        using (ContextoAutomacao.Entrar(int.MaxValue / 2, []))
            await db.SaveChangesAsync();
    }

    /// <summary>Retira da fila os eventos cuja espera terminou (ou todos, se <paramref name="agora"/> for nulo).</summary>
    public async Task<List<EventoAutomacao>> RetirarVencidosAsync(DateTime agora)
    {
        var vencidos = await db.EventosAutomacaoPendentes.Where(p => p.ExecutarEm <= agora).OrderBy(p => p.Id).ToListAsync();
        if (vencidos.Count == 0) return [];
        db.EventosAutomacaoPendentes.RemoveRange(vencidos);
        using (ContextoAutomacao.Entrar(int.MaxValue / 2, []))
            await db.SaveChangesAsync();
        return vencidos.Select(p => new EventoAutomacao(p.Gatilho, p.CartaoId, p.UsuarioId, 0, [], p.ListaId, p.Campo, p.UsuarioAlvoId)).ToList();
    }
}
