namespace KanbanDemandas.Web.Configuracao;

public enum TipoParametro { Texto, Inteiro, Decimal, Booleano, Horario, Opcoes, Usuario, Segredo, Quadro, Lista, MultiOpcoes }

public sealed record ParametroDefinicao(
    string Chave, string Grupo, string Nome, string Ajuda, TipoParametro Tipo,
    decimal? Minimo = null, decimal? Maximo = null, IReadOnlyList<(string Valor, string Rotulo)>? Opcoes = null);

/// <summary>Parâmetros que podem ser alterados pela tela de parametrização (o resto fica só no appsettings).</summary>
public static class CatalogoParametros
{
    public static readonly IReadOnlyList<ParametroDefinicao> Todos =
    [
        new("Automacao:AtrasoMinutos", "Automações", "Espera antes de executar (minutos)",
            "Regras disparadas por alterações só executam depois que o cartão fica este tempo sem nova alteração. Uma nova alteração reinicia a espera, e um vai-e-volta dentro dela não dispara nada. Use 0 para executar na hora.",
            TipoParametro.Inteiro, 0, 1440),
        new("Automacao:IntervaloVerificacaoSegundos", "Automações", "Intervalo de verificação (segundos)",
            "De quanto em quanto tempo o sistema verifica gatilhos de tempo e eventos cuja espera terminou.", TipoParametro.Inteiro, 15, 3600),
        new("Automacao:ProfundidadeMaxima", "Automações", "Encadeamento máximo",
            "Quantas regras podem disparar umas às outras em sequência antes de a cadeia ser interrompida (proteção contra loop).", TipoParametro.Inteiro, 1, 10),
        new("Automacao:LimiteEmailsPorRegraPorHora", "Automações", "Limite de e-mails por regra por hora",
            "Proteção contra envio em massa por uma regra mal configurada.", TipoParametro.Inteiro, 1, 5000),
        new("Automacao:LimiteTeamsPorRegraPorHora", "Automações", "Limite de mensagens no Teams por regra por hora",
            "Proteção contra excesso de mensagens no canal. O Teams também limita cerca de 4 mensagens por segundo por webhook.", TipoParametro.Inteiro, 1, 5000),
        new("Automacao:PontosPorDia", "Automações", "Pontos de estimativa por dia",
            "Converte estimativa em dias úteis (prazo em risco e remarcação de conflitos).", TipoParametro.Decimal, 0.1m, 100),
        new("Automacao:UsuarioSistemaId", "Automações", "Usuário da automação",
            "Autor de comentários e responsável pelas execuções de gatilhos de tempo.", TipoParametro.Usuario),
        new("Automacao:HorarioComercialInicio", "Automações", "Início do horário comercial", "Usado por regras marcadas para rodar só no horário comercial.", TipoParametro.Horario),
        new("Automacao:HorarioComercialFim", "Automações", "Fim do horário comercial", "Usado por regras marcadas para rodar só no horário comercial.", TipoParametro.Horario),

        new("Movimentacao:JanelaCorrecaoMinutos", "Movimentação", "Janela de correção do vai-e-volta (minutos)",
            "Voltar o cartão para a coluna de origem dentro deste tempo desfaz o movimento (estorno). Deixe menor ou igual à espera das automações.",
            TipoParametro.Inteiro, 0, 1440),

        new("Cartao:PrefixoCodigo", "Cartões", "Prefixo do código do cartão",
            "O cartão 123 aparece como PREFIXO-123 (ex.: KB-123). É o protocolo no portal e o que se cita em commits e pull requests.", TipoParametro.Texto),
        new("Cartao:MostrarDescricao", "Cartões", "Mostrar descrição no cartão", "Exibe a descrição no quadro, abaixo do título.", TipoParametro.Booleano),
        new("Cartao:LinhasDescricao", "Cartões", "Linhas da descrição no cartão",
            "Quantidade máxima de linhas; o que passar termina com reticências (...).", TipoParametro.Inteiro, 1, 10),
        new("Quadro:CartoesPorColuna", "Cartões", "Cartões exibidos por coluna",
            "Colunas com mais cartões mostram o botão \"Mostrar mais\".", TipoParametro.Inteiro, 5, 500),

        new("Paginacao:ItensPorPagina", "Geral", "Itens por página", "Tamanho de página em tabelas, busca, notificações e históricos.", TipoParametro.Inteiro, 5, 500),
        new("Calendario:ConsiderarFeriados", "Geral", "Considerar feriados nos dias úteis",
            "Feriados cadastrados deixam de contar como dias úteis em prazos, remarcações e indicadores.", TipoParametro.Booleano),
        new("Aplicacao:UrlBase", "Geral", "Endereço do sistema",
            "Usado nos links enviados por e-mail (ex.: https://kanban.suaempresa.com.br). Vazio = endereço do primeiro acesso após iniciar.", TipoParametro.Texto),

        new("Portal:QuadroId", "Portal", "Quadro que recebe as solicitações",
            "Solicitações abertas no portal viram cartões neste quadro.", TipoParametro.Quadro),
        new("Portal:ListaEntradaId", "Portal", "Lista de entrada",
            "Onde os cartões nascem (normalmente o backlog ou uma lista de triagem). Lista com sublistas usa a primeira sublista.", TipoParametro.Lista),
        new("Portal:UrlBase", "Portal", "Endereço do portal",
            "Usado nos e-mails enviados ao solicitante (ex.: https://solicitacoes.suaempresa.com.br).", TipoParametro.Texto),
        new("Portal:StatusInicial", "Portal", "Status inicial",
            "Status mostrado logo após a abertura, até o cartão entrar numa lista com status público.", TipoParametro.Texto),
        new("Portal:TiposSolicitacao", "Portal", "Tipos de solicitação",
            "Opções do campo \"Tipo\" no formulário, separadas por vírgula (ex.: Melhoria, Erro, Dúvida). Vazio = campo oculto.", TipoParametro.Texto),
        new("Portal:CamposVisiveis", "Portal", "O que o solicitante vê",
            "Informações do cartão exibidas no portal. Título, descrição, anexos enviados por ele e o status público sempre aparecem. " +
            "Desmarcar \"Conversa\" remove a conversa do portal e a opção \"Visível ao solicitante\" dos comentários do cartão.",
            TipoParametro.MultiOpcoes, Opcoes: Enum.GetValues<KanbanDemandas.Core.Regras.CamposPortal>()
                .Where(c => c != KanbanDemandas.Core.Regras.CamposPortal.Nenhum)
                .Select(c => (c.ToString(), KanbanDemandas.Core.Regras.ConfiguracaoPortal.Nome(c))).ToList()),
        new("Portal:PermitirAnexos", "Portal", "Solicitante pode anexar arquivos", "Na abertura e depois, na conversa.", TipoParametro.Booleano),
        new("Portal:PermitirComentarios", "Portal", "Solicitante pode enviar mensagens",
            "As mensagens entram como comentários públicos no cartão e avisam os desenvolvedores. Desligado, o solicitante só lê as respostas do time " +
            "(se a Conversa estiver visível).", TipoParametro.Booleano),
        new("Portal:NotificarSolicitante", "Portal", "Avisar o solicitante por e-mail",
            "Quando o status público muda e quando o time responde com um comentário público.", TipoParametro.Booleano),

        new("Integracoes:GitHub:Habilitado", "Integrações", "Receber webhooks do GitHub",
            "Liga o endereço /api/integracoes/github. Guia de configuração: docs/integracoes/github.md.", TipoParametro.Booleano),
        new("Integracoes:GitHub:SegredoWebhook", "Integrações", "Segredo do webhook do GitHub",
            "O mesmo texto informado em \"Secret\" no webhook do GitHub (mínimo 16 caracteres). Chamadas sem essa assinatura são recusadas.", TipoParametro.Segredo),

        new("Anexos:ExtensoesPermitidas", "Anexos", "Extensões permitidas", "Separadas por vírgula (ex.: .pdf,.docx,.png). Vazio = qualquer extensão.", TipoParametro.Texto),
        new("Anexos:TamanhoMaximoMbPorArquivo", "Anexos", "Tamanho máximo por arquivo (MB)", "", TipoParametro.Inteiro, 1, 2048),
        new("Anexos:TamanhoMaximoMbTotal", "Anexos", "Tamanho máximo por cartão (MB)", "", TipoParametro.Inteiro, 1, 10240),
        new("Anexos:EstrategiaNome", "Anexos", "Nome do arquivo armazenado", "Como o arquivo é nomeado no armazenamento.", TipoParametro.Opcoes,
            Opcoes: [("Guid", "Identificador único (recomendado)"), ("Original", "Nome original"), ("CartaoOriginal", "Id do cartão + nome original")]),
        new("Anexos:PoliticaColisao", "Anexos", "Quando o nome já existe", "Aplica-se às estratégias que usam o nome original.", TipoParametro.Opcoes,
            Opcoes: [("Renomear", "Renomear (acrescenta (1), (2)...)"), ("Sobrescrever", "Sobrescrever o arquivo"), ("Rejeitar", "Recusar o envio")]),
        new("Anexos:Comprimir", "Anexos", "Compactar anexos", "Guarda compactados (gzip) os tipos listados abaixo, quando há ganho real.", TipoParametro.Booleano),
        new("Anexos:ExtensoesComprimir", "Anexos", "Extensões a compactar",
            "Formatos que costumam ganhar com compactação. PDF, JPG, PNG, DOCX, XLSX e ZIP já são compactados e quase não ganham.", TipoParametro.Texto),
        new("Anexos:GanhoMinimoPercentual", "Anexos", "Ganho mínimo para compactar (%)",
            "Se a compactação economizar menos que isso, o arquivo é guardado como está.", TipoParametro.Inteiro, 1, 90)
    ];

    public static ParametroDefinicao? Buscar(string chave)
        => Todos.FirstOrDefault(p => p.Chave.Equals(chave, StringComparison.OrdinalIgnoreCase));
}
