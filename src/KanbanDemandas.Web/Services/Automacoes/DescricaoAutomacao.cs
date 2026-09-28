using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Web.Services.Automacoes;

/// <summary>Textos em português que explicam gatilhos, condições e ações de uma regra.</summary>
public static class DescricaoAutomacao
{
    public static bool EhGatilhoDeTempo(TipoGatilho gatilho) => (int)gatilho >= 20;

    /// <summary>Gatilhos que rodam uma vez por quadro (e aplicam ações nos cartões que atendem às condições).</summary>
    public static bool EhGatilhoDeQuadro(TipoGatilho gatilho) => gatilho is TipoGatilho.Agendado or TipoGatilho.SprintIniciada or TipoGatilho.SprintEncerrando;

    public static bool EhAcaoDeQuadro(TipoAcaoAutomacao tipo) => tipo is TipoAcaoAutomacao.EnviarResumo or TipoAcaoAutomacao.PostarResumoNoTeams;

    public static string NomeGatilho(TipoGatilho gatilho) => gatilho switch
    {
        TipoGatilho.CartaoCriado => "Cartão criado",
        TipoGatilho.CartaoEntrouNaLista => "Cartão entrou em uma lista",
        TipoGatilho.CartaoSaiuDaLista => "Cartão saiu de uma lista",
        TipoGatilho.CampoAlterado => "Campo do cartão alterado",
        TipoGatilho.CartaoAtribuido => "Desenvolvedor atribuído",
        TipoGatilho.ComentarioAdicionado => "Comentário adicionado",
        TipoGatilho.ChecklistConcluido => "Checklist concluído",
        TipoGatilho.CartaoBloqueado => "Cartão ficou bloqueado",
        TipoGatilho.ConflitoDatas => "Conflito de datas detectado",
        TipoGatilho.CommitVinculado => "Commit citou o cartão (GitHub)",
        TipoGatilho.PullRequestAberto => "Pull request aberto (GitHub)",
        TipoGatilho.PullRequestMergeado => "Pull request mergeado (GitHub)",
        TipoGatilho.PrazoProximo => "Prazo se aproximando",
        TipoGatilho.PrazoVencido => "Prazo vencido",
        TipoGatilho.ParadoNaLista => "Cartão parado na lista",
        TipoGatilho.Agendado => "Agendamento (dia e hora)",
        TipoGatilho.SprintIniciada => "Sprint iniciada",
        TipoGatilho.SprintEncerrando => "Sprint terminando",
        _ => gatilho.ToString()
    };

    public static string AjudaGatilho(TipoGatilho gatilho) => gatilho switch
    {
        TipoGatilho.CartaoCriado => "Roda quando um cartão é criado no quadro.",
        TipoGatilho.CartaoEntrouNaLista => "Roda quando um cartão entra na lista escolhida (ou em qualquer lista). Lista com sublistas vale para todas elas.",
        TipoGatilho.CartaoSaiuDaLista => "Roda quando um cartão sai da lista escolhida.",
        TipoGatilho.CampoAlterado => "Roda quando o campo escolhido muda (prazo, prioridade, responsáveis, etiquetas...).",
        TipoGatilho.CartaoAtribuido => "Roda quando alguém é atribuído como desenvolvedor do cartão.",
        TipoGatilho.ComentarioAdicionado => "Roda a cada novo comentário no cartão.",
        TipoGatilho.ChecklistConcluido => "Roda quando o último item do checklist é concluído.",
        TipoGatilho.CartaoBloqueado => "Roda quando o cartão passa a depender de outro ainda não concluído.",
        TipoGatilho.ConflitoDatas => "Roda quando datas ou responsáveis mudam e o cartão passa a conflitar com outro do mesmo desenvolvedor.",
        TipoGatilho.CommitVinculado => "Roda quando um commit enviado ao GitHub cita o código do cartão (ex.: KB-123) na mensagem ou na branch.",
        TipoGatilho.PullRequestAberto => "Roda quando um pull request que cita o código do cartão é aberto ou reaberto.",
        TipoGatilho.PullRequestMergeado => "Roda quando um pull request que cita o código do cartão é mergeado (ex.: mover para Homologação).",
        TipoGatilho.PrazoProximo => "Verificado periodicamente: roda uma vez quando faltam N dias (ou menos) para o prazo.",
        TipoGatilho.PrazoVencido => "Verificado periodicamente: roda quando o prazo passou; pode repetir a cada N dias.",
        TipoGatilho.ParadoNaLista => "Verificado periodicamente: roda uma vez quando o cartão completa N dias na mesma lista.",
        TipoGatilho.Agendado => "Roda nos dias e horário escolhidos, aplicando as ações a todos os cartões que atendem às condições.",
        TipoGatilho.SprintIniciada => "Roda uma vez quando uma sprint do quadro começa, sobre os cartões da sprint.",
        TipoGatilho.SprintEncerrando => "Roda uma vez quando faltam N dias para o fim da sprint, sobre os cartões da sprint.",
        _ => ""
    };

