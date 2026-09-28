using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Regras;

public static class CamposCartao
{
    /// <summary>Valor mais recente do campo: última entrada na lista e, em empate, o último preenchimento.</summary>
    public static string? ValorMaisRecente(IEnumerable<ValorCampoCartao> valores, int definicaoCampoId)
        => valores.Where(v => v.DefinicaoCampoId == definicaoCampoId)
            .OrderByDescending(v => v.NumeroEntradaNaLista).ThenByDescending(v => v.DataPreenchimento)
            .FirstOrDefault()?.Valor;

    public static string Formatar(TipoCampo tipo, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return "—";
        return tipo switch
        {
            TipoCampo.Checkbox => bool.TryParse(valor, out var marcado) && marcado ? "Sim" : "Não",
            TipoCampo.Data when DateTime.TryParse(valor, out var data) => data.ToString("dd/MM/yyyy"),
            _ => valor
        };
    }
}
