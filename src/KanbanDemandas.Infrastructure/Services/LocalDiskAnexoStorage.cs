using System.IO.Compression;
using KanbanDemandas.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KanbanDemandas.Infrastructure.Services;

/// <summary>
/// Implementação de IAnexoStorage que salva arquivos no disco local.
/// Configuração: "Anexos:PastaBase", "Anexos:PoliticaColisao" (Renomear, Sobrescrever ou Rejeitar) e
/// compactação: "Anexos:Comprimir", "Anexos:ExtensoesComprimir" e "Anexos:GanhoMinimoPercentual".
/// Arquivos compactados ganham a extensão ".gz" e são descompactados na leitura.
/// Pode ser substituída por uma implementação de blob storage sem alterar o restante.
/// </summary>
public class LocalDiskAnexoStorage : IAnexoStorage
{
    private const string SufixoCompactado = ".gz";

    private readonly string _pastaBase;
    private readonly IConfiguration _config;
    private readonly ILogger<LocalDiskAnexoStorage> _logger;

    public LocalDiskAnexoStorage(IConfiguration config, ILogger<LocalDiskAnexoStorage> logger)
    {
        _pastaBase = Path.GetFullPath(config["Anexos:PastaBase"] ?? Path.Combine(AppContext.BaseDirectory, "uploads"));
        _config = config;
        _logger = logger;
        Directory.CreateDirectory(_pastaBase);
    }

    private string PoliticaColisao => _config["Anexos:PoliticaColisao"] ?? "Renomear";

    private bool DeveCompactar(string nome)
    {
        if (!bool.TryParse(_config["Anexos:Comprimir"], out var comprimir) || !comprimir) return false;
        var extensoes = (_config["Anexos:ExtensoesComprimir"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return extensoes.Contains(Path.GetExtension(nome), StringComparer.OrdinalIgnoreCase);
    }

    private double GanhoMinimo => Math.Clamp(int.TryParse(_config["Anexos:GanhoMinimoPercentual"], out var ganho) ? ganho : 10, 1, 90) / 100d;

    public async Task<ArquivoArmazenado> SalvarAsync(Stream conteudo, string nomeArmazenado, string contentType, CancellationToken ct = default)
    {
        var nome = NormalizarNome(nomeArmazenado);
        if (DeveCompactar(nome))
        {
            using var original = new MemoryStream();
            await conteudo.CopyToAsync(original, ct);
            var compactado = await CompactarEmMemoriaAsync(original, ct);
            if (compactado.Length <= original.Length * (1 - GanhoMinimo))
                return await GravarAsync(compactado, nome + SufixoCompactado, comprimido: true, ct);
            original.Position = 0;
            return await GravarAsync(original, nome, comprimido: false, ct);
        }
        return await GravarAsync(conteudo, nome, comprimido: false, ct);
    }

    private async Task<ArquivoArmazenado> GravarAsync(Stream conteudo, string nome, bool comprimido, CancellationToken ct)
    {
        var caminho = ResolverCaminho(nome);
        long tamanho;
        await using (var arquivo = new FileStream(caminho, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await conteudo.CopyToAsync(arquivo, ct);
            tamanho = arquivo.Length;
        }
        _logger.LogInformation("Anexo salvo: {Caminho} ({Tamanho} bytes{Compactado})", caminho, tamanho, comprimido ? ", compactado" : "");
        return new ArquivoArmazenado(caminho, comprimido, tamanho);
    }

    private static async Task<MemoryStream> CompactarEmMemoriaAsync(Stream original, CancellationToken ct)
    {
        original.Position = 0;
        var destino = new MemoryStream();
        await using (var gzip = new GZipStream(destino, CompressionLevel.SmallestSize, leaveOpen: true))
            await original.CopyToAsync(gzip, ct);
        destino.Position = 0;
        return destino;
    }

    // Remove diretórios e caracteres inválidos, impedindo que o nome escape da pasta base.
    private static string NormalizarNome(string nome)
    {
        var somenteArquivo = Path.GetFileName(nome);
        var invalidos = Path.GetInvalidFileNameChars();
        var limpo = new string(somenteArquivo.Select(c => invalidos.Contains(c) ? '_' : c).ToArray()).Trim(' ', '.');
        return string.IsNullOrWhiteSpace(limpo) ? $"{Guid.NewGuid():N}" : limpo;
    }

    private string ResolverCaminho(string nome)
    {
        var caminho = Path.Combine(_pastaBase, nome);
        if (!File.Exists(caminho)) return caminho;

        switch (PoliticaColisao.ToLowerInvariant())
        {
            case "sobrescrever":
                return caminho;
            case "rejeitar":
                throw new IOException($"Já existe um anexo armazenado com o nome '{nome}'.");
            default:
                var compactado = nome.EndsWith(SufixoCompactado, StringComparison.OrdinalIgnoreCase);
                var semSufixo = compactado ? nome[..^SufixoCompactado.Length] : nome;
                var baseNome = Path.GetFileNameWithoutExtension(semSufixo);
                var extensao = Path.GetExtension(semSufixo) + (compactado ? SufixoCompactado : "");
                for (var n = 1; ; n++)
                {
                    var alternativo = Path.Combine(_pastaBase, $"{baseNome} ({n}){extensao}");
                    if (!File.Exists(alternativo)) return alternativo;
                }
        }
    }

    public Task<Stream> ObterAsync(string caminho, CancellationToken ct = default)
    {
        if (!File.Exists(caminho))
            throw new FileNotFoundException("Anexo não encontrado.", caminho);

        Stream arquivo = File.OpenRead(caminho);
        return Task.FromResult(caminho.EndsWith(SufixoCompactado, StringComparison.OrdinalIgnoreCase)
            ? new GZipStream(arquivo, CompressionMode.Decompress)
            : arquivo);
    }

    public Task ExcluirAsync(string caminho, CancellationToken ct = default)
    {
        if (File.Exists(caminho))
            File.Delete(caminho);

        return Task.CompletedTask;
    }

    public async Task<ArquivoArmazenado?> CompactarAsync(string caminho, CancellationToken ct = default)
    {
        if (!File.Exists(caminho) || caminho.EndsWith(SufixoCompactado, StringComparison.OrdinalIgnoreCase) || !DeveCompactar(caminho))
            return null;

        using var original = new MemoryStream();
        await using (var arquivo = File.OpenRead(caminho))
            await arquivo.CopyToAsync(original, ct);
        var compactado = await CompactarEmMemoriaAsync(original, ct);
        if (compactado.Length > original.Length * (1 - GanhoMinimo)) return null;

        var destino = caminho + SufixoCompactado;
        await using (var arquivo = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None))
            await compactado.CopyToAsync(arquivo, ct);
        File.Delete(caminho);
        return new ArquivoArmazenado(destino, true, compactado.Length);
    }
}