    public static string NomeCampo(CampoMonitorado campo) => campo switch
    {
        CampoMonitorado.Qualquer => "qualquer campo",
        CampoMonitorado.DataInicio => "data de início",
        CampoMonitorado.Desenvolvedores => "desenvolvedores",
        CampoMonitorado.CampoPersonalizado => "campo personalizado",
        _ => campo.ToString().ToLowerInvariant()
    };

    public static string NomeAcao(TipoAcaoAutomacao tipo) => tipo switch
    {
        TipoAcaoAutomacao.MoverCartao => "Mover cartão",
        TipoAcaoAutomacao.NotificarResponsavel => "Enviar notificação",
        TipoAcaoAutomacao.PreencherCampo => "Preencher campo",
        TipoAcaoAutomacao.AtribuirDesenvolvedor => "Atribuir desenvolvedor",
        TipoAcaoAutomacao.RemoverDesenvolvedores => "Remover desenvolvedores",
        TipoAcaoAutomacao.AdicionarEtiqueta => "Adicionar etiqueta",
        TipoAcaoAutomacao.RemoverEtiqueta => "Remover etiqueta",
        TipoAcaoAutomacao.DefinirPrioridade => "Definir prioridade",
        TipoAcaoAutomacao.DefinirPrazo => "Definir prazo ou início",
        TipoAcaoAutomacao.AdicionarChecklist => "Adicionar itens ao checklist",
        TipoAcaoAutomacao.CriarCartaoFilho => "Criar cartão filho",
        TipoAcaoAutomacao.Comentar => "Comentar no cartão",
        TipoAcaoAutomacao.EnviarEmail => "Enviar e-mail",
        TipoAcaoAutomacao.CriarAlerta => "Criar alerta no cartão",
        TipoAcaoAutomacao.ResolverConflitoDatas => "Resolver conflito de datas",
        TipoAcaoAutomacao.EnviarResumo => "Enviar resumo por e-mail",
        TipoAcaoAutomacao.ResolverAlertas => "Resolver alertas",
        TipoAcaoAutomacao.PostarNoTeams => "Postar no canal do Teams",
        TipoAcaoAutomacao.PostarResumoNoTeams => "Postar resumo do quadro no Teams",
        _ => tipo.ToString()
    };

    public static string NomeCondicao(TipoCondicao tipo) => tipo switch
    {
        TipoCondicao.CampoPersonalizado => "Campo personalizado",
        TipoCondicao.TemPrazo => "Tem prazo",
        TipoCondicao.DiasNaLista => "Dias na lista atual",
        TipoCondicao.TemConflito => "Tem conflito de datas",
        TipoCondicao.PrazoEmRisco => "Prazo em risco (estimativa não cabe)",
        TipoCondicao.ChecklistCompleto => "Checklist completo",
        TipoCondicao.DiasParaPrazo => "Dias até o prazo",
        TipoCondicao.Titulo => "Título",
        _ => tipo.ToString()
    };

    public static string NomeOperador(OperadorCondicao operador) => operador switch
    {
        OperadorCondicao.EstaEm => "é um de",
        OperadorCondicao.NaoEstaEm => "não é nenhum de",
        OperadorCondicao.Igual => "é igual a",
        OperadorCondicao.Diferente => "é diferente de",
        OperadorCondicao.Contem => "contém",
        OperadorCondicao.NaoContem => "não contém",
        OperadorCondicao.Vazio => "está vazio",
        OperadorCondicao.NaoVazio => "está preenchido",
        OperadorCondicao.MaiorOuIgual => "é pelo menos",
        OperadorCondicao.MenorOuIgual => "é no máximo",
        OperadorCondicao.Sim => "sim",
        OperadorCondicao.Nao => "não",
        _ => operador.ToString()
    };

