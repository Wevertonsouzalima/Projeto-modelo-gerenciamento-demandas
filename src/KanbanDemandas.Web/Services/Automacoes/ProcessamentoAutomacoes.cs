using System.Threading.Channels;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure.Data;
using KanbanDemandas.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace KanbanDemandas.Web.Services.Automacoes;

/// <summary>Fila em memória dos eventos de automação gerados ao gravar alterações.</summary>
public sealed class FilaEventosAutomacao : IFilaEventosAutomacao
{
    private readonly Channel<EventoAutomacao> _canal = Channel.CreateUnbounded<EventoAutomacao>(new UnboundedChannelOptions { SingleReader = true });

    public void Enfileirar(EventoAutomacao evento) => _canal.Writer.TryWrite(evento);

    public IAsyncEnumerable<EventoAutomacao> LerTodos(CancellationToken ct) => _canal.Reader.ReadAllAsync(ct);
}

/// <summary>
/// Consome a fila em memória: eventos de pessoas vão para a espera; eventos gerados por automações
/// (continuação de uma cadeia) rodam na hora. Cada evento roda como o usuário que o gerou.
/// </summary>
public sealed class ProcessadorEventosAutomacao(FilaEventosAutomacao fila, IServiceScopeFactory scopeFactory, IConfiguration configuracao,
    ILogger<ProcessadorEventosAutomacao> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evento in fila.LerTodos(stoppingToken))
        {
            try
            {
                var atraso = TimeSpan.FromMinutes(Math.Max(0, configuracao.GetValue("Automacao:AtrasoMinutos", 10)));
                using var escopo = scopeFactory.CreateScope();
                if (evento.Profundidade == 0 && atraso > TimeSpan.Zero)
                {
                    await escopo.ServiceProvider.GetRequiredService<AgendaEventosAutomacao>().AgendarAsync(evento, atraso);
                    continue;
                }
                await ExecutarEventoAsync(escopo.ServiceProvider, evento);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao processar o evento de automação {Gatilho} do cartão {Cartao}.", evento.Gatilho, evento.CartaoId);
            }
        }
    }

    public static async Task ExecutarEventoAsync(IServiceProvider servicos, EventoAutomacao evento)
    {
        servicos.GetRequiredService<SessaoUsuario>().UsuarioId = evento.UsuarioId;
        using (ContextoAutomacao.Entrar(evento.Profundidade, evento.RegrasNaCadeia))
            await servicos.GetRequiredService<MotorAutomacoes>().ProcessarEventoAsync(evento);
    }
}

/// <summary>
/// Verificação periódica: executa os eventos cuja espera terminou e os gatilhos de tempo
/// (prazos, cartões parados, agendamentos e sprints). O intervalo é relido a cada ciclo.
/// </summary>
public sealed class AgendadorAutomacoes(IServiceScopeFactory scopeFactory, IConfiguration configuracao, ILogger<AgendadorAutomacoes> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ExecutarCicloAsync();
            var intervalo = TimeSpan.FromSeconds(Math.Clamp(configuracao.GetValue("Automacao:IntervaloVerificacaoSegundos", 60), 15, 3600));
            try { await Task.Delay(intervalo, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task ExecutarCicloAsync()
    {
        try
        {
            List<EventoAutomacao> vencidos;
            using (var escopo = scopeFactory.CreateScope())
                vencidos = await escopo.ServiceProvider.GetRequiredService<AgendaEventosAutomacao>().RetirarVencidosAsync(DateTime.UtcNow);
            foreach (var evento in vencidos)
            {
                try
                {
                    using var escopo = scopeFactory.CreateScope();
                    await ProcessadorEventosAutomacao.ExecutarEventoAsync(escopo.ServiceProvider, evento);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Falha ao executar o evento adiado {Gatilho} do cartão {Cartao}.", evento.Gatilho, evento.CartaoId);
                }
            }

            using (var escopo = scopeFactory.CreateScope())
            {
                escopo.ServiceProvider.GetRequiredService<SessaoUsuario>().UsuarioId = configuracao.GetValue("Automacao:UsuarioSistemaId", 1);
                await escopo.ServiceProvider.GetRequiredService<MotorAutomacoes>().ProcessarGatilhosDeTempoAsync(DateTime.Now);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha na verificação periódica de automações.");
        }
    }
}
