namespace KanbanDemandas.Core.Enums;

/// <summary>O que um campo automático faz quando o cartão entra de novo na mesma lista.</summary>
public enum RegraReentrada
{
    /// <summary>Mantém o valor da primeira entrada (ex.: data de início real).</summary>
    ManterPrimeiro = 0,

    /// <summary>Substitui pelo valor da entrada mais recente.</summary>
    AtualizarUltimo = 1,

    /// <summary>Registra uma nova ocorrência a cada entrada, preservando as anteriores.</summary>
    NovoRegistro = 2
}
