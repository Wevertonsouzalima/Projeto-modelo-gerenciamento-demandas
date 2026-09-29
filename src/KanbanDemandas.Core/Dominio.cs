using System.Text.RegularExpressions;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Core.Entities
{
    #region Entities/AcaoAutomacao.cs

    /// <summary>Ação de uma regra de automação, executada na ordem definida. Parâmetros em JSON conforme o tipo.</summary>
    public class AcaoAutomacao : EntidadeBase
    {
        public int RegraAutomacaoId { get; set; }
        public RegraAutomacao RegraAutomacao { get; set; } = null!;

        public TipoAcaoAutomacao Tipo { get; set; }
        public int Ordem { get; set; }
        public string? ParametrosJson { get; set; }
    }

    #endregion

    #region Entities/AlertaCartao.cs

    /// <summary>Alerta visível no cartão, criado por automação, até alguém (ou outra regra) resolvê-lo.</summary>
    public class AlertaCartao
    {
        public int Id { get; set; }

        public int CartaoId { get; set; }
        public Cartao Cartao { get; set; } = null!;

        public int? RegraAutomacaoId { get; set; }
        public RegraAutomacao? RegraAutomacao { get; set; }

        public string Mensagem { get; set; } = string.Empty;
        public SeveridadeAlerta Severidade { get; set; } = SeveridadeAlerta.Aviso;

        public bool Resolvido { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? ResolvidoEm { get; set; }
        public int? ResolvidoPorId { get; set; }
    }

    #endregion

    #region Entities/Anexo.cs

    /// <summary>
    /// Arquivo anexado a um cartão.
    /// </summary>
    public class Anexo : EntidadeBase
    {
        public int CartaoId { get; set; }
        public Cartao Cartao { get; set; } = null!;

        public string NomeOriginal { get; set; } = string.Empty;

        /// <summary>Nome do arquivo armazenado no disco/blob (pode diferir do original para evitar colisões).</summary>
        public string NomeArmazenado { get; set; } = string.Empty;

        /// <summary>Caminho relativo ou URL do arquivo no storage.</summary>
        public string Caminho { get; set; } = string.Empty;

        /// <summary>Tipo MIME do arquivo.</summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>Tamanho em bytes.</summary>
        public long TamanhoBytes { get; set; }

        /// <summary>Arquivo guardado compactado (gzip); o download devolve o conteúdo original.</summary>
        public bool Comprimido { get; set; }

        /// <summary>Tamanho ocupado no armazenamento (menor que <see cref="TamanhoBytes"/> quando comprimido).</summary>
        public long TamanhoArmazenadoBytes { get; set; }

        public int EnviadoPorId { get; set; }
        public Usuario EnviadoPor { get; set; } = null!;
    }

    #endregion

    #region Entities/Cartao.cs

    /// <summary>
    /// Cartão Kanban. Entidade central do sistema.
    /// </summary>
    public class Cartao : EntidadeBase
    {
        public string Titulo { get; set; } = string.Empty;
        public string? Descricao { get; set; }

        public int ListaId { get; set; }

        /// <summary>Cartão aberto pelo solicitante no portal de solicitações.</summary>
        public bool OrigemPortal { get; set; }

        /// <summary>Tipo escolhido no portal (ex.: Melhoria, Erro, Dúvida).</summary>
        public string? TipoSolicitacao { get; set; }
        public Lista Lista { get; set; } = null!;

        public int Ordem { get; set; }

        // Datas e prazo
        public DateTime? DataInicio { get; set; }
        public DateTime? Prazo { get; set; }

        // Estimativa em horas/pontos (unidade definida pelo time)
        public decimal? Estimativa { get; set; }

        public Prioridade? Prioridade { get; set; }

        // Papéis fixos
        public int? SistemaId { get; set; }
        public Sistema? Sistema { get; set; }

        public int? SolicitanteId { get; set; }
        public Usuario? Solicitante { get; set; }

        // Hierarquia pai/filho
        public int? CartaoPaiId { get; set; }
        public Cartao? CartaoPai { get; set; }

        // Navegação
        public ICollection<Cartao> CartosFilhos { get; set; } = [];
        public ICollection<CartaoDesenvolvedor> Desenvolvedores { get; set; } = [];
        public ICollection<CartaoEtiqueta> Etiquetas { get; set; } = [];
        public ICollection<ItemTarefa> ItensTarefa { get; set; } = [];
        public ICollection<ValorCampoCartao> ValoresCampo { get; set; } = [];
        public ICollection<Comentario> Comentarios { get; set; } = [];
        public ICollection<Reuniao> Reunioes { get; set; } = [];
        public ICollection<Anexo> Anexos { get; set; } = [];
        public ICollection<HistoricoAtividade> Historico { get; set; } = [];
        public ICollection<RelacaoCartao> RelacoesOrigem { get; set; } = [];
        public ICollection<RelacaoCartao> RelacoesDestino { get; set; } = [];
        public ICollection<SprintCartao> Sprints { get; set; } = [];
        public ICollection<EmailCartao> Emails { get; set; } = [];
        public ICollection<AlertaCartao> Alertas { get; set; } = [];
        public ICollection<VinculoGit> VinculosGit { get; set; } = [];
    }

    #endregion

    #region Entities/CartaoDesenvolvedor.cs

    /// <summary>
    /// Tabela de junção N:N entre Cartão e Desenvolvedor (Usuário).
    /// Um desenvolvedor pode ser marcado como principal para notificações/e-mail padrão.
    /// </summary>
    public class CartaoDesenvolvedor
    {
        public int CartaoId { get; set; }
        public Cartao Cartao { get; set; } = null!;

        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;

        /// <summary>
        /// Desenvolvedor principal do cartão (destinatário padrão de notificações).
        /// </summary>
        public bool Principal { get; set; } = false;
    }

    #endregion

    #region Entities/CartaoEtiqueta.cs

    /// <summary>
    /// Tabela de junção N:N entre Cartão e Etiqueta.
    /// </summary>
    public class CartaoEtiqueta
    {
        public int CartaoId { get; set; }
        public Cartao Cartao { get; set; } = null!;

        public int EtiquetaId { get; set; }
        public Etiqueta Etiqueta { get; set; } = null!;
    }

    #endregion

    #region Entities/Comentario.cs

    /// <summary>
    /// Comentário na linha do tempo de um cartão ou item de tarefa.
    /// Suporta menção @usuario no texto.
    /// </summary>
    public class Comentario : EntidadeBase
    {
        public string Texto { get; set; } = string.Empty;

        public int AutorId { get; set; }
        public Usuario Autor { get; set; } = null!;

        public DateTime DataHora { get; set; }

        /// <summary>Marcado como verdadeiro quando o texto foi editado após criação.</summary>
        public bool Editado { get; set; } = false;

        /// <summary>Visível ao solicitante no portal. Comentários internos do time ficam ocultos.</summary>
        public bool Publico { get; set; }

        // Pertence a um cartão OU a um item de tarefa (um dos dois null)
        public int? CartaoId { get; set; }
        public Cartao? Cartao { get; set; }

        public int? ItemTarefaId { get; set; }
        public ItemTarefa? ItemTarefa { get; set; }
    }

    #endregion

    #region Entities/DefinicaoCampo.cs

    /// <summary>
    /// Define um campo configurável para uma lista/sublista.
    /// Criado pelo usuário em tempo de uso — não fixo no código.
    /// </summary>
    public class DefinicaoCampo : EntidadeBase
    {
        public string Nome { get; set; } = string.Empty;
        public TipoCampo Tipo { get; set; }

        public int ListaId { get; set; }
        public Lista Lista { get; set; } = null!;

        /// <summary>
        /// Se verdadeiro, gera badge de alerta no cartão quando não preenchido.
        /// Não bloqueia a movimentação do cartão.
        /// </summary>
        public bool Obrigatorio { get; set; } = false;

        /// <summary>
        /// Quando verdadeiro: a cada nova entrada do cartão na lista, pede o valor novamente
        /// sem sobrescrever o anterior — cria novo ValorCampoCartao com NumeroEntradaNaLista++.
        /// Quando falso: valor único por cartão, persiste entre entradas.
        /// </summary>
        public bool PedirNovamenteACadaEntrada { get; set; } = false;

        /// <summary>
        /// Opções disponíveis para campos do tipo Selecao (separadas por |).
        /// </summary>
        public string? OpcoesSelecao { get; set; }

        public int Ordem { get; set; }

        /// <summary>Manual ou preenchido automaticamente quando o cartão entra na lista.</summary>
        public PreenchimentoAutomatico Preenchimento { get; set; } = PreenchimentoAutomatico.Manual;

        /// <summary>Para campos automáticos: o que fazer quando o cartão volta a entrar na lista.</summary>
        public RegraReentrada RegraReentrada { get; set; } = RegraReentrada.ManterPrimeiro;

        /// <summary>Campo de data cujo valor também passa a ser a Data de início do cartão (conflitos, filtros e indicadores).</summary>
        public bool DefineDataInicioCartao { get; set; } = false;

        // Navegação
        public ICollection<ValorCampoCartao> Valores { get; set; } = [];

        /// <summary>Retorna as opções de seleção como lista.</summary>
        public List<string> ObterOpcoesSelecao()
            => string.IsNullOrWhiteSpace(OpcoesSelecao)
                ? []
                : [.. OpcoesSelecao.Split('|', StringSplitOptions.RemoveEmptyEntries)];
    }

    #endregion

    #region Entities/EmailCartao.cs

    /// <summary>
    /// Registro de e-mail enviado a partir de um cartão.
    /// Aparece na linha do tempo junto com comentários e reuniões.
    /// </summary>
    public class EmailCartao : EntidadeBase
    {
        public int CartaoId { get; set; }
        public Cartao Cartao { get; set; } = null!;

        public string Para { get; set; } = string.Empty;
        public string? Cc { get; set; }
        public string Assunto { get; set; } = string.Empty;

        /// <summary>Corpo do e-mail em HTML sanitizado.</summary>
        public string CorpoHtml { get; set; } = string.Empty;

        public DateTime EnviadoEm { get; set; }

        public int EnviadoPorId { get; set; }
        public Usuario EnviadoPor { get; set; } = null!;

        public bool Enviado { get; set; } = false;
        public string? ErroEnvio { get; set; }
    }

    #endregion

    #region Entities/EntidadeBase.cs

    /// <summary>
    /// Classe base com auditoria automática e soft-delete para todas as entidades principais.
    /// </summary>
    public abstract class EntidadeBase
    {
        public int Id { get; set; }

        // Auditoria de criação
        public DateTime CriadoEm { get; set; }
        public int CriadoPorId { get; set; }

        // Auditoria de alteração
        public DateTime? AlteradoEm { get; set; }
        public int? AlteradoPorId { get; set; }

        // Soft-delete
        public bool Excluido { get; set; }
        public DateTime? ExcluidoEm { get; set; }
        public int? ExcluidoPorId { get; set; }
    }

    #endregion

    #region Entities/Etiqueta.cs

    /// <summary>
    /// Etiqueta colorida aplicável a cartões (N:N).
    /// </summary>
    public class Etiqueta : EntidadeBase
    {
        public string Nome { get; set; } = string.Empty;

        /// <summary>Cor em formato hex, ex.: #FF5733</summary>
        public string Cor { get; set; } = "#607D8B";

        public int QuadroId { get; set; }
        public Quadro Quadro { get; set; } = null!;

        // Navegação N:N
        public ICollection<CartaoEtiqueta> CartaoEtiquetas { get; set; } = [];
    }

    #endregion

    #region Entities/EventoAutomacaoPendente.cs

    /// <summary>
    /// Evento de automação aguardando o tempo de espera. Cada nova alteração no mesmo cartão adia a execução,
    /// então as regras só rodam depois que o cartão "assenta" — um vai-e-volta dentro da espera não dispara nada.
    /// </summary>
    public class EventoAutomacaoPendente
    {
        public int Id { get; set; }
        public int CartaoId { get; set; }
        public TipoGatilho Gatilho { get; set; }
        public int? ListaId { get; set; }
        public CampoMonitorado Campo { get; set; }
        public int? UsuarioAlvoId { get; set; }
        public int UsuarioId { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime ExecutarEm { get; set; }
    }

    #endregion

    #region Entities/ExecucaoAutomacao.cs

    /// <summary>Registro de cada disparo de regra: o que foi feito, se deu erro e como desfazer.</summary>
    public class ExecucaoAutomacao
    {
        public int Id { get; set; }

        public int RegraAutomacaoId { get; set; }
        public RegraAutomacao RegraAutomacao { get; set; } = null!;

        public int QuadroId { get; set; }

        /// <summary>Cartão alvo; null em execuções de quadro (ex.: resumo por e-mail).</summary>
        public int? CartaoId { get; set; }
        public Cartao? Cartao { get; set; }

        public TipoGatilho Gatilho { get; set; }

        /// <summary>Identifica o disparo de gatilhos de tempo para não repetir (ex.: "prazo:2026-10-01").</summary>
        public string? ChaveDisparo { get; set; }

        public StatusExecucaoAutomacao Status { get; set; }
        public string Resumo { get; set; } = string.Empty;
        public string? Erro { get; set; }

        /// <summary>Operações inversas das alterações feitas, usadas para desfazer a execução.</summary>
        public string? DesfazerJson { get; set; }

        public int EmailsEnviados { get; set; }
        public int MensagensTeams { get; set; }
        public int UsuarioId { get; set; }
        public DateTime OcorridoEm { get; set; }
        public DateTime? DesfeitaEm { get; set; }
        public int? DesfeitaPorId { get; set; }
    }

    #endregion

    #region Entities/Feriado.cs

    /// <summary>Dia não útil considerado nos cálculos de dias úteis (prazos, remarcações e indicadores).</summary>
    public class Feriado : EntidadeBase
    {
        public DateTime Data { get; set; }
        public string Nome { get; set; } = string.Empty;
        public TipoFeriado Tipo { get; set; } = TipoFeriado.Nacional;

        /// <summary>Repete todo ano no mesmo dia e mês (ex.: aniversário da cidade); o ano de <see cref="Data"/> é ignorado.</summary>
        public bool RecorrenteAnual { get; set; }
    }

    #endregion

    #region Entities/HistoricoAtividade.cs

    /// <summary>
    /// Trilha de auditoria de um cartão. Base para cálculo de lead time, badges,
    /// número de entrada em lista e indicadores do dashboard.
    /// </summary>
    public class HistoricoAtividade
    {
        public int Id { get; set; }

        public int CartaoId { get; set; }
        public Cartao Cartao { get; set; } = null!;

        public TipoHistoricoAtividade Tipo { get; set; }

        /// <summary>Lista de origem (para CartaoMovido/CartaoSaiu).</summary>
        public int? ListaOrigemId { get; set; }
        public Lista? ListaOrigem { get; set; }

        /// <summary>Lista de destino (para CartaoMovido/CartaoEntrou).</summary>
        public int? ListaDestinoId { get; set; }
        public Lista? ListaDestino { get; set; }

        /// <summary>Descrição legível da atividade.</summary>
        public string Descricao { get; set; } = string.Empty;

        /// <summary>Valor anterior (para CampoAlterado).</summary>
        public string? ValorAnterior { get; set; }

        /// <summary>Valor novo (para CampoAlterado).</summary>
        public string? ValorNovo { get; set; }

        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;

        public DateTime OcorridoEm { get; set; }

        /// <summary>
        /// Movimento desfeito logo em seguida (cartão voltou para a lista de origem dentro da janela de correção).
        /// Fica gravado para auditoria, mas é ignorado por métricas, linha do tempo e contagem de entradas.
        /// </summary>
        public bool Estornado { get; set; }

        /// <summary>Indica se este movimento alterou a Data de início do cartão (para desfazer no estorno).</summary>
        public bool AlterouDataInicio { get; set; }

        public DateTime? DataInicioAnterior { get; set; }
    }

    #endregion

    #region Entities/ItemTarefa.cs

    /// <summary>
    /// Item de tarefa (checklist evoluído) de um cartão.
    /// Dois modos: simples (título + checkbox) ou detalhado (+ descrição, comentários, reuniões).
    /// Pode ser promovido a cartão filho via CartaoPromovidoId.
    /// </summary>
    public class ItemTarefa : EntidadeBase
    {
        public string Titulo { get; set; } = string.Empty;
        public bool Concluido { get; set; } = false;

        public int CartaoId { get; set; }
        public Cartao Cartao { get; set; } = null!;

        public int Ordem { get; set; }

        // Modo detalhado
        public string? Descricao { get; set; }

        /// <summary>
        /// Desenvolvedor responsável por este item — restrito aos devs já vinculados ao cartão pai.
        /// </summary>
        public int? DesenvolvedorId { get; set; }
        public Usuario? Desenvolvedor { get; set; }

        /// <summary>
        /// Quando promovido a cartão filho, aponta para o novo cartão criado.
        /// O progresso do item passa a refletir o status do cartão filho.
        /// </summary>
        public int? CartaoPromovidoId { get; set; }
        public Cartao? CartaoPromovido { get; set; }

        // Modo detalhado: comentários e reuniões próprios
        public ICollection<Comentario> Comentarios { get; set; } = [];
        public ICollection<Reuniao> Reunioes { get; set; } = [];
    }

    #endregion

    #region Entities/Lista.cs

    /// <summary>
    /// Lista (coluna) ou sublista dentro de um quadro.
    /// Auto-referência ListaPaiId suporta sublistas com configuração própria.
    /// </summary>
    public class Lista : EntidadeBase
    {
        public string Nome { get; set; } = string.Empty;
        public int Ordem { get; set; }

        public int QuadroId { get; set; }
        public Quadro Quadro { get; set; } = null!;

        /// <summary>Null = lista raiz; preenchido = é sublista de ListaPaiId.</summary>
        public int? ListaPaiId { get; set; }
        public Lista? ListaPai { get; set; }

        /// <summary>
        /// Limite WIP (Work In Progress). Null = sem limite.
        /// Quando ultrapassado, exibe sinalização visual — não bloqueia.
        /// </summary>
        public int? LimiteWip { get; set; }

        /// <summary>
        /// Indica que esta lista representa o backlog do quadro.
        /// </summary>
        public bool EhBacklog { get; set; } = false;

        /// <summary>
        /// Posição da lista no caminho (fluxo) do quadro. Null = fora do caminho.
        /// Impede reordenar colunas contra o fluxo; mover cartão fora do caminho apenas gera aviso.
        /// </summary>
        public int? OrdemFluxo { get; set; }

        /// <summary>Campos fixos do cartão que esta etapa exige (ex.: desenvolvedor atribuído em "A Fazer").</summary>
        public CamposFixosCartao CamposFixosExigidos { get; set; } = CamposFixosCartao.Nenhum;

        /// <summary>
        /// Campos fixos que o cartão não pode ter nesta etapa (ex.: desenvolvedor e prazo no Backlog).
        /// Ficam desabilitados na edição, as automações não os preenchem e são limpos quando o cartão entra na lista.
        /// </summary>
        public CamposFixosCartao CamposFixosBloqueados { get; set; } = CamposFixosCartao.Nenhum;

        /// <summary>
        /// Quando verdadeiro, o cartão só entra na lista depois de preencher o que a etapa exige.
        /// Quando falso (padrão), entra e fica sinalizado como pendente.
        /// </summary>
        public bool BloquearEntradaComPendencias { get; set; } = false;

        /// <summary>Status mostrado ao solicitante no portal quando o cartão está nesta lista (null = mantém o status anterior).</summary>
        public string? StatusPortal { get; set; }

        // Navegação
        public ICollection<Lista> Sublistas { get; set; } = [];
        public ICollection<Cartao> Cartoes { get; set; } = [];
        public ICollection<DefinicaoCampo> DefinicoesCampo { get; set; } = [];
        public ICollection<RegraAutomacao> RegrasAutomacao { get; set; } = [];
    }

    #endregion

    #region Entities/Notificacao.cs

    /// <summary>
    /// Notificação in-app para um usuário. Agrega: atribuição, menção, alerta de campo, conflito, bloqueio.
    /// </summary>
    public class Notificacao : EntidadeBase
    {
        public int DestinatarioId { get; set; }
        public Usuario Destinatario { get; set; } = null!;

        public TipoNotificacao Tipo { get; set; }

        public string Mensagem { get; set; } = string.Empty;

        public bool Lida { get; set; } = false;
        public DateTime? LidaEm { get; set; }

        /// <summary>Cartão de origem para link direto.</summary>
        public int? CartaoOrigemId { get; set; }
        public Cartao? CartaoOrigem { get; set; }
    }

    #endregion

    #region Entities/ParametroSistema.cs

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

    #endregion

    #region Entities/Quadro.cs

    /// <summary>
    /// Quadro Kanban. Um por time/projeto/frente de trabalho.
    /// </summary>
    public class Quadro : EntidadeBase
    {
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string Cor { get; set; } = "#1565C0";

        /// <summary>Id do template de quadro origem (null = criado manualmente).</summary>
        public int? TemplateQuadroId { get; set; }

        // Navegação
        public ICollection<Lista> Listas { get; set; } = [];
        public ICollection<Sprint> Sprints { get; set; } = [];
        public ICollection<Etiqueta> Etiquetas { get; set; } = [];
        public ICollection<TemplateCartao> TemplatesCartao { get; set; } = [];
    }

    #endregion

    #region Entities/RegraAutomacao.cs

    /// <summary>
    /// Regra de automação de um quadro: QUANDO (gatilho) + SE (condições) → ENTÃO (ações).
    /// Parâmetros do gatilho e condições ficam em JSON para permitir tipos variados sem uma tabela por tipo.
    /// </summary>
    public class RegraAutomacao : EntidadeBase
    {
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public bool Ativa { get; set; } = true;

        public int QuadroId { get; set; }
        public Quadro Quadro { get; set; } = null!;

        public TipoGatilho Gatilho { get; set; } = TipoGatilho.CartaoEntrouNaLista;

        /// <summary>Lista observada pelo gatilho (entrar/sair/parado). Null = qualquer lista.</summary>
        public int? ListaId { get; set; }
        public Lista? Lista { get; set; }

        /// <summary>Demais parâmetros do gatilho (dias, horário, campo monitorado...).</summary>
        public string? ParametrosGatilhoJson { get; set; }

        public string? CondicoesJson { get; set; }

        /// <summary>Verdadeiro = todas as condições (E); falso = qualquer uma (OU).</summary>
        public bool ExigirTodasCondicoes { get; set; } = true;

        /// <summary>Gatilhos de tempo só executam em dias úteis dentro do horário comercial configurado.</summary>
        public bool SomenteHorarioComercial { get; set; }

        public DateTime? UltimaExecucaoEm { get; set; }

        public ICollection<AcaoAutomacao> Acoes { get; set; } = [];
    }

    #endregion

    #region Entities/RelacaoCartao.cs

    /// <summary>
    /// Vínculo horizontal entre dois cartões (não hierárquico).
    /// Tipos: BloqueadoPor, Bloqueia, RelacionadoA, DuplicadoDe.
    /// </summary>
    public class RelacaoCartao : EntidadeBase
    {
        public int CartaoOrigemId { get; set; }
        public Cartao CartaoOrigem { get; set; } = null!;

        public int CartaoDestinoId { get; set; }
        public Cartao CartaoDestino { get; set; } = null!;

        public TipoRelacaoCartao Tipo { get; set; }
    }

    #endregion

    #region Entities/Reuniao.cs

    /// <summary>
    /// Registro estruturado de reunião vinculado a um cartão ou item de tarefa.
    /// Visualmente diferenciado de comentários na linha do tempo.
    /// </summary>
    public class Reuniao : EntidadeBase
    {
        public DateTime Data { get; set; }

        /// <summary>Ata/decisões da reunião (texto livre).</summary>
        public string Ata { get; set; } = string.Empty;

        public int AutorId { get; set; }
        public Usuario Autor { get; set; } = null!;

        // Pertence a um cartão OU a um item de tarefa
        public int? CartaoId { get; set; }
        public Cartao? Cartao { get; set; }

        public int? ItemTarefaId { get; set; }
        public ItemTarefa? ItemTarefa { get; set; }

        // Participantes
        public ICollection<ReuniaoParticipante> Participantes { get; set; } = [];
    }

    #endregion

    #region Entities/ReuniaoParticipante.cs

    /// <summary>
    /// Participante de uma reunião. Usado para busca por reuniões com determinado participante.
    /// </summary>
    public class ReuniaoParticipante
    {
        public int ReuniaoId { get; set; }
        public Reuniao Reuniao { get; set; } = null!;

        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;
    }

    #endregion

    #region Entities/Sistema.cs

    /// <summary>
    /// Cadastro global de sistemas/produtos. Campo fixo do cartão (não parte do motor configurável).
    /// </summary>
    public class Sistema : EntidadeBase
    {
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;

        // Navegação
        public ICollection<Cartao> Cartoes { get; set; } = [];
    }

    #endregion

    #region Entities/Sprint.cs

    /// <summary>
    /// Sprint do quadro. Pode conter cartões do backlog.
    /// </summary>
    public class Sprint : EntidadeBase
    {
        public string Nome { get; set; } = string.Empty;
        public string? Meta { get; set; }

        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        public bool Ativa { get; set; } = false;
        public bool Fechada { get; set; } = false;

        public int QuadroId { get; set; }
        public Quadro Quadro { get; set; } = null!;

        // N:N com cartões
        public ICollection<SprintCartao> Cartoes { get; set; } = [];
    }

    #endregion

    #region Entities/SprintCartao.cs

    /// <summary>
    /// Tabela de junção N:N entre Sprint e Cartão.
    /// </summary>
    public class SprintCartao
    {
        public int SprintId { get; set; }
        public Sprint Sprint { get; set; } = null!;

        public int CartaoId { get; set; }
        public Cartao Cartao { get; set; } = null!;

        /// <summary>Data em que o cartão foi incluído na sprint.</summary>
        public DateTime AdicionadoEm { get; set; }
    }

    #endregion

    #region Entities/SugestaoAutomacao.cs

    /// <summary>Alteração proposta por uma automação em modo "sugerir", aguardando aprovação de uma pessoa.</summary>
    public class SugestaoAutomacao
    {
        public int Id { get; set; }

        public int RegraAutomacaoId { get; set; }
        public RegraAutomacao RegraAutomacao { get; set; } = null!;

        public int QuadroId { get; set; }
        public int CartaoId { get; set; }
        public Cartao Cartao { get; set; } = null!;

        public string Descricao { get; set; } = string.Empty;

        /// <summary>O que será aplicado ao aprovar (datas novas ou lista de destino).</summary>
        public string PropostaJson { get; set; } = string.Empty;

        public StatusSugestao Status { get; set; } = StatusSugestao.Pendente;
        public DateTime CriadoEm { get; set; }
        public DateTime? DecididoEm { get; set; }
        public int? DecididoPorId { get; set; }
    }

    #endregion

    #region Entities/TemplateCartao.cs

    /// <summary>
    /// Template de cartão associado a um quadro.
    /// Ao criar novo cartão, o usuário pode escolher entre branco ou a partir de um template.
    /// </summary>
    public class TemplateCartao : EntidadeBase
    {
        public string Nome { get; set; } = string.Empty;
        public string? TituloPadrao { get; set; }
        public string? DescricaoPadrao { get; set; }

        public int QuadroId { get; set; }
        public Quadro Quadro { get; set; } = null!;

        /// <summary>Itens de tarefa padrão, serializados como JSON.</summary>
        public string? ItensTarefaPadrao { get; set; }

        /// <summary>Campos pré-preenchidos, serializados como JSON (dicionário nome→valor).</summary>
        public string? CamposPadrao { get; set; }
    }

    #endregion

    #region Entities/TemplateQuadro.cs

    /// <summary>
    /// Template de quadro. Ao criar um quadro novo, o usuário pode partir de um template
    /// com listas, sublistas, campos e regras de automação já configurados.
    /// A estrutura é serializada como JSON para facilitar cópia.
    /// </summary>
    public class TemplateQuadro : EntidadeBase
    {
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }

        /// <summary>
        /// Estrutura completa do template (listas, campos, automações) serializada em JSON.
        /// </summary>
        public string EstruturaJson { get; set; } = "{}";
    }

    #endregion

    #region Entities/Usuario.cs

    /// <summary>
    /// Usuário do sistema. Sem autenticação real na v1 — plugável depois.
    /// </summary>
    public class Usuario : EntidadeBase
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;

        /// <summary>
        /// Receber e-mail ao ser mencionado (configurável por usuário).
        /// </summary>
        public bool NotificarPorEmail { get; set; } = false;

        /// <summary>Permite que regras de automação enviem e-mail para este usuário.</summary>
        public bool ReceberEmailsAutomacao { get; set; } = true;

        // Navegação
        public ICollection<CartaoDesenvolvedor> CartoesDesenvolvedor { get; set; } = [];
        public ICollection<Notificacao> Notificacoes { get; set; } = [];
    }

    #endregion

    #region Entities/ValorCampoCartao.cs

    /// <summary>
    /// Armazena o valor de um campo configurável para um cartão.
    /// Quando PedirNovamenteACadaEntrada=true, há múltiplos registros por (CartaoId, DefinicaoCampoId),
    /// diferenciados por NumeroEntradaNaLista.
    /// </summary>
    public class ValorCampoCartao : EntidadeBase
    {
        public int CartaoId { get; set; }
        public Cartao Cartao { get; set; } = null!;

        public int DefinicaoCampoId { get; set; }
        public DefinicaoCampo DefinicaoCampo { get; set; } = null!;

        /// <summary>
        /// Valor armazenado como string. A interpretação depende do TipoCampo da definição.
        /// Data: ISO 8601 | Número: invariant culture | Checkbox: "true"/"false"
        /// </summary>
        public string? Valor { get; set; }

        /// <summary>
        /// Número ordinal de entrada do cartão nesta lista.
        /// 1 para a primeira entrada, 2 para a segunda, etc.
        /// Derivado do HistoricoAtividade — armazenado aqui para evitar recálculo constante.
        /// </summary>
        public int NumeroEntradaNaLista { get; set; } = 1;

        public DateTime DataPreenchimento { get; set; }

        /// <summary>Movimento que preencheu este valor automaticamente (null = preenchido pelo usuário).</summary>
        public int? HistoricoOrigemId { get; set; }
        public HistoricoAtividade? HistoricoOrigem { get; set; }

        /// <summary>Verdadeiro quando o registro foi criado pelo preenchimento automático (e não só atualizado).</summary>
        public bool CriadoAutomaticamente { get; set; }

        /// <summary>Valor antes da atualização automática, usado para desfazer em caso de estorno.</summary>
        public string? ValorAnteriorAutomatico { get; set; }

        public int PreenchidoPorId { get; set; }
        public Usuario PreenchidoPor { get; set; } = null!;
    }

    #endregion

    #region Entities/VinculoGit.cs

    public enum TipoVinculoGit { Branch = 1, Commit = 2, PullRequest = 3 }

    public enum EstadoPullRequest { Aberto = 1, Fechado = 2, Mergeado = 3 }

    /// <summary>Branch, commit ou pull request do GitHub que cita o código do cartão (ex.: KB-123).</summary>
    public class VinculoGit
    {
        public int Id { get; set; }

        public int CartaoId { get; set; }
        public Cartao Cartao { get; set; } = null!;

        public TipoVinculoGit Tipo { get; set; }
        public string Repositorio { get; set; } = string.Empty;

        /// <summary>Nome da branch, SHA do commit ou número do pull request.</summary>
        public string Identificador { get; set; } = string.Empty;

        public string Titulo { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Autor { get; set; }
        public EstadoPullRequest? Estado { get; set; }

        public DateTime CriadoEm { get; set; }
        public DateTime AtualizadoEm { get; set; }
    }

    #endregion
}

namespace KanbanDemandas.Core.Enums
{
    #region Enums/Automacao.cs

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

    #endregion

    #region Enums/CamposFixosCartao.cs

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

    #endregion

    #region Enums/PreenchimentoAutomatico.cs

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

    #endregion

    #region Enums/Prioridade.cs

    public enum Prioridade
    {
        Baixa = 1,
        Media = 2,
        Alta = 3,
        Critica = 4
    }

    #endregion

    #region Enums/RegraReentrada.cs

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

    #endregion

    #region Enums/TipoAcaoAutomacao.cs

    public enum TipoAcaoAutomacao
    {
        MoverCartao = 1,
        NotificarResponsavel = 2,
        PreencherCampo = 3,
        AtribuirDesenvolvedor = 4,
        RemoverDesenvolvedores = 5,
        AdicionarEtiqueta = 6,
        RemoverEtiqueta = 7,
        DefinirPrioridade = 8,
        DefinirPrazo = 9,
        AdicionarChecklist = 10,
        CriarCartaoFilho = 11,
        Comentar = 12,
        EnviarEmail = 13,
        CriarAlerta = 14,
        ResolverConflitoDatas = 15,
        EnviarResumo = 16,
        ResolverAlertas = 17,
        PostarNoTeams = 18,
        PostarResumoNoTeams = 19
    }

    #endregion

    #region Enums/TipoCampo.cs

    public enum TipoCampo
    {
        Texto = 1,
        Numero = 2,
        Data = 3,
        Selecao = 4,
        Checkbox = 5,
        AreaTexto = 6
    }

    #endregion

    #region Enums/TipoFeriado.cs

    public enum TipoFeriado
    {
        Nacional = 1,
        Estadual = 2,
        Municipal = 3,
        PontoFacultativo = 4,
        Empresa = 5
    }

    #endregion

    #region Enums/TipoHistoricoAtividade.cs

    public enum TipoHistoricoAtividade
    {
        CartaoEntrou = 1,
        CartaoSaiu = 2,
        CampoAlterado = 3,
        CartaoCriado = 4,
        CartaoMovido = 5,
        ComentarioAdicionado = 6,
        ReuniaoRegistrada = 7,
        AnexoAdicionado = 8,
        ItemTarefaAlterado = 9,
        SprintVinculada = 10,
        EmailEnviado = 11,
        CartaoAtribuido = 12,
        PrazoCriado = 13,
        PrazoAlterado = 14
    }

    #endregion

    #region Enums/TipoNotificacao.cs

    public enum TipoNotificacao
    {
        AtribuicaoCartao = 1,
        MencaoComentario = 2,
        CampoPendente = 3,
        ConflitoData = 4,
        BloqueioCartao = 5,
        AutomacaoRegra = 6,
        MencaoReuniao = 7,
        MensagemSolicitante = 8
    }

    #endregion

    #region Enums/TipoRelacaoCartao.cs

    public enum TipoRelacaoCartao
    {
        BloqueadoPor = 1,
        Bloqueia = 2,
        RelacionadoA = 3,
        DuplicadoDe = 4
    }

    #endregion
}

namespace KanbanDemandas.Core.Interfaces
{
    #region Interfaces/IAnexoStorage.cs

    /// <summary>Resultado de gravar um anexo: onde ficou, se foi compactado e quanto ocupa.</summary>
    public sealed record ArquivoArmazenado(string Caminho, bool Comprimido, long TamanhoArmazenado);

    /// <summary>
    /// Abstração de storage de arquivos (disco local ou blob).
    /// </summary>
    public interface IAnexoStorage
    {
        /// <summary>Salva o conteúdo, compactando quando configurado e vantajoso.</summary>
        Task<ArquivoArmazenado> SalvarAsync(Stream conteudo, string nomeArmazenado, string contentType, CancellationToken ct = default);

        /// <summary>Devolve o conteúdo original (descompactado quando necessário).</summary>
        Task<Stream> ObterAsync(string caminho, CancellationToken ct = default);

        Task ExcluirAsync(string caminho, CancellationToken ct = default);

        /// <summary>Compacta um arquivo já armazenado; null quando não compensa ou o formato não está configurado.</summary>
        Task<ArquivoArmazenado?> CompactarAsync(string caminho, CancellationToken ct = default);
    }

    #endregion

    #region Interfaces/IEmailSender.cs

    public interface IEmailSender
    {
        Task EnviarAsync(string para, string? cc, string assunto, string corpoHtml, CancellationToken ct = default);
    }

    #endregion

    #region Interfaces/IExportacaoService.cs

    public interface IExportacaoService
    {
        Task<byte[]> ExportarParaPdfAsync<T>(IEnumerable<T> dados, string titulo, CancellationToken ct = default);
        Task<byte[]> ExportarParaExcelAsync<T>(IEnumerable<T> dados, string nomePlanilha, CancellationToken ct = default);
    }

    #endregion

    #region Interfaces/IFilaEventosAutomacao.cs

    /// <summary>Algo que aconteceu com um cartão e pode disparar regras de automação.</summary>
    public sealed record EventoAutomacao(
        TipoGatilho Gatilho,
        int CartaoId,
        int UsuarioId,
        int Profundidade,
        IReadOnlyList<int> RegrasNaCadeia,
        int? ListaId = null,
        CampoMonitorado Campo = CampoMonitorado.Qualquer,
        int? UsuarioAlvoId = null);

    /// <summary>Recebe eventos gerados ao gravar alterações; o processamento acontece fora da requisição.</summary>
    public interface IFilaEventosAutomacao
    {
        void Enfileirar(EventoAutomacao evento);
    }

    /// <summary>
    /// Contexto da execução de automação em andamento no fluxo assíncrono atual. Alterações gravadas durante uma
    /// execução geram eventos com profundidade maior e a cadeia de regras, o que permite barrar loops.
    /// </summary>
    public static class ContextoAutomacao
    {
        private static readonly AsyncLocal<(int Profundidade, IReadOnlyList<int> Regras)?> Atual = new();

        public static int Profundidade => Atual.Value?.Profundidade ?? 0;
        public static IReadOnlyList<int> RegrasNaCadeia => Atual.Value?.Regras ?? [];

        public static IDisposable Entrar(int profundidade, IReadOnlyList<int> regras)
        {
            var anterior = Atual.Value;
            Atual.Value = (profundidade, regras);
            return new Restaurar(() => Atual.Value = anterior);
        }

        private sealed class Restaurar(Action acao) : IDisposable
        {
            public void Dispose() => acao();
        }
    }

    #endregion

    #region Interfaces/IPublicadorAlteracoes.cs

    /// <summary>
    /// Propaga, em tempo real, que dados de quadros ou notificações de usuários mudaram após um SaveChanges.
    /// </summary>
    public interface IPublicadorAlteracoes
    {
        Task PublicarAsync(IReadOnlyCollection<int> quadroIds, IReadOnlyCollection<int> destinatariosNotificacao);
    }

    #endregion

    #region Interfaces/IUsuarioAtualProvider.cs

    /// <summary>
    /// Fornece o usuário atualmente logado.
    /// Na v1, implementado com um provider fake/stub.
    /// Plugue a implementação real (AD/SSO) sem alterar o restante do sistema.
    /// </summary>
    public interface IUsuarioAtualProvider
    {
        /// <summary>Retorna o usuário atual. Nunca retorna null — lança exceção se não houver sessão.</summary>
        Usuario ObterUsuarioAtual();

        int ObterIdUsuarioAtual();
    }

    #endregion
}

namespace KanbanDemandas.Core.Regras
{
    #region Regras/CaminhoQuadro.cs

    /// <summary>
    /// Regras do caminho (fluxo) do quadro. Uma lista-pai assume a menor posição entre ela e suas sublistas,
    /// para que um grupo (ex.: Homologação) seja ordenado pela primeira etapa que contém.
    /// </summary>
    public static class CaminhoQuadro
    {
        public static int? FluxoEfetivo(int listaId, IReadOnlyCollection<Lista> todas)
        {
            var menor = todas.FirstOrDefault(l => l.Id == listaId)?.OrdemFluxo;
            foreach (var filha in todas.Where(l => l.ListaPaiId == listaId))
            {
                var fluxo = FluxoEfetivo(filha.Id, todas);
                if (fluxo.HasValue && (!menor.HasValue || fluxo < menor)) menor = fluxo;
            }
            return menor;
        }

        public static bool RespeitaCaminho(IEnumerable<int> ordemIrmas, IReadOnlyCollection<Lista> todas)
        {
            int? anterior = null;
            foreach (var id in ordemIrmas)
            {
                var fluxo = FluxoEfetivo(id, todas);
                if (!fluxo.HasValue) continue;
                if (anterior.HasValue && fluxo < anterior) return false;
                anterior = fluxo;
            }
            return true;
        }

        /// <summary>Reposiciona as listas do caminho na ordem do fluxo, mantendo as demais onde estão.</summary>
        public static List<int> OrdenarPeloCaminho(IReadOnlyList<int> ordemIrmas, IReadOnlyCollection<Lista> todas)
        {
            var fila = new Queue<int>(ordemIrmas
                .Where(id => FluxoEfetivo(id, todas).HasValue)
                .OrderBy(id => FluxoEfetivo(id, todas)));
            return ordemIrmas.Select(id => FluxoEfetivo(id, todas).HasValue ? fila.Dequeue() : id).ToList();
        }

        /// <summary>Aviso quando o cartão vai para uma lista que não é a próxima etapa do caminho; null se estiver ok.</summary>
        public static string? AvisoMovimentacao(int origemId, int destinoId, IReadOnlyCollection<Lista> todas)
        {
            var origem = todas.FirstOrDefault(l => l.Id == origemId);
            var destino = todas.FirstOrDefault(l => l.Id == destinoId);
            if (origem is null || destino is null || origemId == destinoId) return null;

            var caminho = todas.Where(l => l.OrdemFluxo.HasValue).OrderBy(l => l.OrdemFluxo).ToList();
            if (caminho.Count == 0 || !origem.OrdemFluxo.HasValue) return null;

            if (!destino.OrdemFluxo.HasValue)
                return $"'{destino.Nome}' está fora do caminho configurado.";

            var proxima = caminho.FirstOrDefault(l => l.OrdemFluxo > origem.OrdemFluxo);
            if (proxima?.Id == destino.Id) return null;
            if (proxima is null) return $"Fora do caminho: '{origem.Nome}' é a última etapa.";
            return $"Fora do caminho: a próxima etapa depois de '{origem.Nome}' é '{proxima.Nome}'.";
        }
    }

    #endregion

    #region Regras/CamposCartao.cs

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

    #endregion

    #region Regras/CodigoCartao.cs

    /// <summary>Código legível do cartão (ex.: KB-123), usado como protocolo no portal e como referência em commits e PRs.</summary>
    public static class CodigoCartao
    {
        public const string PrefixoPadrao = "KB";

        public static string Formatar(int cartaoId, string? prefixo = null) => $"{Normalizar(prefixo)}-{cartaoId}";

        private static string Normalizar(string? prefixo) => string.IsNullOrWhiteSpace(prefixo) ? PrefixoPadrao : prefixo.Trim().TrimEnd('-').ToUpperInvariant();

        /// <summary>IDs de cartão citados no texto (ex.: "KB-12 corrige KB-7", "feature/kb-12-login").</summary>
        public static IReadOnlyList<int> Extrair(string? texto, string? prefixo = null)
        {
            if (string.IsNullOrWhiteSpace(texto)) return [];
            var padrao = new Regex($@"(?<![A-Za-z0-9]){Regex.Escape(Normalizar(prefixo))}[-_ ]?(\d+)\b", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
            return padrao.Matches(texto).Select(m => int.Parse(m.Groups[1].Value)).Where(id => id > 0).Distinct().ToList();
        }
    }

    #endregion

    #region Regras/ListasQuadro.cs

    /// <summary>
    /// Uma lista que possui sublistas é apenas um agrupador: cartões só ficam em listas "folha".
    /// </summary>
    public static class ListasQuadro
    {
        public static bool EhGrupo(int listaId, IEnumerable<Lista> todas)
            => todas.Any(l => l.ListaPaiId == listaId && !l.Excluido);

        /// <summary>Convenção do sistema: a etapa final é a lista chamada "Concluído".</summary>
        public static bool EhConcluido(int listaId, IEnumerable<Lista> todas)
            => todas.FirstOrDefault(l => l.Id == listaId)?.Nome.Equals("Concluído", StringComparison.OrdinalIgnoreCase) ?? false;

        /// <summary>Desce pela primeira sublista até chegar a uma lista que aceita cartões.</summary>
        public static int PrimeiraFolha(int listaId, IReadOnlyCollection<Lista> todas)
        {
            var primeira = todas.Where(l => l.ListaPaiId == listaId && !l.Excluido).OrderBy(l => l.Ordem).FirstOrDefault();
            return primeira is null ? listaId : PrimeiraFolha(primeira.Id, todas);
        }

        /// <summary>Nome com o caminho dos pais, ex.: "Homologação › Code Review".</summary>
        public static string NomeCompleto(Lista lista, IReadOnlyCollection<Lista> todas)
        {
            var pai = lista.ListaPaiId.HasValue ? todas.FirstOrDefault(l => l.Id == lista.ListaPaiId.Value) : null;
            return pai is null ? lista.Nome : $"{NomeCompleto(pai, todas)} › {lista.Nome}";
        }

        /// <summary>Folhas em ordem visual do quadro (pais antes, sublistas na sequência).</summary>
        public static List<Lista> FolhasEmOrdem(IReadOnlyCollection<Lista> todas)
        {
            var resultado = new List<Lista>();
            void Visitar(int? paiId)
            {
                foreach (var lista in todas.Where(l => l.ListaPaiId == paiId && !l.Excluido).OrderBy(l => l.Ordem))
                {
                    if (EhGrupo(lista.Id, todas)) Visitar(lista.Id);
                    else resultado.Add(lista);
                }
            }
            Visitar(null);
            return resultado;
        }
    }

    #endregion

    #region Regras/RegrasEtapa.cs

    /// <summary>O que cada lista (etapa) exige do cartão e o que falta para ele cumprir.</summary>
    public static class RegrasEtapa
    {
        public static readonly CamposFixosCartao[] CamposFixos =
            [CamposFixosCartao.Desenvolvedor, CamposFixosCartao.Prazo, CamposFixosCartao.Estimativa, CamposFixosCartao.Sistema, CamposFixosCartao.Solicitante];

        /// <summary>Campos que uma lista pode proibir (inclui a Data de início, que não é exigível: ela vem da etapa de início).</summary>
        public static readonly CamposFixosCartao[] CamposBloqueaveis =
            [CamposFixosCartao.Desenvolvedor, CamposFixosCartao.Prazo, CamposFixosCartao.DataInicio, CamposFixosCartao.Estimativa, CamposFixosCartao.Sistema, CamposFixosCartao.Solicitante];

        public static string Nome(CamposFixosCartao campo) => campo switch
        {
            CamposFixosCartao.Desenvolvedor => "Desenvolvedor",
            CamposFixosCartao.Prazo => "Prazo",
            CamposFixosCartao.Estimativa => "Estimativa",
            CamposFixosCartao.Sistema => "Sistema",
            CamposFixosCartao.Solicitante => "Solicitante",
            CamposFixosCartao.DataInicio => "Data de início",
            _ => campo.ToString()
        };

        /// <summary>Campos fixos exigidos pela lista que o cartão ainda não tem. Requer Desenvolvedores carregados.</summary>
        public static CamposFixosCartao FixosFaltando(Cartao cartao, Lista lista)
        {
            var faltando = CamposFixosCartao.Nenhum;
            var exigidos = lista.CamposFixosExigidos;
            if (exigidos.HasFlag(CamposFixosCartao.Desenvolvedor) && cartao.Desenvolvedores.Count == 0) faltando |= CamposFixosCartao.Desenvolvedor;
            if (exigidos.HasFlag(CamposFixosCartao.Prazo) && !cartao.Prazo.HasValue) faltando |= CamposFixosCartao.Prazo;
            if (exigidos.HasFlag(CamposFixosCartao.Estimativa) && !cartao.Estimativa.HasValue) faltando |= CamposFixosCartao.Estimativa;
            if (exigidos.HasFlag(CamposFixosCartao.Sistema) && !cartao.SistemaId.HasValue) faltando |= CamposFixosCartao.Sistema;
            if (exigidos.HasFlag(CamposFixosCartao.Solicitante) && !cartao.SolicitanteId.HasValue) faltando |= CamposFixosCartao.Solicitante;
            return faltando;
        }

        /// <summary>Campos que a lista proíbe e o cartão tem preenchidos. Requer Desenvolvedores carregados.</summary>
        public static CamposFixosCartao BloqueadosPreenchidos(Cartao cartao, Lista lista)
        {
            var preenchidos = CamposFixosCartao.Nenhum;
            var bloqueados = lista.CamposFixosBloqueados;
            if (bloqueados.HasFlag(CamposFixosCartao.Desenvolvedor) && cartao.Desenvolvedores.Count > 0) preenchidos |= CamposFixosCartao.Desenvolvedor;
            if (bloqueados.HasFlag(CamposFixosCartao.Prazo) && cartao.Prazo.HasValue) preenchidos |= CamposFixosCartao.Prazo;
            if (bloqueados.HasFlag(CamposFixosCartao.DataInicio) && cartao.DataInicio.HasValue) preenchidos |= CamposFixosCartao.DataInicio;
            if (bloqueados.HasFlag(CamposFixosCartao.Estimativa) && cartao.Estimativa.HasValue) preenchidos |= CamposFixosCartao.Estimativa;
            if (bloqueados.HasFlag(CamposFixosCartao.Sistema) && cartao.SistemaId.HasValue) preenchidos |= CamposFixosCartao.Sistema;
            if (bloqueados.HasFlag(CamposFixosCartao.Solicitante) && cartao.SolicitanteId.HasValue) preenchidos |= CamposFixosCartao.Solicitante;
            return preenchidos;
        }

        public static IEnumerable<CamposFixosCartao> Separar(CamposFixosCartao campos)
            => CamposBloqueaveis.Where(c => campos.HasFlag(c));

        public static string Descrever(CamposFixosCartao campos) => string.Join(", ", Separar(campos).Select(Nome));

        /// <summary>Campos que o usuário digita (os automáticos são preenchidos pelo sistema ao entrar na lista).</summary>
        public static bool EhManual(DefinicaoCampo campo) => campo.Preenchimento == PreenchimentoAutomatico.Manual;

        public static string DescreverAutomatico(DefinicaoCampo campo)
        {
            var origem = campo.Preenchimento == PreenchimentoAutomatico.DataEntrada ? "data de entrada na lista" : "usuário que moveu";
            var reentrada = campo.RegraReentrada switch
            {
                RegraReentrada.AtualizarUltimo => "atualiza a cada entrada",
                RegraReentrada.NovoRegistro => "novo registro a cada entrada",
                _ => "mantém a primeira entrada"
            };
            return $"Automático: {origem} ({reentrada})";
        }
    }

    /// <summary>Pendências para o cartão cumprir a etapa de uma lista.</summary>
    public sealed record PendenciasEtapa(
        int ListaId,
        string ListaNome,
        bool Bloqueia,
        CamposFixosCartao FixosFaltando,
        IReadOnlyList<DefinicaoCampo> ObrigatoriosVazios,
        IReadOnlyList<DefinicaoCampo> Recorrentes,
        CamposFixosCartao BloqueadosPreenchidos = CamposFixosCartao.Nenhum)
    {
        public bool TemObrigatoriasFaltando => FixosFaltando != CamposFixosCartao.Nenhum || ObrigatoriosVazios.Count > 0;
        public bool TemAlgoParaPreencher => TemObrigatoriasFaltando || Recorrentes.Count > 0;

        public IEnumerable<DefinicaoCampo> CamposParaPreencher
            => ObrigatoriosVazios.Concat(Recorrentes).DistinctBy(c => c.Id).OrderBy(c => c.Ordem);
    }

    /// <summary>Dados informados pelo usuário ao entrar em uma etapa.</summary>
    public sealed class DadosEtapa
    {
        public List<int> DesenvolvedorIds { get; set; } = [];
        public DateTime? Prazo { get; set; }
        public decimal? Estimativa { get; set; }
        public int? SistemaId { get; set; }
        public int? SolicitanteId { get; set; }
        public Dictionary<int, string?> ValoresCampo { get; set; } = [];
    }

    #endregion

    #region Regras/StatusPortal.cs

    public sealed record MudancaStatusPortal(string Status, DateTime Em);

    /// <summary>
    /// Status que o solicitante vê no portal. Cada lista pode ter um status público; listas sem status mantêm
    /// o último status público (o solicitante não enxerga movimentações internas entre colunas).
    /// </summary>
    public static class StatusPortal
    {
        public const string StatusInicialPadrao = "Recebida";

        /// <summary>Linha do tempo de status públicos, sem repetições consecutivas. Entradas estornadas já vêm filtradas.</summary>
        public static List<MudancaStatusPortal> Historico(Cartao cartao, IEnumerable<HistoricoAtividade> entradas, IReadOnlyCollection<Lista> listas, string? statusInicial)
        {
            var resultado = new List<MudancaStatusPortal> { new(string.IsNullOrWhiteSpace(statusInicial) ? StatusInicialPadrao : statusInicial.Trim(), cartao.CriadoEm) };
            var ordenadas = entradas
                .Where(h => h.ListaDestinoId.HasValue && (h.Tipo == TipoHistoricoAtividade.CartaoMovido || h.Tipo == TipoHistoricoAtividade.CartaoCriado))
                .OrderBy(h => h.OcorridoEm).ThenBy(h => h.Id);
            foreach (var entrada in ordenadas)
            {
                var status = listas.FirstOrDefault(l => l.Id == entrada.ListaDestinoId)?.StatusPortal?.Trim();
                if (string.IsNullOrEmpty(status) || status == resultado[^1].Status) continue;
                resultado.Add(new MudancaStatusPortal(status, entrada.OcorridoEm));
            }
            return resultado;
        }

        public static string Atual(Cartao cartao, IEnumerable<HistoricoAtividade> entradas, IReadOnlyCollection<Lista> listas, string? statusInicial)
            => Historico(cartao, entradas, listas, statusInicial)[^1].Status;
    }

    /// <summary>Informações que o admin libera para o solicitante ver no portal (Parametrização › Portal).</summary>
    [Flags]
    public enum CamposPortal
    {
        Nenhum = 0,
        Prazo = 1,
        DataInicio = 2,
        Prioridade = 4,
        Sistema = 8,
        Desenvolvedores = 16,
        Estimativa = 32,
        Etiquetas = 64,
        AnexosDoTime = 128,
        HistoricoStatus = 256,
        Comentarios = 512
    }

    public static class ConfiguracaoPortal
    {
        public static CamposPortal LerCampos(string? valor)
            => (valor ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Aggregate(CamposPortal.Nenhum, (acc, nome) => Enum.TryParse<CamposPortal>(nome, true, out var campo) ? acc | campo : acc);

        public static string GravarCampos(CamposPortal campos)
            => string.Join(",", Enum.GetValues<CamposPortal>().Where(c => c != CamposPortal.Nenhum && campos.HasFlag(c)));

        public static string Nome(CamposPortal campo) => campo switch
        {
            CamposPortal.DataInicio => "Data de início",
            CamposPortal.Desenvolvedores => "Desenvolvedores responsáveis",
            CamposPortal.AnexosDoTime => "Anexos adicionados pelo time",
            CamposPortal.HistoricoStatus => "Histórico de status",
            CamposPortal.Comentarios => "Conversa (comentários públicos)",
            _ => campo.ToString()
        };

        public static List<string> LerTipos(string? valor)
            => (valor ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    #endregion
}
