using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Web.Services.Automacoes;

public sealed record ModeloProntoAutomacao(string Nome, string Descricao, string Icone, Func<int, RegraAutomacao> Criar);

/// <summary>Receitas de automação prontas para abrir no editor e ajustar (listas e pessoas ficam para escolher).</summary>
public static class ModelosProntosAutomacao
{
    private static AcaoAutomacao Acao(TipoAcaoAutomacao tipo, ParametrosAcao p, int ordem)
        => new() { Tipo = tipo, Ordem = ordem, ParametrosJson = JsonAutomacao.Serializar(p) };

    private static RegraAutomacao Regra(int quadroId, string nome, string descricao, TipoGatilho gatilho, ParametrosGatilho? gatilhoParametros,
        List<Condicao>? condicoes, params (TipoAcaoAutomacao Tipo, ParametrosAcao Parametros)[] acoes)
    {
        var regra = new RegraAutomacao
        {
            QuadroId = quadroId, Nome = nome, Descricao = descricao, Gatilho = gatilho,
            ParametrosGatilhoJson = gatilhoParametros is null ? null : JsonAutomacao.Serializar(gatilhoParametros),
            CondicoesJson = condicoes is { Count: > 0 } ? JsonAutomacao.Serializar(condicoes) : null
        };
        for (var i = 0; i < acoes.Length; i++) regra.Acoes.Add(Acao(acoes[i].Tipo, acoes[i].Parametros, i));
        return regra;
    }

