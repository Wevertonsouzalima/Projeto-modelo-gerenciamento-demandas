using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Entities;

/// <summary>Dia não útil considerado nos cálculos de dias úteis (prazos, remarcações e indicadores).</summary>
public class Feriado : EntidadeBase
{
    public DateTime Data { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoFeriado Tipo { get; set; } = TipoFeriado.Nacional;

    /// <summary>Repete todo ano no mesmo dia e mês (ex.: aniversário da cidade); o ano de <see cref="Data"/> é ignorado.</summary>
    public bool RecorrenteAnual { get; set; }
}
