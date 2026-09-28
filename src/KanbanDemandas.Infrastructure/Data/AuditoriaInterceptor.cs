using KanbanDemandas.Core.Entities;
using KanbanDemandas.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KanbanDemandas.Infrastructure.Data;

/// <summary>
/// Interceptor que preenche automaticamente os campos de auditoria
/// (CriadoEm, CriadoPorId, AlteradoEm, AlteradoPorId) antes de cada SaveChanges.
/// </summary>
public class AuditoriaInterceptor : SaveChangesInterceptor
{
    private readonly SessaoUsuario _sessao;

    public AuditoriaInterceptor(SessaoUsuario sessao)
    {
        _sessao = sessao;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        AplicarAuditoria(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AplicarAuditoria(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AplicarAuditoria(DbContext? context)
    {
        if (context is null) return;

        int usuarioId = _sessao.UsuarioId ?? 1;
        var agora = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<EntidadeBase>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CriadoEm = agora;
                    entry.Entity.CriadoPorId = usuarioId;
                    break;

                case EntityState.Modified:
                    entry.Entity.AlteradoEm = agora;
                    entry.Entity.AlteradoPorId = usuarioId;
                    break;
            }
        }
    }
}