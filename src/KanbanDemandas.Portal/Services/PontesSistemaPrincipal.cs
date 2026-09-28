using System.Threading.Channels;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace KanbanDemandas.Portal.Services;

/// <summary>
/// Eventos de automação gerados no portal (cartão criado, comentário...). O portal não roda automações:
/// grava os eventos na fila do banco, que o sistema principal executa respeitando a mesma espera.
/// </summary>
public sealed class FilaEventosPortal : IFilaEventosAutomacao
{
    private readonly Channel<EventoAutomacao> _canal = Channel.CreateUnbounded<EventoAutomacao>(new UnboundedChannelOptions { SingleReader = true });

    public void Enfileirar(EventoAutomacao evento) => _canal.Writer.TryWrite(evento);

    public IAsyncEnumerable<EventoAutomacao> LerTodos(CancellationToken ct) => _canal.Reader.ReadAllAsync(ct);
}

public sealed class GravadorEventosPortal(FilaEventosPortal fila, IServiceScopeFactory scopeFactory, IConfiguration configuracao, ILogger<GravadorEventosPortal> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evento in fila.LerTodos(stoppingToken))
        {
            try
            {
                var atraso = TimeSpan.FromMinutes(Math.Max(0, configuracao.GetValue("Automacao:AtrasoMinutos", 10)));
                using var escopo = scopeFactory.CreateScope();
                await escopo.ServiceProvider.GetRequiredService<AgendaEventosAutomacao>().AgendarAsync(evento, atraso);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao gravar o evento de automação {Gatilho} do cartão {Cartao}.", evento.Gatilho, evento.CartaoId);
            }
        }
    }
}

/// <summary>
/// Avisa o sistema principal (pelo hub de tempo real dele) que quadros ou notificações mudaram,
/// para que telas abertas lá atualizem na hora. Falhas são ignoradas: o dado já está gravado no banco.
/// </summary>
public sealed class PublicadorViaSistemaPrincipal(IConfiguration configuracao, ILogger<PublicadorViaSistemaPrincipal> logger)
    : IPublicadorAlteracoes, IAsyncDisposable
{
    private readonly SemaphoreSlim _trava = new(1, 1);
    private HubConnection? _conexao;

    public async Task PublicarAsync(IReadOnlyCollection<int> quadroIds, IReadOnlyCollection<int> destinatariosNotificacao)
    {
        var url = configuracao["Portal:UrlSistemaPrincipal"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            var conexao = await ConectarAsync(url);
            await conexao.InvokeAsync("AvisarAlteracao", quadroIds.ToArray(), destinatariosNotificacao.ToArray());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Não foi possível avisar o sistema principal em {Url}.", url);
        }
    }

    private async Task<HubConnection> ConectarAsync(string url)
    {
        await _trava.WaitAsync();
        try
        {
            if (_conexao is null)
            {
                _conexao = new HubConnectionBuilder().WithUrl($"{url}/hubs/quadro").WithAutomaticReconnect().Build();
            }
            if (_conexao.State == HubConnectionState.Disconnected)
                await _conexao.StartAsync();
            return _conexao;
        }
        finally
        {
            _trava.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_conexao is not null) await _conexao.DisposeAsync();
    }
}
