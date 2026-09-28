using System.Text.RegularExpressions;

namespace KanbanDemandas.Core.Regras;

/// <summary>Código legível do cartão (ex.: KB-123), usado como protocolo no portal e como referência em commits e PRs.</summary>
public static class CodigoCartao
{
    public const string PrefixoPadrao = "KB";

    public static string Formatar(int cartaoId, string? prefixo = null) => $"{Normalizar(prefixo)}-{cartaoId}";

    private static string Normalizar(string? prefixo) => string.IsNullOrWhiteSpace(prefixo) ? PrefixoPadrao : prefixo.Trim().TrimEnd('-').ToUpperInvariant();

    /// <summary>IDs de cartão citados no texto (ex.: "KB-12 corrige KB-7", "feature/kb-12-login").</summary>
    public static IReadOnlyList<int> Extrair(string? texto, string? prefixo = null)
    {
        if (string.IsNullOrWhiteSpace(texto)) return [];
        var padrao = new Regex($@"(?<![A-Za-z0-9]){Regex.Escape(Normalizar(prefixo))}[-_ ]?(\d+)\b", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
        return padrao.Matches(texto).Select(m => int.Parse(m.Groups[1].Value)).Where(id => id > 0).Distinct().ToList();
    }
}
