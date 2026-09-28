namespace KanbanDemandas.Core.Interfaces;

public interface IExportacaoService
{
    Task<byte[]> ExportarParaPdfAsync<T>(IEnumerable<T> dados, string titulo, CancellationToken ct = default);
    Task<byte[]> ExportarParaExcelAsync<T>(IEnumerable<T> dados, string nomePlanilha, CancellationToken ct = default);
}
