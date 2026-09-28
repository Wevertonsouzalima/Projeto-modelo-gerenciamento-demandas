namespace KanbanDemandas.Core.Interfaces;

/// <summary>Resultado de gravar um anexo: onde ficou, se foi compactado e quanto ocupa.</summary>
public sealed record ArquivoArmazenado(string Caminho, bool Comprimido, long TamanhoArmazenado);

/// <summary>
/// Abstração de storage de arquivos (disco local ou blob).
/// </summary>
public interface IAnexoStorage
{
    /// <summary>Salva o conteúdo, compactando quando configurado e vantajoso.</summary>
    Task<ArquivoArmazenado> SalvarAsync(Stream conteudo, string nomeArmazenado, string contentType, CancellationToken ct = default);

    /// <summary>Devolve o conteúdo original (descompactado quando necessário).</summary>
    Task<Stream> ObterAsync(string caminho, CancellationToken ct = default);

    Task ExcluirAsync(string caminho, CancellationToken ct = default);

    /// <summary>Compacta um arquivo já armazenado; null quando não compensa ou o formato não está configurado.</summary>
    Task<ArquivoArmazenado?> CompactarAsync(string caminho, CancellationToken ct = default);
}