    public static readonly IReadOnlyList<ModeloProntoAutomacao> Todos =
    [
        new("Resolver conflitos de agenda (sugerir)",
            "Quando dois cartões do mesmo desenvolvedor se sobrepõem, sugere remarcar o mais recente para o primeiro período livre.",
            "EventBusy",
            q => Regra(q, "Resolver conflitos de agenda", "Sugere novas datas para o cartão mais recente em conflito.", TipoGatilho.ConflitoDatas, null, null,
                (TipoAcaoAutomacao.ResolverConflitoDatas, new ParametrosAcao { Estrategia = EstrategiaConflito.MaisRecente, Resolucao = ResolucaoConflito.EmpurrarDatas, Modo = ModoAplicacao.Sugerir }))),

        new("Conflito: remarcar e voltar para A Fazer",
            "Aplica direto: o cartão que ainda não começou é remarcado e volta para a lista escolhida (ex.: A Fazer).",
            "Update",
            q => Regra(q, "Conflito: remarcar e devolver", "Remarca o cartão não iniciado e o move para a fila.", TipoGatilho.ConflitoDatas, null, null,
                (TipoAcaoAutomacao.ResolverConflitoDatas, new ParametrosAcao { Estrategia = EstrategiaConflito.NaoIniciado, Resolucao = ResolucaoConflito.EmpurrarDatas, Modo = ModoAplicacao.Aplicar }),
                (TipoAcaoAutomacao.NotificarResponsavel, new ParametrosAcao { Destinatarios = DestinatariosAutomacao.Desenvolvedores, Texto = "\"{titulo}\" foi remarcado por conflito de agenda: {inicio} a {prazo}." }))),

        new("Escalonar cartão parado",
            "Cartão há 5 dias na mesma coluna ganha alerta, sobe a prioridade e avisa desenvolvedores e solicitante.",
            "HourglassBottom",
            q => Regra(q, "Escalonar cartão parado", "Evita cartões esquecidos.", TipoGatilho.ParadoNaLista, new ParametrosGatilho { Dias = 5 }, null,
                (TipoAcaoAutomacao.CriarAlerta, new ParametrosAcao { Severidade = SeveridadeAlerta.Aviso, Texto = "Parado há {dias_na_lista} dias em {lista}" }),
                (TipoAcaoAutomacao.DefinirPrioridade, new ParametrosAcao { AumentarUmNivel = true }),
                (TipoAcaoAutomacao.NotificarResponsavel, new ParametrosAcao { Destinatarios = DestinatariosAutomacao.Desenvolvedores | DestinatariosAutomacao.Solicitante, Texto = "\"{titulo}\" está parado há {dias_na_lista} dias em {lista}." }))),

        new("Lembrete de prazo por e-mail",
            "Dois dias antes do prazo, envia e-mail e notificação aos desenvolvedores.",
            "Alarm",
            q => Regra(q, "Lembrete de prazo", "Aviso antecipado de vencimento.", TipoGatilho.PrazoProximo, new ParametrosGatilho { Dias = 2 }, null,
                (TipoAcaoAutomacao.EnviarEmail, new ParametrosAcao { Destinatarios = DestinatariosAutomacao.Desenvolvedores, Assunto = "Prazo próximo: {titulo}", Texto = "Olá,\n\nO cartão \"{titulo}\" vence em {prazo} e está em \"{lista}\".\n\n{link}" }),
                (TipoAcaoAutomacao.NotificarResponsavel, new ParametrosAcao { Destinatarios = DestinatariosAutomacao.Desenvolvedores, Texto = "\"{titulo}\" vence em {prazo}." }))),

        new("Cartão atrasado",
            "Quando o prazo vence: alerta crítico no cartão e e-mail para desenvolvedores e solicitante (repete a cada 3 dias).",
            "ReportProblem",
            q => Regra(q, "Cartão atrasado", "Cobrança automática de atrasos.", TipoGatilho.PrazoVencido, new ParametrosGatilho { Dias = 3 }, null,
                (TipoAcaoAutomacao.CriarAlerta, new ParametrosAcao { Severidade = SeveridadeAlerta.Critico, Texto = "Atrasado: prazo era {prazo}" }),
                (TipoAcaoAutomacao.EnviarEmail, new ParametrosAcao { Destinatarios = DestinatariosAutomacao.Desenvolvedores | DestinatariosAutomacao.Solicitante, Assunto = "Atrasado: {titulo}", Texto = "O cartão \"{titulo}\" passou do prazo ({prazo}).\n\nDesenvolvedores: {desenvolvedores}\n\n{link}" }))),

        new("Resumo diário por e-mail",
            "Todo dia útil às 08:00, cada desenvolvedor recebe seus cartões atrasados, vencendo, bloqueados e em conflito.",
            "MarkEmailUnread",
            q => Regra(q, "Resumo diário", "Agenda do dia de cada desenvolvedor.", TipoGatilho.Agendado, new ParametrosGatilho { Horario = "08:00" }, null,
                (TipoAcaoAutomacao.EnviarResumo, new ParametrosAcao { Destinatarios = DestinatariosAutomacao.Desenvolvedores }))),

        new("Atribuir por menor carga",
            "Cartão sem desenvolvedor que entra na lista escolhida é atribuído a quem tem menos pontos em andamento.",
            "Balance",
            q => Regra(q, "Distribuir cartões pela carga", "Balanceamento automático de trabalho.", TipoGatilho.CartaoEntrouNaLista, null,
                [new Condicao { Tipo = TipoCondicao.Desenvolvedor, Operador = OperadorCondicao.Vazio }],
                (TipoAcaoAutomacao.AtribuirDesenvolvedor, new ParametrosAcao { ModoAtribuicao = ModoAtribuicao.MenorCarga }))),

        new("Prazo em risco",
            "Todo dia útil às 09:00, marca cartões cuja estimativa não cabe nos dias úteis até o prazo e avisa os desenvolvedores.",
            "TrendingDown",
            q => Regra(q, "Prazo em risco", "Detecta prazos que não fecham com a estimativa.", TipoGatilho.Agendado, new ParametrosGatilho { Horario = "09:00" },
                [new Condicao { Tipo = TipoCondicao.PrazoEmRisco, Operador = OperadorCondicao.Sim }],
                (TipoAcaoAutomacao.CriarAlerta, new ParametrosAcao { Severidade = SeveridadeAlerta.Aviso, Texto = "Prazo em risco: a estimativa não cabe até {prazo}" }),
                (TipoAcaoAutomacao.NotificarResponsavel, new ParametrosAcao { Destinatarios = DestinatariosAutomacao.Desenvolvedores, Texto = "\"{titulo}\" provavelmente não fecha até {prazo}." }))),

        new("Ao concluir: limpar alertas e avisar solicitante",
            "Quando o cartão entra na lista escolhida (ex.: Concluído), resolve alertas e envia e-mail ao solicitante.",
            "TaskAlt",
            q => Regra(q, "Entrega concluída", "Fecha o ciclo com o solicitante.", TipoGatilho.CartaoEntrouNaLista, null, null,
                (TipoAcaoAutomacao.ResolverAlertas, new ParametrosAcao { TodosAlertasDoCartao = true }),
                (TipoAcaoAutomacao.EnviarEmail, new ParametrosAcao { Destinatarios = DestinatariosAutomacao.Solicitante, Assunto = "Concluído: {titulo}", Texto = "Olá,\n\nO cartão \"{titulo}\" foi concluído.\n\n{link}" }))),

        new("Checklist completo → próxima etapa",
            "Quando o último item do checklist é marcado, move o cartão para a lista escolhida.",
            "Checklist",
            q => Regra(q, "Checklist completo avança", "Avança o cartão ao terminar as tarefas.", TipoGatilho.ChecklistConcluido, null, null,
                (TipoAcaoAutomacao.MoverCartao, new ParametrosAcao()))),

        new("Cartão bloqueado",
            "Quando o cartão passa a depender de outro não concluído, cria alerta e avisa os desenvolvedores.",
            "Block",
            q => Regra(q, "Aviso de bloqueio", "Deixa bloqueios visíveis.", TipoGatilho.CartaoBloqueado, null, null,
                (TipoAcaoAutomacao.CriarAlerta, new ParametrosAcao { Severidade = SeveridadeAlerta.Aviso, Texto = "Bloqueado por outro cartão" }),
                (TipoAcaoAutomacao.NotificarResponsavel, new ParametrosAcao { Destinatarios = DestinatariosAutomacao.Desenvolvedores, Texto = "\"{titulo}\" ficou bloqueado." }))),

        new("Sprint terminando",
            "Dois dias antes do fim da sprint, avisa os desenvolvedores de cada cartão ainda aberto.",
            "Flag",
            q => Regra(q, "Sprint terminando", "Foco no que falta fechar.", TipoGatilho.SprintEncerrando, new ParametrosGatilho { Dias = 2 }, null,
                (TipoAcaoAutomacao.NotificarResponsavel, new ParametrosAcao { Destinatarios = DestinatariosAutomacao.Desenvolvedores, Texto = "A sprint termina em breve e \"{titulo}\" ainda está em {lista}." }))),

        new("Resumo diário no canal do Teams",
            "Todo dia útil às 09:00, posta no canal os números do quadro e os cartões atrasados. Cole a URL do webhook do canal.",
            "Forum",
            q => Regra(q, "Resumo diário no Teams", "Visão do quadro para o time no canal.", TipoGatilho.Agendado, new ParametrosGatilho { Horario = "09:00" }, null,
                (TipoAcaoAutomacao.PostarResumoNoTeams, new ParametrosAcao()))),

        new("Avisar o canal do Teams",
            "Quando um cartão entra na lista escolhida (ex.: Homologação), posta no canal com o botão para abrir o cartão.",
            "Campaign",
            q => Regra(q, "Aviso no Teams", "Mantém o canal a par das entregas.", TipoGatilho.CartaoEntrouNaLista, null, null,
                (TipoAcaoAutomacao.PostarNoTeams, new ParametrosAcao { Texto = "{titulo} chegou em {lista}. Responsáveis: {desenvolvedores}." }))),

        new("Crítico: prioridade máxima e etiqueta",
            "Cartão criado com a palavra \"urgente\" no título vira prioridade crítica e avisa o desenvolvedor principal.",
            "PriorityHigh",
            q => Regra(q, "Urgências", "Triagem automática de urgências.", TipoGatilho.CartaoCriado, null,
                [new Condicao { Tipo = TipoCondicao.Titulo, Operador = OperadorCondicao.Contem, Texto = "urgente" }],
                (TipoAcaoAutomacao.DefinirPrioridade, new ParametrosAcao { Prioridade = Prioridade.Critica }),
                (TipoAcaoAutomacao.NotificarResponsavel, new ParametrosAcao { Destinatarios = DestinatariosAutomacao.DesenvolvedorPrincipal, Texto = "Novo cartão urgente: \"{titulo}\"." })))
    ];
}
