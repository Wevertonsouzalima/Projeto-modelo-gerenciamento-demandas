namespace KanbanDemandas.Core.Enums;

/// <summary>Campos fixos do cartão que uma lista pode exigir ou proibir na etapa.</summary>
[Flags]
public enum CamposFixosCartao
{
    Nenhum = 0,
    Desenvolvedor = 1,
    Prazo = 2,
    Estimativa = 4,
    Sistema = 8,
    Solicitante = 16,
    DataInicio = 32
}
