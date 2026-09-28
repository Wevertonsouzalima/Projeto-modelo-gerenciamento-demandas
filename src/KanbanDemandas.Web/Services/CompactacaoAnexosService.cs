using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KanbanDemandas.Web.Services;

public sealed record ResultadoCompactacao(int Analisados, int Compactados, long BytesEconomizados, int Erros);

/// <summary>Compacta anexos gravados antes da compactação existir (ou antes de o formato ser incluído na lista).</summary>
public sealed class CompactacaoAnexosService(KanbanDbContext db, IAnexoStorage storage, ILogger<CompactacaoAnexosService> logger)
{
    public async Task<ResultadoCompactacao> CompactarExistentesAsync(CancellationToken ct = default)
    {
        var anexos = await db.Anexos.Where(a => !a.Excluido && !a.Comprimido).ToListAsync(ct);
        var compactados = 0;
        var erros = 0;
        long economia = 0;
        foreach (var anexo in anexos)
        {
            try
            {
                var resultado = await storage.CompactarAsync(anexo.Caminho, ct);
                if (resultado is null) continue;
                var antes = anexo.TamanhoArmazenadoBytes > 0 ? anexo.TamanhoArmazenadoBytes : anexo.TamanhoBytes;
                anexo.Caminho = resultado.Caminho;
                anexo.NomeArmazenado = Path.GetFileName(resultado.Caminho);
                anexo.Comprimido = true;
                anexo.TamanhoArmazenadoBytes = resultado.TamanhoArmazenado;
                economia += antes - resultado.TamanhoArmazenado;
                compactados++;
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                erros++;
                logger.LogWarning(ex, "Falha ao compactar o anexo {Anexo}.", anexo.Id);
            }
        }
        return new ResultadoCompactacao(anexos.Count, compactados, economia, erros);
    }
}