    public static IReadOnlyList<OperadorCondicao> OperadoresPara(TipoCondicao tipo) => tipo switch
    {
        TipoCondicao.Lista => [OperadorCondicao.EstaEm, OperadorCondicao.NaoEstaEm],
        TipoCondicao.Prioridade => [OperadorCondicao.EstaEm, OperadorCondicao.NaoEstaEm, OperadorCondicao.MaiorOuIgual, OperadorCondicao.MenorOuIgual, OperadorCondicao.Vazio, OperadorCondicao.NaoVazio],
        TipoCondicao.Etiqueta or TipoCondicao.Sistema or TipoCondicao.Desenvolvedor or TipoCondicao.Solicitante
            => [OperadorCondicao.EstaEm, OperadorCondicao.NaoEstaEm, OperadorCondicao.Vazio, OperadorCondicao.NaoVazio],
        TipoCondicao.CampoPersonalizado => [OperadorCondicao.NaoVazio, OperadorCondicao.Vazio, OperadorCondicao.Igual, OperadorCondicao.Diferente, OperadorCondicao.Contem, OperadorCondicao.NaoContem, OperadorCondicao.MaiorOuIgual, OperadorCondicao.MenorOuIgual, OperadorCondicao.Sim, OperadorCondicao.Nao],
        TipoCondicao.DiasNaLista or TipoCondicao.DiasParaPrazo => [OperadorCondicao.MaiorOuIgual, OperadorCondicao.MenorOuIgual, OperadorCondicao.Igual],
        TipoCondicao.Estimativa => [OperadorCondicao.MaiorOuIgual, OperadorCondicao.MenorOuIgual, OperadorCondicao.Vazio, OperadorCondicao.NaoVazio],
        TipoCondicao.Titulo => [OperadorCondicao.Contem, OperadorCondicao.NaoContem],
        _ => [OperadorCondicao.Sim, OperadorCondicao.Nao]
    };

    public static string Gatilho(RegraAutomacao regra, FotoQuadro foto)
    {
        var p = JsonAutomacao.Ler<ParametrosGatilho>(regra.ParametrosGatilhoJson);
        var lista = regra.ListaId.HasValue ? $"\"{foto.NomeLista(regra.ListaId.Value)}\"" : "qualquer lista";
        return regra.Gatilho switch
        {
            TipoGatilho.CartaoEntrouNaLista => $"um cartão entrar em {lista}",
            TipoGatilho.CartaoSaiuDaLista => $"um cartão sair de {lista}",
            TipoGatilho.CampoAlterado => $"o campo {NomeCampo(p.Campo)} de um cartão for alterado",
            TipoGatilho.PrazoProximo => $"faltarem {p.Dias} dia(s) ou menos para o prazo",
            TipoGatilho.PrazoVencido => p.Dias > 0 ? $"o prazo vencer (repetindo a cada {p.Dias} dia(s))" : "o prazo vencer",
            TipoGatilho.ParadoNaLista => $"um cartão ficar {p.Dias} dia(s) parado em {lista}",
            TipoGatilho.Agendado => $"for {p.Horario} {DescreverDias(p.DiasSemana)}",
            TipoGatilho.SprintEncerrando => $"faltarem {p.Dias} dia(s) para o fim de uma sprint",
            _ => NomeGatilho(regra.Gatilho).ToLowerInvariant()
        };
    }

    private static string DescreverDias(List<DayOfWeek> dias)
    {
        if (dias.Count == 0 || dias.Count == 7) return "todos os dias";
        var uteis = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        if (dias.Count == 5 && uteis.All(dias.Contains)) return "em dias úteis";
        var nomes = new System.Globalization.CultureInfo("pt-BR").DateTimeFormat;
        return "às " + string.Join(", ", dias.OrderBy(d => ((int)d + 6) % 7).Select(d => nomes.GetDayName(d)));
    }

