using KanbanDemandas.Infrastructure.Services;
using KanbanDemandas.Infrastructure.Configuracao;
using KanbanDemandas.Infrastructure;
using KanbanDemandas.Infrastructure.Data;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Web.Components;
using KanbanDemandas.Web.Configuracao;
using KanbanDemandas.Web.Services.Automacoes;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Parâmetros alterados na tela de parametrização (tabela ParametrosSistema) sobrepõem o appsettings.
var fonteParametros = new ParametrosBancoConfigurationSource(builder.Configuration.GetConnectionString("DefaultConnection"));
((IConfigurationBuilder)builder.Configuration).Add(fonteParametros);
builder.Services.AddSingleton(fonteParametros.Provider);
builder.Services.AddScoped<ParametrosService>();

// ── Blazor + componentes interativos (Server-side) ────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── SignalR ───────────────────────────────────────────────────────────────
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1 MB
});

// ── MudBlazor (UI) ────────────────────────────────────────────────────────
builder.Services.AddMudServices();

// ── Infrastructure (DbContext, EF Core, repositórios, serviços) ───────────
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<MovimentacaoCartaoService>();
builder.Services.AddScoped<KanbanDemandas.Web.Services.ValoresCampoService>();
builder.Services.AddScoped<KanbanDemandas.Web.Services.AssistenteMovimentacao>();
builder.Services.AddSingleton<KanbanDemandas.Web.Services.PublicadorAlteracoesTempoReal>();
builder.Services.AddSingleton<KanbanDemandas.Core.Interfaces.IPublicadorAlteracoes>(sp => sp.GetRequiredService<KanbanDemandas.Web.Services.PublicadorAlteracoesTempoReal>());

