using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure;
using KanbanDemandas.Infrastructure.Configuracao;
using KanbanDemandas.Infrastructure.Services;
using KanbanDemandas.Portal.Components;
using KanbanDemandas.Portal.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Mesmos parâmetros do sistema principal (tabela ParametrosSistema), inclusive a configuração do portal.
var fonteParametros = new ParametrosBancoConfigurationSource(builder.Configuration.GetConnectionString("DefaultConnection"));
((IConfigurationBuilder)builder.Configuration).Add(fonteParametros);
builder.Services.AddSingleton(fonteParametros.Provider);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<MovimentacaoCartaoService>();
builder.Services.AddScoped<AgendaEventosAutomacao>();
builder.Services.AddScoped<PortalService>();

// Pontes com o sistema principal: eventos de automação vão para a fila no banco; avisos de tempo real vão pelo hub dele.
builder.Services.AddSingleton<FilaEventosPortal>();
builder.Services.AddSingleton<IFilaEventosAutomacao>(sp => sp.GetRequiredService<FilaEventosPortal>());
builder.Services.AddHostedService<GravadorEventosPortal>();
builder.Services.AddSingleton<PublicadorViaSistemaPrincipal>();
builder.Services.AddSingleton<IPublicadorAlteracoes>(sp => sp.GetRequiredService<PublicadorViaSistemaPrincipal>());

var app = builder.Build();

if (app.Environment.IsProduction())
{
    app.Logger.LogWarning("Portal em Production sem autenticação real: qualquer pessoa com acesso à URL pode escolher qualquer usuário.");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
