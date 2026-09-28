using KanbanDemandas.Infrastructure.Configuracao;
using System.Globalization;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KanbanDemandas.Web.Configuracao;

/// <summary>Lê e grava os parâmetros da tela; ao salvar, a configuração é recarregada sem reiniciar o sistema.</summary>
public sealed class ParametrosService(
    KanbanDbContext db,
    IConfiguration configuracao,
    ParametrosBancoConfigurationProvider provedor,
    IUsuarioAtualProvider usuarioProvider)
{
    public string? ValorAtual(string chave) => configuracao[chave];

    /// <summary>Valor vindo do appsettings (sem a sobreposição do banco).</summary>
    public string? ValorPadrao(string chave)
    {
        if (configuracao is not IConfigurationRoot raiz) return null;
        foreach (var fonte in raiz.Providers.Reverse())
        {
            if (fonte is ParametrosBancoConfigurationProvider) continue;
            if (fonte.TryGet(chave, out var valor)) return valor;
        }
        return null;
    }

    public async Task<HashSet<string>> ChavesAlteradasAsync()
        => (await db.ParametrosSistema.AsNoTracking().Select(p => p.Chave).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static string? Validar(ParametroDefinicao definicao, string? valor)
    {
        var texto = valor?.Trim() ?? "";
        switch (definicao.Tipo)
        {
            case TipoParametro.Inteiro:
            case TipoParametro.Decimal:
            case TipoParametro.Usuario:
            case TipoParametro.Quadro:
            case TipoParametro.Lista:
                if (!decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var numero))
                    return $"{definicao.Nome}: informe um número.";
                if (definicao.Tipo != TipoParametro.Decimal && numero != Math.Truncate(numero)) return $"{definicao.Nome}: informe um número inteiro.";
                if (definicao.Minimo.HasValue && numero < definicao.Minimo) return $"{definicao.Nome}: mínimo {definicao.Minimo}.";
                if (definicao.Maximo.HasValue && numero > definicao.Maximo) return $"{definicao.Nome}: máximo {definicao.Maximo}.";
                break;
            case TipoParametro.Booleano when !bool.TryParse(texto, out _):
                return $"{definicao.Nome}: valor inválido.";
            case TipoParametro.Horario when !TimeSpan.TryParse(texto, out _):
                return $"{definicao.Nome}: informe o horário como HH:mm.";
            case TipoParametro.Segredo when texto.Length is > 0 and < 16:
                return $"{definicao.Nome}: use pelo menos 16 caracteres.";
            case TipoParametro.MultiOpcoes when texto.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any(v => definicao.Opcoes?.Any(o => o.Valor == v) != true):
                return $"{definicao.Nome}: opção inválida.";
            case TipoParametro.Opcoes when definicao.Opcoes?.Any(o => o.Valor == texto) != true:
                return $"{definicao.Nome}: opção inválida.";
            case TipoParametro.Texto when definicao.Chave is "Aplicacao:UrlBase" or "Portal:UrlBase" && texto.Length > 0
                && !(Uri.TryCreate(texto, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"):
                return $"{definicao.Nome}: informe um endereço começando com http:// ou https://.";
        }
        return null;
    }

    /// <summary>Grava os valores; igual ao padrão do appsettings = remove a sobreposição.</summary>
    public async Task SalvarAsync(IReadOnlyDictionary<string, string?> valores)
    {
        var existentes = await db.ParametrosSistema.ToDictionaryAsync(p => p.Chave, StringComparer.OrdinalIgnoreCase);
        foreach (var (chave, valorBruto) in valores)
        {
            if (CatalogoParametros.Buscar(chave) is null) continue;
            var valor = valorBruto?.Trim() ?? "";
            var igualAoPadrao = valor == (ValorPadrao(chave) ?? "");
            if (existentes.TryGetValue(chave, out var existente))
            {
                if (igualAoPadrao) db.ParametrosSistema.Remove(existente);
                else if (existente.Valor != valor)
                {
                    existente.Valor = valor;
                    existente.AlteradoEm = DateTime.UtcNow;
                    existente.AlteradoPorId = usuarioProvider.ObterIdUsuarioAtual();
                }
            }
            else if (!igualAoPadrao)
            {
                db.ParametrosSistema.Add(new ParametroSistema
                {
                    Chave = chave, Valor = valor, AlteradoEm = DateTime.UtcNow, AlteradoPorId = usuarioProvider.ObterIdUsuarioAtual()
                });
            }
        }
        await db.SaveChangesAsync();
        provedor.Recarregar();
    }

    public async Task RestaurarAsync(string chave)
    {
        var existente = await db.ParametrosSistema.FirstOrDefaultAsync(p => p.Chave == chave);
        if (existente is null) return;
        db.ParametrosSistema.Remove(existente);
        await db.SaveChangesAsync();
        provedor.Recarregar();
    }
}
