using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KanbanDemandas.Infrastructure.Services;

/// <summary>
/// Implementação fake de IUsuarioAtualProvider para a v1 (sem autenticação real).
/// O usuário selecionado é mantido por sessão via um estado Scoped simples.
/// Para plugar AD/SSO depois: basta criar outra implementação de IUsuarioAtualProvider
/// e registrá-la no DI — nenhuma outra mudança necessária.
/// </summary>
public class FakeUsuarioAtualProvider : IUsuarioAtualProvider
{
    private readonly SessaoUsuario _sessao;
    private readonly KanbanDbContext _db;

    public FakeUsuarioAtualProvider(SessaoUsuario sessao, KanbanDbContext db)
    {
        _sessao = sessao;
        _db = db;
    }

    public Usuario ObterUsuarioAtual()
    {
        var id = _sessao.UsuarioId ?? 1; // fallback para Admin
        var usuario = _db.Usuarios
            .AsNoTracking()
            .FirstOrDefault(u => u.Id == id)
            ?? throw new InvalidOperationException($"Usuário com Id={id} não encontrado.");
        return usuario;
    }

    public int ObterIdUsuarioAtual() => _sessao.UsuarioId ?? 1;
}

/// <summary>
/// Estado de sessão do usuário selecionado. Registrado como Scoped (por circuito Blazor).
/// Permite troca de usuário em tempo de execução sem reiniciar o app.
/// </summary>
public class SessaoUsuario
{
    public int? UsuarioId { get; set; }

    public event Action? OnChange;
    public void NotificarMudanca() => OnChange?.Invoke();
}
