namespace KanbanDemandas.Core.Entities;

/// <summary>
/// Valor de configuração alterado pela tela de parametrização. A chave segue o formato do appsettings
/// (ex.: "Automacao:AtrasoMinutos"); sem registro aqui, vale o valor do appsettings.
/// </summary>
public class ParametroSistema
{
    public string Chave { get; set; } = string.Empty;
    public string? Valor { get; set; }
    public DateTime AlteradoEm { get; set; }
    public int AlteradoPorId { get; set; }
}