// ── Motor de automações (eventos em fila + verificação periódica de gatilhos de tempo) ──
builder.Services.AddSingleton<FilaEventosAutomacao>();
builder.Services.AddSingleton<IFilaEventosAutomacao>(sp => sp.GetRequiredService<FilaEventosAutomacao>());
builder.Services.AddSingleton<UrlBaseAplicacao>();
builder.Services.AddScoped<MotorAutomacoes>();
builder.Services.AddScoped<AgendaEventosAutomacao>();
builder.Services.AddScoped<KanbanDemandas.Web.Services.FeriadosService>();
builder.Services.AddScoped<KanbanDemandas.Web.Services.CompactacaoAnexosService>();
builder.Services.AddScoped<KanbanDemandas.Web.Services.Relatorios.RelatoriosService>();
builder.Services.AddSingleton<MensagemTeams>();
builder.Services.AddScoped<KanbanDemandas.Web.Services.Portal.NotificadorSolicitante>();
builder.Services.AddScoped<KanbanDemandas.Web.Services.Integracoes.GitHubWebhookService>();
builder.Services.AddHttpClient("Teams", cliente => cliente.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddHttpClient("BrasilAPI", cliente =>
{
    cliente.BaseAddress = new Uri("https://brasilapi.com.br/");
    cliente.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHostedService<ProcessadorEventosAutomacao>();
builder.Services.AddHostedService<AgendadorAutomacoes>();

// ── HttpContextAccessor ───────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (app.Environment.IsProduction())
{
    app.Logger.LogWarning(
        "Executando em Production sem autenticação real (FakeUsuarioAtualProvider). " +
        "Qualquer pessoa com acesso à URL pode agir como qualquer usuário cadastrado. " +
        "Substitua IUsuarioAtualProvider por um provedor real (AD/SSO) antes de expor este ambiente.");
}


// ── Pipeline HTTP ─────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// Links em e-mails de automação precisam do endereço público; sem "Aplicacao:UrlBase" configurado, usa o da primeira requisição.
app.Use((contexto, proximo) =>
{
    contexto.RequestServices.GetRequiredService<UrlBaseAplicacao>().Detectar(contexto.Request);
    return proximo();
});
app.UseAntiforgery();
app.MapStaticAssets();

// ── Rota de download de anexos ────────────────────────────────────────────
// Nota: sem autenticação real na v1 (fora de escopo, ver especificação), não há como restringir o
// download a um usuário específico. O que dá para validar sem login é que o anexo e o cartão dono
// ainda existem (não foram excluídos), evitando servir arquivos de registros já removidos logicamente.
app.MapGet("/api/anexos/{id:int}", async (int id, KanbanDbContext db, IAnexoStorage storage, CancellationToken ct) =>
{
    var anexo = await db.Anexos.Include(a => a.Cartao).FirstOrDefaultAsync(a => a.Id == id && !a.Excluido, ct);
    if (anexo is null || anexo.Cartao.Excluido) return Results.NotFound();
    var stream = await storage.ObterAsync(anexo.Caminho, ct);
    return Results.File(stream, anexo.ContentType, anexo.NomeOriginal);
});

async Task<List<LinhaExportacao>> ConsultarExportacaoAsync(FiltroExportacao filtro, KanbanDbContext db, CancellationToken ct)
{
    var consulta = db.Cartoes.AsNoTracking().AsSplitQuery()
        .Include(c => c.Lista).Include(c => c.Sistema).Include(c => c.Solicitante)
        .Include(c => c.Desenvolvedores).ThenInclude(d => d.Usuario)
        .Include(c => c.Etiquetas).ThenInclude(e => e.Etiqueta)
        .Where(c => !c.Excluido && !c.Lista.Excluido);

    if (!string.IsNullOrWhiteSpace(filtro.Busca)) consulta = consulta.Where(c => c.Titulo.Contains(filtro.Busca) || (c.Descricao != null && c.Descricao.Contains(filtro.Busca)));
    if (filtro.QuadroId.HasValue) consulta = consulta.Where(c => c.Lista.QuadroId == filtro.QuadroId.Value);
    if (filtro.ListaId.HasValue) consulta = consulta.Where(c => c.ListaId == filtro.ListaId.Value);
    if (filtro.SistemaId.HasValue) consulta = consulta.Where(c => c.SistemaId == filtro.SistemaId.Value);
    if (filtro.Prioridade.HasValue) consulta = consulta.Where(c => c.Prioridade == filtro.Prioridade.Value);
    if (filtro.DesenvolvedorId.HasValue) consulta = consulta.Where(c => c.Desenvolvedores.Any(d => d.UsuarioId == filtro.DesenvolvedorId.Value));
    if (filtro.ResponsavelId.HasValue) consulta = consulta.Where(c => c.Desenvolvedores.Any(d => d.UsuarioId == filtro.ResponsavelId.Value));
    if (filtro.SolicitanteId.HasValue) consulta = consulta.Where(c => c.SolicitanteId == filtro.SolicitanteId.Value);
    if (filtro.EtiquetaId.HasValue) consulta = consulta.Where(c => c.Etiquetas.Any(e => e.EtiquetaId == filtro.EtiquetaId.Value));
    if (filtro.PeriodoDe.HasValue) consulta = consulta.Where(c => c.CriadoEm >= filtro.PeriodoDe.Value.ToUniversalTime());
    if (filtro.PeriodoAte.HasValue) consulta = consulta.Where(c => c.CriadoEm < filtro.PeriodoAte.Value.Date.AddDays(1).ToUniversalTime());

    // Filtro de data do board: o campo (prazo, início ou criação) é escolhido na tela; cartões sem a data ficam de fora.
    if (filtro.DataDe.HasValue || filtro.DataAte.HasValue)
    {
        var de = filtro.DataDe?.Date.ToUniversalTime() ?? DateTime.MinValue;
        var ate = filtro.DataAte?.Date.AddDays(1).ToUniversalTime() ?? DateTime.MaxValue;
        consulta = filtro.CampoData?.ToLowerInvariant() switch
        {
            "inicio" => consulta.Where(c => c.DataInicio.HasValue && c.DataInicio >= de && c.DataInicio < ate),
            "criacao" => consulta.Where(c => c.CriadoEm >= de && c.CriadoEm < ate),
            _ => consulta.Where(c => c.Prazo.HasValue && c.Prazo >= de && c.Prazo < ate)
        };
    }

    var cartoes = await consulta.OrderBy(c => c.Lista.Ordem).ThenBy(c => c.Ordem).ToListAsync(ct);
    static string Data(DateTime? data) => data?.ToLocalTime().ToString("dd/MM/yyyy") ?? "";
    return cartoes.Select(c => new LinhaExportacao(
        c.Titulo, c.Lista.Nome, c.Sistema?.Nome ?? "", c.Solicitante?.Nome ?? "",
        string.Join(", ", c.Desenvolvedores.OrderByDescending(d => d.Principal).Select(d => d.Usuario.Nome)),
        string.Join(", ", c.Etiquetas.Select(e => e.Etiqueta.Nome)),
        c.Prioridade?.ToString() ?? "", Data(c.DataInicio), Data(c.Prazo), c.Estimativa?.ToString("0.##") ?? "", Data(c.CriadoEm))).ToList();
}

app.MapGet("/api/export/cartoes.xlsx", async ([AsParameters] FiltroExportacao filtro, KanbanDbContext db, IExportacaoService exportacao, CancellationToken ct) =>
{
    var arquivo = await exportacao.ExportarParaExcelAsync(await ConsultarExportacaoAsync(filtro, db, ct), "Cartoes", ct);
    return Results.File(arquivo, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "cartoes.xlsx");
});

app.MapGet("/api/export/cartoes.pdf", async ([AsParameters] FiltroExportacao filtro, KanbanDbContext db, IExportacaoService exportacao, CancellationToken ct) =>
{
    var arquivo = await exportacao.ExportarParaPdfAsync(await ConsultarExportacaoAsync(filtro, db, ct), "Cartões", ct);
    return Results.File(arquivo, "application/pdf", "cartoes.pdf");
});

// ── Webhook do GitHub (guia: docs/integracoes/github.md) ─────────────────
// Sem login real, a segurança vem da assinatura HMAC: só aceita chamadas assinadas com o segredo configurado.
app.MapPost("/api/integracoes/github", async (HttpRequest requisicao, KanbanDemandas.Web.Services.Integracoes.GitHubWebhookService servico, IConfiguration configuracao) =>
{
    if (!configuracao.GetValue("Integracoes:GitHub:Habilitado", false)) return Results.NotFound();
    var segredo = configuracao["Integracoes:GitHub:SegredoWebhook"];
    if (string.IsNullOrWhiteSpace(segredo)) return Results.Problem("Segredo do webhook não configurado na Parametrização.", statusCode: 503);
    using var leitor = new StreamReader(requisicao.Body);
    var corpo = await leitor.ReadToEndAsync();
    if (!KanbanDemandas.Web.Services.Integracoes.GitHubWebhookService.AssinaturaValida(corpo, requisicao.Headers["X-Hub-Signature-256"], segredo))
        return Results.Unauthorized();
    var resultado = await servico.ProcessarAsync(requisicao.Headers["X-GitHub-Event"].ToString(), corpo);
    return Results.Ok(resultado);
}).DisableAntiforgery();

app.MapHub<KanbanDemandas.Web.Hubs.QuadroHub>("/hubs/quadro");

// ── Rotas Blazor ──────────────────────────────────────────────────────────
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

internal sealed record FiltroExportacao(
    string? Busca, int? QuadroId, int? ListaId, int? SistemaId, KanbanDemandas.Core.Enums.Prioridade? Prioridade,
    int? DesenvolvedorId, int? ResponsavelId, int? SolicitanteId, int? EtiquetaId,
    DateTime? PeriodoDe, DateTime? PeriodoAte, string? CampoData, DateTime? DataDe, DateTime? DataAte);

internal sealed record LinhaExportacao(
    string Titulo, string Lista, string Sistema, string Solicitante, string Desenvolvedores, string Etiquetas,
    string Prioridade, string Inicio, string Prazo, string Estimativa, string CriadoEm);
