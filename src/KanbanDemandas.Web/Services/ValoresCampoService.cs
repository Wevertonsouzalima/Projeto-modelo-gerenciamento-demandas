using KanbanDemandas.Core.Regras;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KanbanDemandas.Web.Services;

/// <summary>Gravação dos campos de lista e dos dados exigidos por uma etapa, usada por todas as telas.</summary>
public sealed class ValoresCampoService(KanbanDbContext db, IUsuarioAtualProvider usuarioProvider)
{
    /// <summary>
    /// Grava os valores dos campos manuais da lista. Campo "pedir a cada entrada" ganha um registro por entrada;
    /// os demais têm um valor único que é atualizado. Campos automáticos são ignorados.
    /// </summary>
    public async Task SalvarValoresAsync(int cartaoId, int listaId, IReadOnlyDictionary<int, string?> valores)
    {
        await AplicarValoresAsync(cartaoId, listaId, valores);
        await db.SaveChangesAsync();
    }

    /// <summary>Aplica o que o usuário informou para cumprir a etapa (campos fixos + campos da lista).</summary>
    public async Task AplicarDadosEtapaAsync(int cartaoId, int listaId, DadosEtapa dados)
    {
        var cartao = await db.Cartoes.Include(c => c.Desenvolvedores).FirstOrDefaultAsync(c => c.Id == cartaoId && !c.Excluido);
        if (cartao is null) return;
        var usuarioId = usuarioProvider.ObterIdUsuarioAtual();

        var novos = dados.DesenvolvedorIds.Where(id => cartao.Desenvolvedores.All(d => d.UsuarioId != id)).Distinct().ToList();
        var semPrincipal = cartao.Desenvolvedores.All(d => !d.Principal);
        foreach (var id in novos)
        {
            db.CartaoDesenvolvedores.Add(new CartaoDesenvolvedor { CartaoId = cartaoId, UsuarioId = id, Principal = semPrincipal && id == novos[0] });
            if (id != usuarioId)
            {
                db.Notificacoes.Add(new Notificacao
                {
                    DestinatarioId = id, CartaoOrigemId = cartaoId, Tipo = TipoNotificacao.AtribuicaoCartao,
                    Mensagem = $"Você foi atribuído ao cartão '{cartao.Titulo}'.", CriadoPorId = usuarioId, CriadoEm = DateTime.UtcNow
                });
            }
        }
        if (dados.Prazo.HasValue) cartao.Prazo = dados.Prazo.Value.ToUniversalTime();
        if (dados.Estimativa.HasValue) cartao.Estimativa = dados.Estimativa;
        if (dados.SistemaId.HasValue) cartao.SistemaId = dados.SistemaId;
        if (dados.SolicitanteId.HasValue) cartao.SolicitanteId = dados.SolicitanteId;

        await AplicarValoresAsync(cartaoId, listaId, dados.ValoresCampo);
        await db.SaveChangesAsync();
    }

    private async Task AplicarValoresAsync(int cartaoId, int listaId, IReadOnlyDictionary<int, string?> valores)
    {
        if (valores.Count == 0) return;
        var usuarioId = usuarioProvider.ObterIdUsuarioAtual();
        var campos = await db.DefinicoesCampo
            .Where(c => c.ListaId == listaId && !c.Excluido && c.Preenchimento == PreenchimentoAutomatico.Manual)
            .ToListAsync();

        // O número da entrada vem do histórico: quantas vezes o cartão entrou nesta lista (estornos não contam).
        var numeroEntradaAtual = Math.Max(1, await db.HistoricoAtividades.CountAsync(h => h.CartaoId == cartaoId && h.ListaDestinoId == listaId
            && (h.Tipo == TipoHistoricoAtividade.CartaoMovido || h.Tipo == TipoHistoricoAtividade.CartaoCriado)));

        foreach (var campo in campos.Where(c => valores.ContainsKey(c.Id)))
        {
            var valor = valores[campo.Id];
            var existente = await db.ValoresCampoCartao
                .Where(v => v.CartaoId == cartaoId && v.DefinicaoCampoId == campo.Id)
                .OrderByDescending(v => v.NumeroEntradaNaLista).ThenByDescending(v => v.DataPreenchimento)
                .FirstOrDefaultAsync();

            if (existente is null || (campo.PedirNovamenteACadaEntrada && existente.NumeroEntradaNaLista != numeroEntradaAtual))
            {
                if (string.IsNullOrWhiteSpace(valor)) continue;
                db.ValoresCampoCartao.Add(new ValorCampoCartao
                {
                    CartaoId = cartaoId, DefinicaoCampoId = campo.Id, Valor = valor,
                    NumeroEntradaNaLista = numeroEntradaAtual, DataPreenchimento = DateTime.UtcNow,
                    PreenchidoPorId = usuarioId, CriadoPorId = usuarioId, CriadoEm = DateTime.UtcNow
                });
            }
            else if (existente.Valor != valor)
            {
                existente.Valor = valor;
                existente.DataPreenchimento = DateTime.UtcNow;
                existente.PreenchidoPorId = usuarioId;
            }
        }
    }
}
