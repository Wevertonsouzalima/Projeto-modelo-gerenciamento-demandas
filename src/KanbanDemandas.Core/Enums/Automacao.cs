namespace KanbanDemandas.Core.Enums;

/// <summary>O que dispara uma regra de automação. Valores ≥ 20 são gatilhos de tempo (verificados periodicamente).</summary>
public enum TipoGatilho
{
    CartaoCriado = 1,
    CartaoEntrouNaLista = 2,
    CartaoSaiuDaLista = 3,
    CampoAlterado = 4,
    CartaoAtribuido = 5,
    ComentarioAdicionado = 6,
    ChecklistConcluido = 7,
    CartaoBloqueado = 8,
    ConflitoDatas = 9,
    CommitVinculado = 10,
    PullRequestAberto = 11,
    PullRequestMergeado = 12,

    PrazoProximo = 20,
    PrazoVencido = 21,
    ParadoNaLista = 22,
    Agendado = 23,
    SprintIniciada = 24,
    SprintEncerrando = 25
}

/// <summary>Campo observado pelo gatilho "Campo alterado".</summary>
public enum CampoMonitorado
{
    Qualquer = 0,
    Prazo = 1,
    DataInicio = 2,
    Prioridade = 3,
    Desenvolvedores = 4,
    Etiquetas = 5,
    Sistema = 6,
    Solicitante = 7,
    Estimativa = 8,
    CampoPersonalizado = 9
}

public enum TipoCondicao
{
    Lista = 1,
    Prioridade = 2,
    Etiqueta = 3,
    Sistema = 4,
    Desenvolvedor = 5,
    Solicitante = 6,
    CampoPersonalizado = 7,
    TemPrazo = 8,
    Atrasado = 9,
    Bloqueado = 10,
    DiasNaLista = 11,
    Estimativa = 12,
    TemConflito = 13,
    PrazoEmRisco = 14,
    ChecklistCompleto = 15,
    Titulo = 16,
    DiasParaPrazo = 17
}

public enum OperadorCondicao
{
    EstaEm = 1,
    NaoEstaEm = 2,
    Igual = 3,
    Diferente = 4,
    Contem = 5,
    NaoContem = 6,
    Vazio = 7,
    NaoVazio = 8,
    MaiorOuIgual = 9,
    MenorOuIgual = 10,
    Sim = 11,
    Nao = 12
}

/// <summary>Quem recebe notificações e e-mails de uma ação.</summary>
[Flags]
public enum DestinatariosAutomacao
{
    Nenhum = 0,
    Desenvolvedores = 1,
    DesenvolvedorPrincipal = 2,
    Solicitante = 4,
    Criador = 8,
    UsuariosEspecificos = 16
}

public enum ModoAtribuicao
{
    UsuarioEspecifico = 1,
    MenorCarga = 2,
    Revezamento = 3,
    Solicitante = 4,
    QuemDisparou = 5
}

public enum BaseDataAutomacao
{
    Hoje = 1,
    PrazoAtual = 2,
    Limpar = 3
}

/// <summary>Em um conflito de datas, qual cartão cede a vez.</summary>
public enum EstrategiaConflito
{
    MenorPrioridade = 1,
    MaisRecente = 2,
    NaoIniciado = 3,
    MaisAntigo = 4
}

public enum ResolucaoConflito
{
    EmpurrarDatas = 1,
    MoverParaLista = 2,
    ApenasAvisar = 3
}

public enum ModoAplicacao
{
    Sugerir = 1,
    Aplicar = 2
}

public enum SeveridadeAlerta
{
    Info = 1,
    Aviso = 2,
    Critico = 3
}

public enum StatusExecucaoAutomacao
{
    Sucesso = 1,
    Parcial = 2,
    Erro = 3,
    Desfeita = 4,
    Simulada = 5
}

public enum StatusSugestao
{
    Pendente = 1,
    Aprovada = 2,
    Rejeitada = 3,
    Descartada = 4
}
