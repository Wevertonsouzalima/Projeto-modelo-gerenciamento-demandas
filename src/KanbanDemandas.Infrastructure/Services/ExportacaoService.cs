using ClosedXML.Excel;
using KanbanDemandas.Core.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection;

namespace KanbanDemandas.Infrastructure.Services;

/// <summary>
/// Gera exportações em PDF (QuestPDF) e Excel (ClosedXML) a partir de qualquer lista de objetos.
/// </summary>
public class ExportacaoService : IExportacaoService
{
    public ExportacaoService()
    {
        // Licença community (gratuita para projetos internos)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> ExportarParaPdfAsync<T>(IEnumerable<T> dados, string titulo, CancellationToken ct = default)
    {
        var lista = dados.ToList();
        var propriedades = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);

                page.Header().Text(titulo)
                    .SemiBold().FontSize(14).FontColor(Colors.Blue.Darken3);

                page.Content().Table(table =>
                {
                    // Colunas dinâmicas
                    table.ColumnsDefinition(cols =>
                    {
                        foreach (var _ in propriedades)
                            cols.RelativeColumn();
                    });

                    // Cabeçalho
                    table.Header(header =>
                    {
                        foreach (var prop in propriedades)
                        {
                            header.Cell().Background(Colors.Blue.Darken3)
                                .Padding(4).Text(prop.Name)
                                .FontColor(Colors.White).FontSize(9).Bold();
                        }
                    });

                    // Linhas
                    var linha = 0;
                    foreach (var item in lista)
                    {
                        var bg = linha++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                        foreach (var prop in propriedades)
                        {
                            var valor = prop.GetValue(item)?.ToString() ?? "";
                            table.Cell().Background(bg).Padding(3)
                                .Text(valor).FontSize(8);
                        }
                    }
                });

                page.Footer().AlignRight()
                    .Text(text =>
                    {
                        text.Span("Gerado em ").FontSize(8);
                        text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8);
                        text.Span(" — Página ").FontSize(8);
                        text.CurrentPageNumber().FontSize(8);
                        text.Span(" de ").FontSize(8);
                        text.TotalPages().FontSize(8);
                    });
            });
        });

        var bytes = pdf.GeneratePdf();
        return Task.FromResult(bytes);
    }

    public Task<byte[]> ExportarParaExcelAsync<T>(IEnumerable<T> dados, string nomePlanilha, CancellationToken ct = default)
    {
        var lista = dados.ToList();
        var propriedades = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(nomePlanilha[..Math.Min(nomePlanilha.Length, 31)]);

        // Cabeçalho
        for (var col = 0; col < propriedades.Length; col++)
        {
            var cell = ws.Cell(1, col + 1);
            cell.Value = propriedades[col].Name;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Dados
        for (var row = 0; row < lista.Count; row++)
        {
            for (var col = 0; col < propriedades.Length; col++)
            {
                var valor = propriedades[col].GetValue(lista[row]);
                var cell = ws.Cell(row + 2, col + 1);

                cell.Value = valor switch
                {
                    null => XLCellValue.FromObject(""),
                    bool b => b,
                    DateTime dt => dt,
                    int i => i,
                    long l => l,
                    decimal d => d,
                    double dbl => dbl,
                    _ => valor.ToString() ?? ""
                };

                if (row % 2 == 1)
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
            }
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }
}