    public static string Condicao(Condicao c, FotoQuadro foto)
    {
        var nome = NomeCondicao(c.Tipo).ToLowerInvariant();
        if (c.Operador is OperadorCondicao.Sim or OperadorCondicao.Nao && c.Tipo != TipoCondicao.CampoPersonalizado)
            return c.Operador == OperadorCondicao.Sim ? nome : $"não {nome}";
        var valor = c.Tipo switch
        {
            TipoCondicao.Lista => Nomes(c.Ids.Select(foto.NomeLista)),
            TipoCondicao.Prioridade => Nomes(c.Ids.Select(id => ((Prioridade)id).ToString())),
            TipoCondicao.Etiqueta => Nomes(c.Ids.Select(id => foto.Etiquetas.FirstOrDefault(e => e.Id == id)?.Nome)),
            TipoCondicao.Sistema => Nomes(c.Ids.Select(id => foto.Sistemas.FirstOrDefault(s => s.Id == id)?.Nome)),
            TipoCondicao.Desenvolvedor or TipoCondicao.Solicitante => Nomes(c.Ids.Select(id => foto.NomeUsuario(id))),
            TipoCondicao.DiasNaLista or TipoCondicao.DiasParaPrazo or TipoCondicao.Estimativa => c.Numero?.ToString("0.##") ?? "?",
            _ => c.Operador is OperadorCondicao.MaiorOuIgual or OperadorCondicao.MenorOuIgual && c.Numero.HasValue ? c.Numero.Value.ToString("0.##") : $"\"{c.Texto}\""
        };
        if (c.Tipo == TipoCondicao.CampoPersonalizado) nome = $"campo \"{c.CampoNome}\"";
        return c.Operador is OperadorCondicao.Vazio or OperadorCondicao.NaoVazio or OperadorCondicao.Sim or OperadorCondicao.Nao
            ? $"{nome} {NomeOperador(c.Operador)}"
            : $"{nome} {NomeOperador(c.Operador)} {valor}";
    }

    private static string Nomes(IEnumerable<string?> nomes)
    {
        var lista = nomes.OfType<string>().ToList();
        return lista.Count == 0 ? "(nada selecionado)" : string.Join(" ou ", lista.Select(n => $"\"{n}\""));
    }

    public static string Destinatarios(ParametrosAcao p, FotoQuadro foto)
    {
        var partes = new List<string>();
        if (p.Destinatarios.HasFlag(DestinatariosAutomacao.Desenvolvedores)) partes.Add("desenvolvedores");
        if (p.Destinatarios.HasFlag(DestinatariosAutomacao.DesenvolvedorPrincipal)) partes.Add("desenvolvedor principal");
        if (p.Destinatarios.HasFlag(DestinatariosAutomacao.Solicitante)) partes.Add("solicitante");
        if (p.Destinatarios.HasFlag(DestinatariosAutomacao.Criador)) partes.Add("criador do cartão");
        if (p.Destinatarios.HasFlag(DestinatariosAutomacao.UsuariosEspecificos))
            partes.AddRange(p.DestinatarioIds.Select(id => foto.NomeUsuario(id)).OfType<string>());
        if (!string.IsNullOrWhiteSpace(p.EmailsFixos)) partes.Add(p.EmailsFixos);
        return partes.Count == 0 ? "ninguém" : string.Join(", ", partes);
    }

