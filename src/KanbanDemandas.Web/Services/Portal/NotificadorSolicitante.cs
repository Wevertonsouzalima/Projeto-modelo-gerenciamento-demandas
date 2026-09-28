using System.Net;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Core.Regras;
using KanbanDemandas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KanbanDemandas.Web.Services.Portal;

/// <summary>
/// Mantém o solicitante informado por e-mail: mudança de status público e respostas públicas do time.
/// Roda no processamento de eventos (depois da espera das automações), então um vai-e-volta não gera e-mail.
/// </summary>
public sealed class NotificadorSolicitante(KanbanDbContext db, IEmailSender emailSender, IConfiguration configuracao, ILogger<NotificadorSolicitante> logger)
{
    private bool Ativo => configuracao.GetValue("Portal:NotificarSolicitante", true);
    private string UrlPortal => (configuracao["Portal:UrlBase"] ?? "").TrimEnd('/');
    private string? Prefixo => configuracao["Cartao:PrefixoCodigo"];

    public string LinkSolicitacao(int cartaoId) => string.IsNullOrEmpty(UrlPortal) ? "" : $"{UrlPortal}/solicitacoes/{cartaoId}";

    public async Task AoEntrarNaListaAsync(EventoAutomacao evento)
    {
        if (!Ativo) return;
        var cartao = await db.Cartoes.AsNoTracking().Include(c => c.Solicitante).Include(c => c.Lista)
            .FirstOrDefaultAsync(c => c.Id == evento.CartaoId && !c.Excluido);
        if (cartao?.Solicitante is not { } solicitante || cartao.ListaId != evento.ListaId) return;

        var listas = await db.Listas.AsNoTracking().Where(l => l.QuadroId == cartao.Lista.QuadroId).ToListAsync();
        var entradas = await db.HistoricoAtividades.AsNoTracking().Where(h => h.CartaoId == cartao.Id).ToListAsync();
        var historico = StatusPortal.Historico(cartao, entradas, listas, configuracao["Portal:StatusInicial"]);
        if (historico.Count < 2) return;

        // Só avisa se a mudança mais recente de status público foi causada por esta entrada na lista.
        var ultima = historico[^1];
        var entradaAtual = entradas.Where(h => h.ListaDestinoId == cartao.ListaId).MaxBy(h => h.OcorridoEm);
        if (entradaAtual is null || ultima.Em != entradaAtual.OcorridoEm) return;

        var codigo = CodigoCartao.Formatar(cartao.Id, Prefixo);
        await EnviarAsync(solicitante, $"[{codigo}] Sua solicitação agora está: {ultima.Status}",
            $"<p>Olá, {WebUtility.HtmlEncode(solicitante.Nome)}.</p>" +
            $"<p>A solicitação <b>{codigo} — {WebUtility.HtmlEncode(cartao.Titulo)}</b> mudou de <b>{WebUtility.HtmlEncode(historico[^2].Status)}</b> " +
            $"para <b>{WebUtility.HtmlEncode(ultima.Status)}</b>.</p>" + BotaoLink(cartao.Id));
    }

    /// <summary>Chamado quando alguém do time publica um comentário visível ao solicitante.</summary>
    public async Task AoResponderAsync(int cartaoId, int autorId, string texto)
    {
        if (!Ativo) return;
        var cartao = await db.Cartoes.AsNoTracking().Include(c => c.Solicitante).FirstOrDefaultAsync(c => c.Id == cartaoId && !c.Excluido);
        if (cartao?.Solicitante is not { } solicitante || solicitante.Id == autorId) return;
        var autor = await db.Usuarios.AsNoTracking().Where(u => u.Id == autorId).Select(u => u.Nome).FirstOrDefaultAsync() ?? "O time";
        var codigo = CodigoCartao.Formatar(cartao.Id, Prefixo);
        await EnviarAsync(solicitante, $"[{codigo}] Nova mensagem sobre sua solicitação",
            $"<p>Olá, {WebUtility.HtmlEncode(solicitante.Nome)}.</p>" +
            $"<p><b>{WebUtility.HtmlEncode(autor)}</b> escreveu sobre <b>{codigo} — {WebUtility.HtmlEncode(cartao.Titulo)}</b>:</p>" +
            $"<blockquote style=\"border-left:3px solid #1565c0;margin:0;padding:4px 12px;color:#333\">{WebUtility.HtmlEncode(texto).Replace("\n", "<br>")}</blockquote>" +
            BotaoLink(cartao.Id));
    }

    private string BotaoLink(int cartaoId)
    {
        var link = LinkSolicitacao(cartaoId);
        return string.IsNullOrEmpty(link) ? "" : $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Acompanhar no portal de solicitações</a></p>";
    }

    private async Task EnviarAsync(Usuario destinatario, string assunto, string corpo)
    {
        if (!destinatario.Ativo || !destinatario.ReceberEmailsAutomacao || string.IsNullOrWhiteSpace(destinatario.Email)) return;
        try
        {
            await emailSender.EnviarAsync(destinatario.Email, null, assunto, corpo);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao avisar o solicitante {Usuario} por e-mail.", destinatario.Id);
        }
    }
}
