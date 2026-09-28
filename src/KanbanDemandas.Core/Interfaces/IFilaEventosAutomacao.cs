using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Interfaces;

/// <summary>Algo que aconteceu com um cartão e pode disparar regras de automação.</summary>
public sealed record EventoAutomacao(
    TipoGatilho Gatilho,
    int CartaoId,
    int UsuarioId,
    int Profundidade,
    IReadOnlyList<int> RegrasNaCadeia,
    int? ListaId = null,
    CampoMonitorado Campo = CampoMonitorado.Qualquer,
    int? UsuarioAlvoId = null);

/// <summary>Recebe eventos gerados ao gravar alterações; o processamento acontece fora da requisição.</summary>
public interface IFilaEventosAutomacao
{
    void Enfileirar(EventoAutomacao evento);
}

/// <summary>
/// Contexto da execução de automação em andamento no fluxo assíncrono atual. Alterações gravadas durante uma
/// execução geram eventos com profundidade maior e a cadeia de regras, o que permite barrar loops.
/// </summary>
public static class ContextoAutomacao
{
    private static readonly AsyncLocal<(int Profundidade, IReadOnlyList<int> Regras)?> Atual = new();

    public static int Profundidade => Atual.Value?.Profundidade ?? 0;
    public static IReadOnlyList<int> RegrasNaCadeia => Atual.Value?.Regras ?? [];

    public static IDisposable Entrar(int profundidade, IReadOnlyList<int> regras)
    {
        var anterior = Atual.Value;
        Atual.Value = (profundidade, regras);
        return new Restaurar(() => Atual.Value = anterior);
    }

    private sealed class Restaurar(Action acao) : IDisposable
    {
        public void Dispose() => acao();
    }
}