    public static string Acao(TipoAcaoAutomacao tipo, ParametrosAcao p, FotoQuadro foto) => tipo switch
    {
        TipoAcaoAutomacao.MoverCartao => $"mover para \"{(p.ListaId.HasValue ? foto.NomeLista(p.ListaId.Value) : "?")}\"{(p.NoTopo ? " (no topo)" : "")}",
        TipoAcaoAutomacao.NotificarResponsavel => $"notificar {Destinatarios(p, foto)}",
        TipoAcaoAutomacao.PreencherCampo => $"preencher \"{p.CampoNome}\" com \"{p.Valor}\"{(p.SomenteSeVazio ? " (se vazio)" : "")}",
        TipoAcaoAutomacao.AtribuirDesenvolvedor => p.ModoAtribuicao switch
        {
            ModoAtribuicao.UsuarioEspecifico => $"atribuir a {foto.NomeUsuario(p.UsuarioId) ?? "?"}",
            ModoAtribuicao.MenorCarga => "atribuir ao desenvolvedor com menor carga",
            ModoAtribuicao.Revezamento => "atribuir em revezamento",
            ModoAtribuicao.Solicitante => "atribuir ao solicitante",
            _ => "atribuir a quem disparou"
        } + (p.SubstituirAtuais ? " (substituindo os atuais)" : ""),
        TipoAcaoAutomacao.RemoverDesenvolvedores => p.UsuarioIds.Count == 0 ? "remover todos os desenvolvedores" : $"remover {string.Join(", ", p.UsuarioIds.Select(id => foto.NomeUsuario(id)))}",
        TipoAcaoAutomacao.AdicionarEtiqueta => $"adicionar etiqueta \"{foto.Etiquetas.FirstOrDefault(e => e.Id == p.EtiquetaId)?.Nome}\"",
        TipoAcaoAutomacao.RemoverEtiqueta => $"remover etiqueta \"{foto.Etiquetas.FirstOrDefault(e => e.Id == p.EtiquetaId)?.Nome}\"",
        TipoAcaoAutomacao.DefinirPrioridade => p.AumentarUmNivel ? "aumentar a prioridade em um nível" : $"definir prioridade {p.Prioridade}",
        TipoAcaoAutomacao.DefinirPrazo => p.BaseData == BaseDataAutomacao.Limpar
            ? $"limpar {(p.AplicarEmDataInicio ? "a data de início" : "o prazo")}"
            : $"definir {(p.AplicarEmDataInicio ? "início" : "prazo")} para {(p.BaseData == BaseDataAutomacao.Hoje ? "hoje" : "o prazo atual")} + {p.Dias} dia(s){(p.DiasUteis ? " úteis" : "")}",
        TipoAcaoAutomacao.AdicionarChecklist => $"adicionar {p.Itens.Count} item(ns) ao checklist",
        TipoAcaoAutomacao.CriarCartaoFilho => $"criar cartão filho \"{p.Titulo}\"",
        TipoAcaoAutomacao.Comentar => $"comentar \"{Resumir(p.Texto)}\"",
        TipoAcaoAutomacao.EnviarEmail => $"enviar e-mail para {Destinatarios(p, foto)}",
        TipoAcaoAutomacao.CriarAlerta => $"criar alerta ({p.Severidade}) \"{Resumir(p.Texto)}\"",
        TipoAcaoAutomacao.ResolverConflitoDatas => $"{(p.Modo == ModoAplicacao.Sugerir ? "sugerir" : "aplicar")} solução de conflito: " + p.Resolucao switch
        {
            ResolucaoConflito.EmpurrarDatas => "empurrar as datas",
            ResolucaoConflito.MoverParaLista => $"mover para \"{(p.ListaId.HasValue ? foto.NomeLista(p.ListaId.Value) : "?")}\"",
            _ => "apenas avisar"
        } + (p.Resolucao == ResolucaoConflito.EmpurrarDatas && p.ListaId.HasValue ? $" e mover para \"{foto.NomeLista(p.ListaId.Value)}\"" : "")
          + $" do cartão {NomeEstrategia(p.Estrategia)}",
        TipoAcaoAutomacao.EnviarResumo => $"enviar resumo por e-mail para {Destinatarios(p, foto)}",
        TipoAcaoAutomacao.ResolverAlertas => p.TodosAlertasDoCartao ? "resolver todos os alertas do cartão" : "resolver os alertas criados por esta regra",
        TipoAcaoAutomacao.PostarNoTeams => $"postar no Teams ({HostWebhook(p.UrlWebhook)})",
        TipoAcaoAutomacao.PostarResumoNoTeams => $"postar resumo do quadro no Teams ({HostWebhook(p.UrlWebhook)})",
        _ => NomeAcao(tipo).ToLowerInvariant()
    };

    // A URL do webhook funciona como senha; nas descrições aparece só o domínio.
    public static string HostWebhook(string? url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : "URL não definida";

    public static string NomeEstrategia(EstrategiaConflito estrategia) => estrategia switch
    {
        EstrategiaConflito.MenorPrioridade => "de menor prioridade",
        EstrategiaConflito.MaisAntigo => "mais antigo",
        EstrategiaConflito.NaoIniciado => "que ainda não começou",
        _ => "mais recente"
    };

    public static string Regra(RegraAutomacao regra, FotoQuadro foto)
    {
        var condicoes = JsonAutomacao.Ler<List<Condicao>>(regra.CondicoesJson);
        var texto = $"Quando {Gatilho(regra, foto)}";
        if (condicoes.Count > 0)
            texto += ", se " + string.Join(regra.ExigirTodasCondicoes ? " e " : " ou ", condicoes.Select(c => Condicao(c, foto)));
        var acoes = regra.Acoes.Where(a => !a.Excluido).OrderBy(a => a.Ordem)
            .Select(a => Acao(a.Tipo, JsonAutomacao.Ler<ParametrosAcao>(a.ParametrosJson), foto)).ToList();
        return texto + ", então " + (acoes.Count == 0 ? "(nenhuma ação)" : string.Join("; ", acoes)) + ".";
    }

    private static string Resumir(string? texto) => string.IsNullOrEmpty(texto) ? "" : texto.Length <= 60 ? texto : texto[..57] + "...";
}
