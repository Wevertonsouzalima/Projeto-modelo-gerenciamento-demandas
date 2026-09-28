namespace KanbanDemandas.Core.Enums;

/// <summary>Como um campo de lista é preenchido quando o cartão entra na lista.</summary>
public enum PreenchimentoAutomatico
{
    /// <summary>O usuário preenche.</summary>
    Manual = 0,

    /// <summary>Data em que o cartão entrou na lista (campos do tipo Data).</summary>
    DataEntrada = 1,

    /// <summary>Nome do usuário que moveu o cartão para a lista.</summary>
    UsuarioQueMoveu = 2
}
