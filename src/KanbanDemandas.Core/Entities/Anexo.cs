namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Arquivo anexado a um cartão.
/// </summary>
public class Anexo : EntidadeBase
{
    public int CartaoId { get; set; }
    public Cartao Cartao { get; set; } = null!;

    public string NomeOriginal { get; set; } = string.Empty;

    /// <summary>Nome do arquivo armazenado no disco/blob (pode diferir do original para evitar colisões).</summary>
    public string NomeArmazenado { get; set; } = string.Empty;

    /// <summary>Caminho relativo ou URL do arquivo no storage.</summary>
    public string Caminho { get; set; } = string.Empty;

    /// <summary>Tipo MIME do arquivo.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Tamanho em bytes.</summary>
    public long TamanhoBytes { get; set; }

    /// <summary>Arquivo guardado compactado (gzip); o download devolve o conteúdo original.</summary>
    public bool Comprimido { get; set; }

    /// <summary>Tamanho ocupado no armazenamento (menor que <see cref="TamanhoBytes"/> quando comprimido).</summary>
    public long TamanhoArmazenadoBytes { get; set; }

    public int EnviadoPorId { get; set; }
    public Usuario EnviadoPor { get; set; } = null!;
}
