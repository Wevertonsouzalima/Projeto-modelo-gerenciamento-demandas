namespace KanbanDemandas.Portal.Services;

public static class Formatacao
{
    /// <summary>Tamanho de arquivo legível (B, KB, MB).</summary>
    public static string Tamanho(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024d:0.#} KB";
        return $"{bytes / 1024d / 1024d:0.#} MB";
    }
}
