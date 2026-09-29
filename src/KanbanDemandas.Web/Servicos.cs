using System.Globalization;
using System.Net.Http.Json;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using System.Threading.Channels;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Core.Regras;
using KanbanDemandas.Infrastructure.Configuracao;
using KanbanDemandas.Infrastructure.Data;
using KanbanDemandas.Infrastructure.Services;
using KanbanDemandas.Web.Components.Shared;
using KanbanDemandas.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace KanbanDemandas.Web.Configuracao
{
    #region Configuracao/CatalogoParametros.cs

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

    #endregion

    #region Configuracao/ParametrosService.cs

    /// <summary>Lê e grava os parâmetros da tela; ao salvar, a configuração é recarregada sem reiniciar o sistema.</summary>
    public sealed class ParametrosService(
        KanbanDbContext db,
        IConfiguration configuracao,
        ParametrosBancoConfigurationProvider provedor,
        IUsuarioAtualProvider usuarioProvider)
    {
        public string? ValorAtual(string chave) => configuracao[chave];

        /// <summary>Valor vindo do appsettings (sem a sobreposição do banco).</summary>
        public string? ValorPadrao(string chave)
        {
            if (configuracao is not IConfigurationRoot raiz) return null;
            foreach (var fonte in raiz.Providers.Reverse())
            {
                if (fonte is ParametrosBancoConfigurationProvider) continue;
                if (fonte.TryGet(chave, out var valor)) return valor;
            }
            return null;
        }

        public async Task<HashSet<string>> ChavesAlteradasAsync()
            => (await db.ParametrosSistema.AsNoTracking().Select(p => p.Chave).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        public static string? Validar(ParametroDefinicao definicao, string? valor)
        {
            var texto = valor?.Trim() ?? "";
            switch (definicao.Tipo)
            {
                case TipoParametro.Inteiro:
                case TipoParametro.Decimal:
                case TipoParametro.Usuario:
                case TipoParametro.Quadro:
                case TipoParametro.Lista:
                    if (!decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var numero))
                        return $"{definicao.Nome}: informe um número.";
                    if (definicao.Tipo != TipoParametro.Decimal && numero != Math.Truncate(numero)) return $"{definicao.Nome}: informe um número inteiro.";
                    if (definicao.Minimo.HasValue && numero < definicao.Minimo) return $"{definicao.Nome}: mínimo {definicao.Minimo}.";
                    if (definicao.Maximo.HasValue && numero > definicao.Maximo) return $"{definicao.Nome}: máximo {definicao.Maximo}.";
                    break;
                case TipoParametro.Booleano when !bool.TryParse(texto, out _):
                    return $"{definicao.Nome}: valor inválido.";
                case TipoParametro.Horario when !TimeSpan.TryParse(texto, out _):
                    return $"{definicao.Nome}: informe o horário como HH:mm.";
                case TipoParametro.Segredo when texto.Length is > 0 and < 16:
                    return $"{definicao.Nome}: use pelo menos 16 caracteres.";
                case TipoParametro.MultiOpcoes when texto.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any(v => definicao.Opcoes?.Any(o => o.Valor == v) != true):
                    return $"{definicao.Nome}: opção inválida.";
                case TipoParametro.Opcoes when definicao.Opcoes?.Any(o => o.Valor == texto) != true:
                    return $"{definicao.Nome}: opção inválida.";
                case TipoParametro.Texto when definicao.Chave is "Aplicacao:UrlBase" or "Portal:UrlBase" && texto.Length > 0
                    && !(Uri.TryCreate(texto, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"):
                    return $"{definicao.Nome}: informe um endereço começando com http:// ou https://.";
            }
            return null;
        }

        /// <summary>Grava os valores; igual ao padrão do appsettings = remove a sobreposição.</summary>
        public async Task SalvarAsync(IReadOnlyDictionary<string, string?> valores)
        {
            var existentes = await db.ParametrosSistema.ToDictionaryAsync(p => p.Chave, StringComparer.OrdinalIgnoreCase);
            foreach (var (chave, valorBruto) in valores)
            {
                if (CatalogoParametros.Buscar(chave) is null) continue;
                var valor = valorBruto?.Trim() ?? "";
                var igualAoPadrao = valor == (ValorPadrao(chave) ?? "");
                if (existentes.TryGetValue(chave, out var existente))
                {
                    if (igualAoPadrao) db.ParametrosSistema.Remove(existente);
                    else if (existente.Valor != valor)
                    {
                        existente.Valor = valor;
                        existente.AlteradoEm = DateTime.UtcNow;
                        existente.AlteradoPorId = usuarioProvider.ObterIdUsuarioAtual();
                    }
                }
                else if (!igualAoPadrao)
                {
                    db.ParametrosSistema.Add(new ParametroSistema
                    {
                        Chave = chave, Valor = valor, AlteradoEm = DateTime.UtcNow, AlteradoPorId = usuarioProvider.ObterIdUsuarioAtual()
                    });
                }
            }
            await db.SaveChangesAsync();
            provedor.Recarregar();
        }

        public async Task RestaurarAsync(string chave)
        {
            var existente = await db.ParametrosSistema.FirstOrDefaultAsync(p => p.Chave == chave);
            if (existente is null) return;
            db.ParametrosSistema.Remove(existente);
            await db.SaveChangesAsync();
            provedor.Recarregar();
        }
    }

    #endregion
}

namespace KanbanDemandas.Web.Hubs
{
    #region Hubs/QuadroHub.cs

    public sealed class QuadroHub(KanbanDemandas.Web.Services.PublicadorAlteracoesTempoReal publicador) : Hub
    {
        /// <summary>
        /// Usado pelo portal de solicitações (processo separado) para que quadros abertos e sinos de notificação
        /// deste sistema atualizem na hora. Só dispara recarga; nenhum dado trafega por aqui.
        /// </summary>
        public Task AvisarAlteracao(int[] quadroIds, int[] destinatariosNotificacao)
            => publicador.PublicarAsync(quadroIds.Distinct().Take(50).ToList(), destinatariosNotificacao.Distinct().Take(500).ToList());

        public Task EntrarNoQuadro(int quadroId)
            => Groups.AddToGroupAsync(Context.ConnectionId, Grupo(quadroId));

        public Task SairDoQuadro(int quadroId)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, Grupo(quadroId));

        public static string Grupo(int quadroId) => $"quadro:{quadroId}";
    }

    #endregion
}

namespace KanbanDemandas.Web.Services
{
    #region Services/AssistenteMovimentacao.cs

    /// <summary>
    /// Fluxo de interface para mover um cartão, igual em todas as telas: se a lista de destino bloqueia a entrada
    /// com pendências, pede os dados antes (cancelar = não move); se só avisa, move e depois oferece o preenchimento.
    /// </summary>
    public sealed class AssistenteMovimentacao(
        MovimentacaoCartaoService movimentacao,
        ValoresCampoService valoresCampo,
        IDialogService dialog,
        ISnackbar snackbar)
    {
        public async Task<ResultadoMovimentacao?> MoverAsync(int cartaoId, int listaDestinoId, int? cartaoAlvoId)
        {
            var pendencias = await movimentacao.ObterPendenciasAsync(cartaoId, listaDestinoId);
            if (pendencias is { BloqueadosPreenchidos: not CamposFixosCartao.Nenhum })
            {
                var confirmado = await dialog.ShowMessageBox($"Entrada em {pendencias.ListaNome}",
                    $"\"{pendencias.ListaNome}\" não permite: {RegrasEtapa.Descrever(pendencias.BloqueadosPreenchidos)}. " +
                    "Ao mover, esses dados serão removidos do cartão (fica registrado no histórico).",
                    yesText: "Mover e remover", cancelText: "Cancelar");
                if (confirmado != true) return null;
            }
            DadosEtapa? dadosAntes = null;
            if (pendencias is { Bloqueia: true, TemObrigatoriasFaltando: true })
            {
                dadosAntes = await PedirDadosAsync(cartaoId, pendencias, obrigatorio: true);
                if (dadosAntes is null)
                {
                    snackbar.Add($"Movimento cancelado: '{pendencias.ListaNome}' exige preencher as pendências antes de entrar.", Severity.Info);
                    return null;
                }
            }

            ResultadoMovimentacao? resultado;
            try
            {
                resultado = await movimentacao.MoverAsync(cartaoId, listaDestinoId, cartaoAlvoId);
            }
            catch (Exception)
            {
                snackbar.Add("Não foi possível mover o cartão.", Severity.Error);
                return null;
            }
            if (resultado is null) return null;

            if (resultado.Estornado)
            {
                snackbar.Add($"Movimento desfeito: o cartão voltou para '{resultado.ListaDestinoNome}' e a ida foi retirada do histórico.", Severity.Info);
                return resultado;
            }
            if (resultado.MudouDeLista)
                snackbar.Add($"Cartão movido para {resultado.ListaDestinoNome}.", Severity.Success);
            if (resultado.AvisoCaminho is not null)
                snackbar.Add(resultado.AvisoCaminho, Severity.Warning);
            if (resultado.CamposLimpos != CamposFixosCartao.Nenhum)
                snackbar.Add($"Removido por não ser permitido em '{resultado.ListaDestinoNome}': {RegrasEtapa.Descrever(resultado.CamposLimpos)}.", Severity.Info);

            if (!resultado.MudouDeLista) return resultado;

            if (dadosAntes is not null)
            {
                await valoresCampo.AplicarDadosEtapaAsync(cartaoId, resultado.ListaDestinoId, dadosAntes);
            }
            else if (pendencias is { TemAlgoParaPreencher: true })
            {
                var dadosDepois = await PedirDadosAsync(cartaoId, pendencias, obrigatorio: false);
                if (dadosDepois is not null)
                    await valoresCampo.AplicarDadosEtapaAsync(cartaoId, resultado.ListaDestinoId, dadosDepois);
            }
            return resultado;
        }

        private async Task<DadosEtapa?> PedirDadosAsync(int cartaoId, PendenciasEtapa pendencias, bool obrigatorio)
        {
            var referencia = await dialog.ShowAsync<DialogEntradaEtapa>($"Entrada em {pendencias.ListaNome}",
                new DialogParameters<DialogEntradaEtapa>
                {
                    { x => x.CartaoId, cartaoId }, { x => x.Pendencias, pendencias }, { x => x.Obrigatorio, obrigatorio }
                },
                new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, BackdropClick = !obrigatorio });
            var resultado = await referencia.Result;
            return resultado is { Canceled: false, Data: DadosEtapa dados } ? dados : null;
        }
    }

    #endregion

    #region Services/CompactacaoAnexosService.cs

    public sealed record ResultadoCompactacao(int Analisados, int Compactados, long BytesEconomizados, int Erros);

    /// <summary>Compacta anexos gravados antes da compactação existir (ou antes de o formato ser incluído na lista).</summary>
    public sealed class CompactacaoAnexosService(KanbanDbContext db, IAnexoStorage storage, ILogger<CompactacaoAnexosService> logger)
    {
        public async Task<ResultadoCompactacao> CompactarExistentesAsync(CancellationToken ct = default)
        {
            var anexos = await db.Anexos.Where(a => !a.Excluido && !a.Comprimido).ToListAsync(ct);
            var compactados = 0;
            var erros = 0;
            long economia = 0;
            foreach (var anexo in anexos)
            {
                try
                {
                    var resultado = await storage.CompactarAsync(anexo.Caminho, ct);
                    if (resultado is null) continue;
                    var antes = anexo.TamanhoArmazenadoBytes > 0 ? anexo.TamanhoArmazenadoBytes : anexo.TamanhoBytes;
                    anexo.Caminho = resultado.Caminho;
                    anexo.NomeArmazenado = Path.GetFileName(resultado.Caminho);
                    anexo.Comprimido = true;
                    anexo.TamanhoArmazenadoBytes = resultado.TamanhoArmazenado;
                    economia += antes - resultado.TamanhoArmazenado;
                    compactados++;
                    await db.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    erros++;
                    logger.LogWarning(ex, "Falha ao compactar o anexo {Anexo}.", anexo.Id);
                }
            }
            return new ResultadoCompactacao(anexos.Count, compactados, economia, erros);
        }
    }

    #endregion

    #region Services/FeriadosService.cs

    public sealed record FeriadoCalculado(DateTime Data, string Nome, TipoFeriado Tipo);

    /// <summary>Feriados nacionais brasileiros calculados localmente (fixos e móveis, a partir da Páscoa).</summary>
    public static class FeriadosNacionais
    {
        /// <summary>Domingo de Páscoa pelo algoritmo de Meeus/Jones/Butcher (calendário gregoriano).</summary>
        public static DateTime Pascoa(int ano)
        {
            var a = ano % 19;
            var b = ano / 100;
            var c = ano % 100;
            var d = b / 4;
            var e = b % 4;
            var f = (b + 8) / 25;
            var g = (b - f + 1) / 3;
            var h = (19 * a + b - d - g + 15) % 30;
            var i = c / 4;
            var k = c % 4;
            var l = (32 + 2 * e + 2 * i - h - k) % 7;
            var m = (a + 11 * h + 22 * l) / 451;
            var mes = (h + l - 7 * m + 114) / 31;
            var dia = (h + l - 7 * m + 114) % 31 + 1;
            return new DateTime(ano, mes, dia);
        }

        public static List<FeriadoCalculado> Calcular(int ano)
        {
            var pascoa = Pascoa(ano);
            var lista = new List<FeriadoCalculado>
            {
                new(new DateTime(ano, 1, 1), "Confraternização Universal", TipoFeriado.Nacional),
                new(pascoa.AddDays(-48), "Carnaval (segunda-feira)", TipoFeriado.PontoFacultativo),
                new(pascoa.AddDays(-47), "Carnaval (terça-feira)", TipoFeriado.PontoFacultativo),
                new(pascoa.AddDays(-2), "Sexta-feira Santa", TipoFeriado.Nacional),
                new(new DateTime(ano, 4, 21), "Tiradentes", TipoFeriado.Nacional),
                new(new DateTime(ano, 5, 1), "Dia do Trabalho", TipoFeriado.Nacional),
                new(pascoa.AddDays(60), "Corpus Christi", TipoFeriado.PontoFacultativo),
                new(new DateTime(ano, 9, 7), "Independência do Brasil", TipoFeriado.Nacional),
                new(new DateTime(ano, 10, 12), "Nossa Senhora Aparecida", TipoFeriado.Nacional),
                new(new DateTime(ano, 11, 2), "Finados", TipoFeriado.Nacional),
                new(new DateTime(ano, 11, 15), "Proclamação da República", TipoFeriado.Nacional),
                new(new DateTime(ano, 12, 25), "Natal", TipoFeriado.Nacional)
            };
            // Dia Nacional de Zumbi e da Consciência Negra é feriado nacional desde 2024 (Lei 14.759/2023).
            if (ano >= 2024) lista.Add(new(new DateTime(ano, 11, 20), "Dia Nacional de Zumbi e da Consciência Negra", TipoFeriado.Nacional));
            return lista.OrderBy(f => f.Data).ToList();
        }
    }

    /// <summary>Cadastro de feriados e conjunto de datas usado nos cálculos de dias úteis.</summary>
    public sealed class FeriadosService(KanbanDbContext db, IHttpClientFactory httpClientFactory, IUsuarioAtualProvider usuarioProvider,
        ILogger<FeriadosService> logger)
    {
        private sealed record FeriadoBrasilApi(string Date, string Name, string Type);

        /// <summary>Datas não úteis no período, com os recorrentes projetados em cada ano.</summary>
        public static async Task<HashSet<DateTime>> CarregarDatasAsync(KanbanDbContext db, int anoInicial, int anoFinal)
        {
            var feriados = await db.Feriados.AsNoTracking().ToListAsync();
            var datas = new HashSet<DateTime>();
            foreach (var feriado in feriados)
            {
                if (!feriado.RecorrenteAnual)
                {
                    datas.Add(feriado.Data.Date);
                    continue;
                }
                for (var ano = anoInicial; ano <= anoFinal; ano++)
                    if (DateTime.DaysInMonth(ano, feriado.Data.Month) >= feriado.Data.Day)
                        datas.Add(new DateTime(ano, feriado.Data.Month, feriado.Data.Day));
            }
            return datas;
        }

        /// <summary>
        /// Traz os feriados nacionais do ano pela BrasilAPI; se ela não responder, usa o cálculo local.
        /// Não duplica datas já cadastradas.
        /// </summary>
        public async Task<(int Inseridos, string Fonte)> ImportarNacionaisAsync(int ano, bool usarApi)
        {
            List<FeriadoCalculado> feriados;
            var fonte = "cálculo local";
            if (usarApi)
            {
                try
                {
                    var cliente = httpClientFactory.CreateClient("BrasilAPI");
                    var resposta = await cliente.GetFromJsonAsync<List<FeriadoBrasilApi>>($"api/feriados/v1/{ano}") ?? [];
                    feriados = resposta.Select(f => new FeriadoCalculado(DateTime.Parse(f.Date, System.Globalization.CultureInfo.InvariantCulture), f.Name, TipoFeriado.Nacional)).ToList();
                    // A BrasilAPI não traz os pontos facultativos (Carnaval e Corpus Christi); eles vêm do cálculo local.
                    feriados.AddRange(FeriadosNacionais.Calcular(ano).Where(f => f.Tipo == TipoFeriado.PontoFacultativo));
                    fonte = "BrasilAPI";
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "BrasilAPI indisponível; usando cálculo local dos feriados de {Ano}.", ano);
                    feriados = FeriadosNacionais.Calcular(ano);
                    fonte = "cálculo local (BrasilAPI indisponível)";
                }
            }
            else
            {
                feriados = FeriadosNacionais.Calcular(ano);
            }

            var inicio = new DateTime(ano, 1, 1);
            var fim = new DateTime(ano, 12, 31);
            var existentes = (await db.Feriados.Where(f => f.Data >= inicio && f.Data <= fim).Select(f => f.Data).ToListAsync())
                .Select(d => d.Date).ToHashSet();
            var usuarioId = usuarioProvider.ObterIdUsuarioAtual();
            var novos = feriados.Where(f => existentes.Add(f.Data.Date)).Select(f => new Feriado
            {
                Data = f.Data.Date, Nome = f.Nome, Tipo = f.Tipo, CriadoPorId = usuarioId, CriadoEm = DateTime.UtcNow
            }).ToList();
            db.Feriados.AddRange(novos);
            await db.SaveChangesAsync();
            return (novos.Count, fonte);
        }
    }

    #endregion

    #region Services/OpcaoSelecao.cs

    public sealed record OpcaoSelecao(int Id, string Nome, string? Cor = null);

    #endregion

    #region Services/PublicadorAlteracoesTempoReal.cs

    /// <summary>
    /// Quadros: avisa o grupo SignalR de cada quadro afetado. Notificações: dispara um evento em memória,
    /// já que no Blazor Server todos os circuitos (e o sino do layout) vivem neste mesmo processo.
    /// </summary>
    public sealed class PublicadorAlteracoesTempoReal(IHubContext<QuadroHub> hubContext) : IPublicadorAlteracoes
    {
        public event Action<IReadOnlyCollection<int>>? NotificacoesAlteradas;

        public async Task PublicarAsync(IReadOnlyCollection<int> quadroIds, IReadOnlyCollection<int> destinatariosNotificacao)
        {
            foreach (var quadroId in quadroIds)
                await hubContext.Clients.Group(QuadroHub.Grupo(quadroId)).SendAsync("QuadroAtualizado");

            if (destinatariosNotificacao.Count > 0)
                NotificacoesAlteradas?.Invoke(destinatariosNotificacao);
        }
    }

    #endregion

    #region Services/ValoresCampoService.cs

    /// <summary>Gravação dos campos de lista e dos dados exigidos por uma etapa, usada por todas as telas.</summary>
    public sealed class ValoresCampoService(KanbanDbContext db, IUsuarioAtualProvider usuarioProvider)
    {
        /// <summary>
        /// Grava os valores dos campos manuais da lista. Campo "pedir a cada entrada" ganha um registro por entrada;
        /// os demais têm um valor único que é atualizado. Campos automáticos são ignorados.
        /// </summary>
        public async Task SalvarValoresAsync(int cartaoId, int listaId, IReadOnlyDictionary<int, string?> valores)
        {
            await AplicarValoresAsync(cartaoId, listaId, valores);
            await db.SaveChangesAsync();
        }

        /// <summary>Aplica o que o usuário informou para cumprir a etapa (campos fixos + campos da lista).</summary>
        public async Task AplicarDadosEtapaAsync(int cartaoId, int listaId, DadosEtapa dados)
        {
            var cartao = await db.Cartoes.Include(c => c.Desenvolvedores).FirstOrDefaultAsync(c => c.Id == cartaoId && !c.Excluido);
            if (cartao is null) return;
            var usuarioId = usuarioProvider.ObterIdUsuarioAtual();

            var novos = dados.DesenvolvedorIds.Where(id => cartao.Desenvolvedores.All(d => d.UsuarioId != id)).Distinct().ToList();
            var semPrincipal = cartao.Desenvolvedores.All(d => !d.Principal);
            foreach (var id in novos)
            {
                db.CartaoDesenvolvedores.Add(new CartaoDesenvolvedor { CartaoId = cartaoId, UsuarioId = id, Principal = semPrincipal && id == novos[0] });
                if (id != usuarioId)
                {
                    db.Notificacoes.Add(new Notificacao
                    {
                        DestinatarioId = id, CartaoOrigemId = cartaoId, Tipo = TipoNotificacao.AtribuicaoCartao,
                        Mensagem = $"Você foi atribuído ao cartão '{cartao.Titulo}'.", CriadoPorId = usuarioId, CriadoEm = DateTime.UtcNow
                    });
                }
            }
            if (dados.Prazo.HasValue) cartao.Prazo = dados.Prazo.Value.ToUniversalTime();
            if (dados.Estimativa.HasValue) cartao.Estimativa = dados.Estimativa;
            if (dados.SistemaId.HasValue) cartao.SistemaId = dados.SistemaId;
            if (dados.SolicitanteId.HasValue) cartao.SolicitanteId = dados.SolicitanteId;

            await AplicarValoresAsync(cartaoId, listaId, dados.ValoresCampo);
            await db.SaveChangesAsync();
        }

        private async Task AplicarValoresAsync(int cartaoId, int listaId, IReadOnlyDictionary<int, string?> valores)
        {
            if (valores.Count == 0) return;
            var usuarioId = usuarioProvider.ObterIdUsuarioAtual();
            var campos = await db.DefinicoesCampo
                .Where(c => c.ListaId == listaId && !c.Excluido && c.Preenchimento == PreenchimentoAutomatico.Manual)
                .ToListAsync();

            // O número da entrada vem do histórico: quantas vezes o cartão entrou nesta lista (estornos não contam).
            var numeroEntradaAtual = Math.Max(1, await db.HistoricoAtividades.CountAsync(h => h.CartaoId == cartaoId && h.ListaDestinoId == listaId
                && (h.Tipo == TipoHistoricoAtividade.CartaoMovido || h.Tipo == TipoHistoricoAtividade.CartaoCriado)));

            foreach (var campo in campos.Where(c => valores.ContainsKey(c.Id)))
            {
                var valor = valores[campo.Id];
                var existente = await db.ValoresCampoCartao
                    .Where(v => v.CartaoId == cartaoId && v.DefinicaoCampoId == campo.Id)
                    .OrderByDescending(v => v.NumeroEntradaNaLista).ThenByDescending(v => v.DataPreenchimento)
                    .FirstOrDefaultAsync();

                if (existente is null || (campo.PedirNovamenteACadaEntrada && existente.NumeroEntradaNaLista != numeroEntradaAtual))
                {
                    if (string.IsNullOrWhiteSpace(valor)) continue;
                    db.ValoresCampoCartao.Add(new ValorCampoCartao
                    {
                        CartaoId = cartaoId, DefinicaoCampoId = campo.Id, Valor = valor,
                        NumeroEntradaNaLista = numeroEntradaAtual, DataPreenchimento = DateTime.UtcNow,
                        PreenchidoPorId = usuarioId, CriadoPorId = usuarioId, CriadoEm = DateTime.UtcNow
                    });
                }
                else if (existente.Valor != valor)
                {
                    existente.Valor = valor;
                    existente.DataPreenchimento = DateTime.UtcNow;
                    existente.PreenchidoPorId = usuarioId;
                }
            }
        }
    }

    #endregion
}

namespace KanbanDemandas.Web.Services.Automacoes
{
    #region Services/Automacoes/AvaliadorCondicoes.cs

    public static class AvaliadorCondicoes
    {
        public static bool Atende(IReadOnlyCollection<Condicao> condicoes, bool exigirTodas, Cartao cartao, FotoQuadro foto, decimal pontosPorDia)
        {
            if (condicoes.Count == 0) return true;
            return exigirTodas
                ? condicoes.All(c => Avaliar(c, cartao, foto, pontosPorDia))
                : condicoes.Any(c => Avaliar(c, cartao, foto, pontosPorDia));
        }

        public static bool Avaliar(Condicao condicao, Cartao cartao, FotoQuadro foto, decimal pontosPorDia)
        {
            var op = condicao.Operador;
            return condicao.Tipo switch
            {
                TipoCondicao.Lista => Conjunto(op, condicao.Ids.Count > 0 && foto.ListaOuPaiEm(cartao.ListaId, condicao.Ids), true),
                TipoCondicao.Prioridade => op switch
                {
                    OperadorCondicao.Vazio => !cartao.Prioridade.HasValue,
                    OperadorCondicao.NaoVazio => cartao.Prioridade.HasValue,
                    OperadorCondicao.MaiorOuIgual => cartao.Prioridade.HasValue && condicao.Ids.Count > 0 && (int)cartao.Prioridade.Value >= condicao.Ids[0],
                    OperadorCondicao.MenorOuIgual => cartao.Prioridade.HasValue && condicao.Ids.Count > 0 && (int)cartao.Prioridade.Value <= condicao.Ids[0],
                    _ => Conjunto(op, cartao.Prioridade.HasValue && condicao.Ids.Contains((int)cartao.Prioridade.Value), cartao.Prioridade.HasValue)
                },
                TipoCondicao.Etiqueta => Conjunto(op, cartao.Etiquetas.Any(e => condicao.Ids.Contains(e.EtiquetaId)), cartao.Etiquetas.Count > 0),
                TipoCondicao.Sistema => Conjunto(op, cartao.SistemaId.HasValue && condicao.Ids.Contains(cartao.SistemaId.Value), cartao.SistemaId.HasValue),
                TipoCondicao.Desenvolvedor => Conjunto(op, cartao.Desenvolvedores.Any(d => condicao.Ids.Contains(d.UsuarioId)), cartao.Desenvolvedores.Count > 0),
                TipoCondicao.Solicitante => Conjunto(op, cartao.SolicitanteId.HasValue && condicao.Ids.Contains(cartao.SolicitanteId.Value), cartao.SolicitanteId.HasValue),
                TipoCondicao.CampoPersonalizado => AvaliarCampo(condicao, cartao, foto),
                TipoCondicao.TemPrazo => SimNao(op, cartao.Prazo.HasValue),
                TipoCondicao.Atrasado => SimNao(op, cartao.Prazo.HasValue && cartao.Prazo.Value.ToLocalTime().Date < DateTime.Today && !foto.EstaConcluido(cartao)),
                TipoCondicao.Bloqueado => SimNao(op, foto.EstaBloqueado(cartao)),
                TipoCondicao.TemConflito => SimNao(op, foto.Conflitos(cartao).Count > 0),
                TipoCondicao.PrazoEmRisco => SimNao(op, foto.PrazoEmRisco(cartao, pontosPorDia)),
                TipoCondicao.ChecklistCompleto => SimNao(op, cartao.ItensTarefa.Count > 0 && cartao.ItensTarefa.All(i => i.Concluido)),
                TipoCondicao.DiasNaLista => Comparar(op, foto.DiasNaLista(cartao), condicao.Numero),
                TipoCondicao.Estimativa => op switch
                {
                    OperadorCondicao.Vazio => !cartao.Estimativa.HasValue,
                    OperadorCondicao.NaoVazio => cartao.Estimativa.HasValue,
                    _ => cartao.Estimativa.HasValue && Comparar(op, cartao.Estimativa.Value, condicao.Numero)
                },
                TipoCondicao.DiasParaPrazo => cartao.Prazo.HasValue
                    && Comparar(op, (cartao.Prazo.Value.ToLocalTime().Date - DateTime.Today).Days, condicao.Numero),
                TipoCondicao.Titulo => Texto(op, cartao.Titulo, condicao.Texto),
                _ => false
            };
        }

        // EstaEm/NaoEstaEm comparam com os IDs escolhidos; Vazio/NaoVazio olham se o cartão tem algum valor.
        private static bool Conjunto(OperadorCondicao op, bool contido, bool temValor) => op switch
        {
            OperadorCondicao.NaoEstaEm or OperadorCondicao.Diferente => !contido,
            OperadorCondicao.Vazio => !temValor,
            OperadorCondicao.NaoVazio => temValor,
            _ => contido
        };

        private static bool SimNao(OperadorCondicao op, bool valor) => op == OperadorCondicao.Nao ? !valor : valor;

        private static bool Comparar(OperadorCondicao op, decimal valor, decimal? referencia)
        {
            if (!referencia.HasValue) return false;
            return op switch
            {
                OperadorCondicao.MenorOuIgual => valor <= referencia,
                OperadorCondicao.Igual => valor == referencia,
                OperadorCondicao.Diferente => valor != referencia,
                _ => valor >= referencia
            };
        }

        private static bool Texto(OperadorCondicao op, string? valor, string? referencia)
        {
            valor ??= string.Empty;
            referencia ??= string.Empty;
            return op switch
            {
                OperadorCondicao.Vazio => string.IsNullOrWhiteSpace(valor),
                OperadorCondicao.NaoVazio => !string.IsNullOrWhiteSpace(valor),
                OperadorCondicao.Igual => valor.Trim().Equals(referencia.Trim(), StringComparison.OrdinalIgnoreCase),
                OperadorCondicao.Diferente => !valor.Trim().Equals(referencia.Trim(), StringComparison.OrdinalIgnoreCase),
                OperadorCondicao.NaoContem => !valor.Contains(referencia, StringComparison.OrdinalIgnoreCase),
                _ => valor.Contains(referencia, StringComparison.OrdinalIgnoreCase)
            };
        }

        private static bool AvaliarCampo(Condicao condicao, Cartao cartao, FotoQuadro foto)
        {
            var (definicao, valor) = foto.ValorCampo(cartao, condicao.CampoNome);
            if (definicao is null) return condicao.Operador == OperadorCondicao.Vazio;
            if (condicao.Operador is OperadorCondicao.MaiorOuIgual or OperadorCondicao.MenorOuIgual)
            {
                if (string.IsNullOrWhiteSpace(valor)) return false;
                if (definicao.Tipo == TipoCampo.Data && DateTime.TryParse(valor, out var data) && DateTime.TryParse(condicao.Texto, out var referenciaData))
                    return condicao.Operador == OperadorCondicao.MaiorOuIgual ? data.Date >= referenciaData.Date : data.Date <= referenciaData.Date;
                if (decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var numero))
                    return Comparar(condicao.Operador, numero, condicao.Numero);
                return false;
            }
            if (definicao.Tipo == TipoCampo.Checkbox && condicao.Operador is OperadorCondicao.Sim or OperadorCondicao.Nao)
                return SimNao(condicao.Operador, bool.TryParse(valor, out var marcado) && marcado);
            return Texto(condicao.Operador, valor, condicao.Texto);
        }
    }

    #endregion

    #region Services/Automacoes/ConflitosAgenda.cs

    /// <summary>
    /// Dois cartões conflitam quando compartilham algum desenvolvedor e os intervalos [início, prazo] se sobrepõem.
    /// Cartão sem início conta como um dia (o do prazo). Cartões concluídos não conflitam.
    /// </summary>
    public static class ConflitosAgenda
    {
        public static (DateTime Inicio, DateTime Fim)? Intervalo(Cartao cartao)
        {
            if (!cartao.Prazo.HasValue) return null;
            var fim = cartao.Prazo.Value.ToLocalTime().Date;
            var inicio = (cartao.DataInicio ?? cartao.Prazo.Value).ToLocalTime().Date;
            return (inicio <= fim ? inicio : fim, fim);
        }

        public static bool Sobrepoe((DateTime Inicio, DateTime Fim) a, (DateTime Inicio, DateTime Fim) b)
            => a.Inicio <= b.Fim && b.Inicio <= a.Fim;

        /// <summary>Cartões que conflitam com <paramref name="cartao"/>. Requer Desenvolvedores carregados.</summary>
        public static List<Cartao> Encontrar(Cartao cartao, IEnumerable<Cartao> outros, Func<Cartao, bool>? estaConcluido = null)
        {
            var intervalo = Intervalo(cartao);
            if (intervalo is null || cartao.Desenvolvedores.Count == 0) return [];
            if (estaConcluido?.Invoke(cartao) == true) return [];
            var devs = cartao.Desenvolvedores.Select(d => d.UsuarioId).ToHashSet();
            return outros
                .Where(o => o.Id != cartao.Id && !o.Excluido && estaConcluido?.Invoke(o) != true)
                .Where(o => devs.Overlaps(o.Desenvolvedores.Select(d => d.UsuarioId)))
                .Where(o => Intervalo(o) is { } outro && Sobrepoe(intervalo.Value, outro))
                .ToList();
        }
    }

    #endregion

    #region Services/Automacoes/DescricaoAutomacao.cs

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

    #endregion

    #region Services/Automacoes/DiasUteis.cs

    /// <summary>Contas com dias úteis: segunda a sexta, descontando os feriados informados (datas sem hora).</summary>
    public static class DiasUteis
    {
        public static bool EhUtil(DateTime data, IReadOnlySet<DateTime>? feriados = null)
            => data.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && feriados?.Contains(data.Date) != true;

        public static DateTime ProximoUtil(DateTime data, IReadOnlySet<DateTime>? feriados = null)
        {
            var dia = data.Date;
            while (!EhUtil(dia, feriados)) dia = dia.AddDays(1);
            return dia;
        }

        /// <summary>Soma (ou subtrai) dias úteis; 0 devolve o próprio dia (ajustado para dia útil).</summary>
        public static DateTime Adicionar(DateTime data, int dias, IReadOnlySet<DateTime>? feriados = null)
        {
            var dia = data.Date;
            var passo = dias >= 0 ? 1 : -1;
            var restantes = Math.Abs(dias);
            while (restantes > 0)
            {
                dia = dia.AddDays(passo);
                if (EhUtil(dia, feriados)) restantes--;
            }
            return EhUtil(dia, feriados) ? dia : ProximoUtil(dia, feriados);
        }

        /// <summary>Quantidade de dias úteis no intervalo fechado [inicio, fim]; mínimo 1.</summary>
        public static int Contar(DateTime inicio, DateTime fim, IReadOnlySet<DateTime>? feriados = null)
        {
            if (fim.Date < inicio.Date) return 1;
            var total = 0;
            for (var dia = inicio.Date; dia <= fim.Date; dia = dia.AddDays(1))
                if (EhUtil(dia, feriados)) total++;
            return Math.Max(1, total);
        }
    }

    #endregion

    #region Services/Automacoes/FotoQuadro.cs

    /// <summary>
    /// Retrato somente leitura de um quadro com tudo o que condições e gatilhos precisam avaliar
    /// (listas, cartões, responsáveis, campos, relações e quando cada cartão entrou na lista atual).
    /// </summary>
    public sealed class FotoQuadro
    {
        public int QuadroId { get; init; }
        public List<Lista> Listas { get; init; } = [];
        public List<Cartao> Cartoes { get; init; } = [];
        public List<Usuario> Usuarios { get; init; } = [];
        public List<Sprint> Sprints { get; init; } = [];
        public List<Etiqueta> Etiquetas { get; init; } = [];
        public List<Sistema> Sistemas { get; init; } = [];
        public Dictionary<int, DateTime> EntradaNaListaAtual { get; init; } = [];

        /// <summary>Datas não úteis (vazio quando o parâmetro "considerar feriados" está desligado).</summary>
        public IReadOnlySet<DateTime> Feriados { get; init; } = new HashSet<DateTime>();

        public static async Task<FotoQuadro> CarregarAsync(KanbanDbContext db, int quadroId, bool considerarFeriados = true)
        {
            var listas = await db.Listas.AsNoTracking().Include(l => l.DefinicoesCampo.Where(d => !d.Excluido))
                .Where(l => l.QuadroId == quadroId && !l.Excluido).ToListAsync();
            var idsListas = listas.Select(l => l.Id).ToList();
            var cartoes = await db.Cartoes.AsNoTracking().AsSplitQuery()
                .Include(c => c.Sistema)
                .Include(c => c.Desenvolvedores)
                .Include(c => c.Etiquetas)
                .Include(c => c.ValoresCampo)
                .Include(c => c.ItensTarefa.Where(i => !i.Excluido))
                .Include(c => c.RelacoesOrigem).ThenInclude(r => r.CartaoDestino)
                .Include(c => c.RelacoesDestino).ThenInclude(r => r.CartaoOrigem)
                .Where(c => idsListas.Contains(c.ListaId) && !c.Excluido)
                .ToListAsync();
            var idsCartoes = cartoes.Select(c => c.Id).ToList();

            // Estornos já ficam fora pelo filtro global do histórico.
            var entradas = await db.HistoricoAtividades.AsNoTracking()
                .Where(h => idsCartoes.Contains(h.CartaoId) && h.ListaDestinoId != null
                    && (h.Tipo == TipoHistoricoAtividade.CartaoMovido || h.Tipo == TipoHistoricoAtividade.CartaoCriado))
                .GroupBy(h => new { h.CartaoId, h.ListaDestinoId })
                .Select(g => new { g.Key.CartaoId, g.Key.ListaDestinoId, Ultima = g.Max(h => h.OcorridoEm) })
                .ToListAsync();
            var entradaAtual = cartoes.ToDictionary(c => c.Id,
                c => entradas.FirstOrDefault(e => e.CartaoId == c.Id && e.ListaDestinoId == c.ListaId)?.Ultima ?? c.CriadoEm);

            return new FotoQuadro
            {
                QuadroId = quadroId,
                Listas = listas,
                Cartoes = cartoes,
                Usuarios = await db.Usuarios.AsNoTracking().Where(u => u.Ativo && !u.Excluido).OrderBy(u => u.Nome).ToListAsync(),
                Sprints = await db.Sprints.AsNoTracking().Include(s => s.Cartoes).Where(s => s.QuadroId == quadroId && !s.Excluido).ToListAsync(),
                EntradaNaListaAtual = entradaAtual,
                Feriados = considerarFeriados ? await FeriadosService.CarregarDatasAsync(db, DateTime.Today.Year - 2, DateTime.Today.Year + 3) : new HashSet<DateTime>(),
                Etiquetas = await db.Etiquetas.AsNoTracking().Where(e => e.QuadroId == quadroId && !e.Excluido).OrderBy(e => e.Nome).ToListAsync(),
                Sistemas = await db.Sistemas.AsNoTracking().Where(s => s.Ativo).OrderBy(s => s.Nome).ToListAsync()
            };
        }

        public Cartao? Cartao(int id) => Cartoes.FirstOrDefault(c => c.Id == id);
        public Lista? Lista(int id) => Listas.FirstOrDefault(l => l.Id == id);
        public string NomeLista(int id) => Lista(id) is { } lista ? ListasQuadro.NomeCompleto(lista, Listas) : "";
        public string? NomeUsuario(int? id) => Usuarios.FirstOrDefault(u => u.Id == id)?.Nome;

        public bool EstaConcluido(Cartao cartao) => ListasQuadro.EhConcluido(cartao.ListaId, Listas);

        public int DiasNaLista(Cartao cartao)
            => EntradaNaListaAtual.TryGetValue(cartao.Id, out var entrada) ? (DateTime.Today - entrada.ToLocalTime().Date).Days : 0;

        public List<Cartao> Conflitos(Cartao cartao) => ConflitosAgenda.Encontrar(cartao, Cartoes, EstaConcluido);

        public bool EstaBloqueado(Cartao cartao)
            => cartao.RelacoesOrigem.Any(r => r.Tipo == TipoRelacaoCartao.BloqueadoPor && !ListasQuadro.EhConcluido(r.CartaoDestino.ListaId, Listas))
            || cartao.RelacoesDestino.Any(r => r.Tipo == TipoRelacaoCartao.Bloqueia && !ListasQuadro.EhConcluido(r.CartaoOrigem.ListaId, Listas));

        /// <summary>A lista do cartão ou alguma das listas-pai dela está no conjunto (condição por grupo).</summary>
        public bool ListaOuPaiEm(int listaId, IReadOnlyCollection<int> ids)
        {
            for (var atual = Lista(listaId); atual is not null; atual = atual.ListaPaiId.HasValue ? Lista(atual.ListaPaiId.Value) : null)
                if (ids.Contains(atual.Id)) return true;
            return false;
        }

        /// <summary>Valor mais recente de um campo personalizado pelo nome, preferindo a definição da lista atual.</summary>
        public (DefinicaoCampo? Definicao, string? Valor) ValorCampo(Cartao cartao, string? nome)
        {
            if (string.IsNullOrWhiteSpace(nome)) return (null, null);
            var definicoes = Listas.SelectMany(l => l.DefinicoesCampo)
                .Where(d => d.Nome.Trim().Equals(nome.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            var daLista = definicoes.FirstOrDefault(d => d.ListaId == cartao.ListaId);
            if (daLista is not null)
            {
                var valor = CamposCartao.ValorMaisRecente(cartao.ValoresCampo.Where(v => !v.Excluido), daLista.Id);
                if (!string.IsNullOrWhiteSpace(valor)) return (daLista, valor);
            }
            var ids = definicoes.Select(d => d.Id).ToHashSet();
            var ultimo = cartao.ValoresCampo.Where(v => !v.Excluido && ids.Contains(v.DefinicaoCampoId) && !string.IsNullOrWhiteSpace(v.Valor))
                .OrderByDescending(v => v.DataPreenchimento).FirstOrDefault();
            return ultimo is null ? (daLista ?? definicoes.FirstOrDefault(), null) : (definicoes.First(d => d.Id == ultimo.DefinicaoCampoId), ultimo.Valor);
        }

        /// <summary>A estimativa (em dias úteis, via pontos por dia) não cabe nos dias úteis até o prazo.</summary>
        public bool PrazoEmRisco(Cartao cartao, decimal pontosPorDia)
        {
            if (!cartao.Prazo.HasValue || !cartao.Estimativa.HasValue || cartao.Estimativa <= 0 || EstaConcluido(cartao)) return false;
            var prazo = cartao.Prazo.Value.ToLocalTime().Date;
            if (prazo < DateTime.Today) return true;
            var inicio = cartao.DataInicio?.ToLocalTime().Date is { } dataInicio && dataInicio > DateTime.Today ? dataInicio : DateTime.Today;
            var necessarios = (int)Math.Ceiling(cartao.Estimativa.Value / Math.Max(0.1m, pontosPorDia));
            return necessarios > DiasUteis.Contar(inicio, prazo, Feriados);
        }
    }

    #endregion

    #region Services/Automacoes/MarcadoresAutomacao.cs

    /// <summary>Endereço público da aplicação, para links em e-mails gerados fora de uma requisição.</summary>
    public sealed class UrlBaseAplicacao(IConfiguration configuracao)
    {
        private string? _detectada;

        public string Valor
        {
            get
            {
                var configurada = configuracao["Aplicacao:UrlBase"]?.TrimEnd('/');
                return string.IsNullOrWhiteSpace(configurada) ? _detectada ?? "" : configurada;
            }
        }

        /// <summary>Guarda o endereço da primeira requisição recebida quando nenhum foi configurado.</summary>
        public void Detectar(HttpRequest requisicao) => _detectada ??= $"{requisicao.Scheme}://{requisicao.Host}{requisicao.PathBase}";

        public string LinkCartao(int quadroId, int cartaoId) => $"{Valor}/quadros/{quadroId}?cartao={cartaoId}";
    }

    /// <summary>Substitui marcadores como {titulo} e {prazo} pelos dados do cartão.</summary>
    public static class MarcadoresAutomacao
    {
        public static readonly (string Marcador, string Descricao)[] Disponiveis =
        [
            ("{codigo}", "código do cartão (ex.: KB-123)"), ("{titulo}", "título do cartão"), ("{lista}", "lista atual"), ("{prazo}", "prazo"), ("{inicio}", "data de início"),
            ("{prioridade}", "prioridade"), ("{desenvolvedores}", "desenvolvedores"), ("{solicitante}", "solicitante"),
            ("{sistema}", "sistema"), ("{dias_na_lista}", "dias na lista atual"), ("{conflitos}", "cartões em conflito"),
            ("{hoje}", "data de hoje"), ("{regra}", "nome da regra"), ("{link}", "link para o cartão")
        ];

        public static string Aplicar(string? texto, Cartao? cartao, FotoQuadro foto, string nomeRegra, string link, bool html, string? prefixoCodigo = null)
        {
            if (string.IsNullOrEmpty(texto)) return string.Empty;
            string V(string? valor) => html ? WebUtility.HtmlEncode(valor ?? "") : valor ?? "";
            var resultado = html ? WebUtility.HtmlEncode(texto).Replace("\n", "<br>", StringComparison.Ordinal) : texto;

            resultado = resultado
                .Replace("{hoje}", V(DateTime.Today.ToString("dd/MM/yyyy")), StringComparison.OrdinalIgnoreCase)
                .Replace("{regra}", V(nomeRegra), StringComparison.OrdinalIgnoreCase);
            if (cartao is null) return resultado;

            var linkFormatado = html ? $"<a href=\"{WebUtility.HtmlEncode(link)}\">{WebUtility.HtmlEncode(link)}</a>" : link;
            var desenvolvedores = string.Join(", ", cartao.Desenvolvedores.OrderByDescending(d => d.Principal).Select(d => foto.NomeUsuario(d.UsuarioId)).OfType<string>());
            var conflitos = string.Join(", ", foto.Conflitos(cartao).Select(c => c.Titulo));
            var sistema = cartao.Sistema?.Nome;

            return resultado
                .Replace("{codigo}", V(CodigoCartao.Formatar(cartao.Id, prefixoCodigo)), StringComparison.OrdinalIgnoreCase)
                .Replace("{titulo}", V(cartao.Titulo), StringComparison.OrdinalIgnoreCase)
                .Replace("{lista}", V(foto.NomeLista(cartao.ListaId)), StringComparison.OrdinalIgnoreCase)
                .Replace("{prazo}", V(cartao.Prazo?.ToLocalTime().ToString("dd/MM/yyyy") ?? "sem prazo"), StringComparison.OrdinalIgnoreCase)
                .Replace("{inicio}", V(cartao.DataInicio?.ToLocalTime().ToString("dd/MM/yyyy") ?? "não iniciado"), StringComparison.OrdinalIgnoreCase)
                .Replace("{prioridade}", V(cartao.Prioridade?.ToString() ?? "sem prioridade"), StringComparison.OrdinalIgnoreCase)
                .Replace("{desenvolvedores}", V(desenvolvedores.Length > 0 ? desenvolvedores : "ninguém"), StringComparison.OrdinalIgnoreCase)
                .Replace("{solicitante}", V(foto.NomeUsuario(cartao.SolicitanteId) ?? "sem solicitante"), StringComparison.OrdinalIgnoreCase)
                .Replace("{sistema}", V(sistema ?? "sem sistema"), StringComparison.OrdinalIgnoreCase)
                .Replace("{dias_na_lista}", V(foto.DiasNaLista(cartao).ToString()), StringComparison.OrdinalIgnoreCase)
                .Replace("{conflitos}", V(conflitos.Length > 0 ? conflitos : "nenhum"), StringComparison.OrdinalIgnoreCase)
                .Replace("{link}", linkFormatado, StringComparison.OrdinalIgnoreCase);
        }
    }

    #endregion

    #region Services/Automacoes/MensagemTeams.cs

    /// <summary>
    /// Envio de mensagens para canais do Teams via webhook (fluxo "Postar em um canal quando uma solicitação de
    /// webhook for recebida" do app Workflows, ou o antigo conector "Incoming Webhook"). Não usa Entra ID:
    /// quem tem a URL posta no canal. A mensagem é um Adaptive Card, formato aceito pelos dois tipos de webhook.
    /// </summary>
    public sealed class MensagemTeams(IHttpClientFactory httpClientFactory)
    {
        public sealed record Fato(string Titulo, string Valor);

        public static bool UrlValida(string? url)
            => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;

        public static object Montar(string titulo, string? texto, IEnumerable<Fato> fatos, string? link, string? textoBotao, IEnumerable<string>? itens = null)
        {
            var corpo = new List<object>
            {
                new { type = "TextBlock", text = titulo, weight = "Bolder", size = "Medium", wrap = true }
            };
            if (!string.IsNullOrWhiteSpace(texto)) corpo.Add(new { type = "TextBlock", text = texto, wrap = true });
            var listaFatos = fatos.Where(f => !string.IsNullOrWhiteSpace(f.Valor)).Select(f => new { title = f.Titulo, value = f.Valor }).ToList();
            if (listaFatos.Count > 0) corpo.Add(new { type = "FactSet", facts = listaFatos });
            foreach (var item in itens ?? []) corpo.Add(new { type = "TextBlock", text = item, wrap = true, spacing = "None" });

            var cartao = new Dictionary<string, object>
            {
                ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
                ["type"] = "AdaptiveCard",
                ["version"] = "1.4",
                ["body"] = corpo
            };
            if (!string.IsNullOrWhiteSpace(link) && Uri.TryCreate(link, UriKind.Absolute, out _))
                cartao["actions"] = new[] { new { type = "Action.OpenUrl", title = textoBotao ?? "Abrir", url = link } };

            return new
            {
                type = "message",
                attachments = new[] { new { contentType = "application/vnd.microsoft.card.adaptive", contentUrl = (string?)null, content = cartao } }
            };
        }

        public async Task EnviarAsync(string url, object mensagem, CancellationToken ct = default)
        {
            if (!UrlValida(url)) throw new InvalidOperationException("a URL do webhook do Teams deve começar com https://.");
            var cliente = httpClientFactory.CreateClient("Teams");
            using var resposta = await cliente.PostAsJsonAsync(url, mensagem, new JsonSerializerOptions(), ct);
            if (!resposta.IsSuccessStatusCode)
            {
                var detalhe = await resposta.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"o Teams recusou a mensagem ({(int)resposta.StatusCode}): {(detalhe.Length > 200 ? detalhe[..200] : detalhe)}");
            }
        }
    }

    #endregion

    #region Services/Automacoes/ModelosAutomacao.cs

    public sealed class ParametrosGatilho
    {
        public CampoMonitorado Campo { get; set; } = CampoMonitorado.Qualquer;

        /// <summary>Prazo próximo: dias de antecedência. Parado: dias na lista. Sprint encerrando: dias antes do fim. Vencido: repetir a cada N dias (0 = uma vez).</summary>
        public int Dias { get; set; } = 2;

        /// <summary>Agendado: horário no formato HH:mm.</summary>
        public string Horario { get; set; } = "08:00";

        /// <summary>Agendado: dias da semana; vazio = todos os dias.</summary>
        public List<DayOfWeek> DiasSemana { get; set; } = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
    }

    public sealed class Condicao
    {
        public TipoCondicao Tipo { get; set; } = TipoCondicao.Lista;
        public OperadorCondicao Operador { get; set; } = OperadorCondicao.EstaEm;

        /// <summary>IDs de listas, etiquetas, sistemas, usuários ou valores de prioridade.</summary>
        public List<int> Ids { get; set; } = [];

        /// <summary>Campos personalizados são definidos por lista; a condição os identifica pelo nome.</summary>
        public string? CampoNome { get; set; }
        public string? Texto { get; set; }
        public decimal? Numero { get; set; }
    }

    public sealed class ParametrosAcao
    {
        public int? ListaId { get; set; }
        public bool NoTopo { get; set; }

        public ModoAtribuicao ModoAtribuicao { get; set; } = ModoAtribuicao.MenorCarga;
        public int? UsuarioId { get; set; }

        /// <summary>Candidatos para menor carga/revezamento; vazio = todos os usuários ativos.</summary>
        public List<int> UsuarioIds { get; set; } = [];
        public bool SubstituirAtuais { get; set; }

        public int? EtiquetaId { get; set; }

        public Prioridade? Prioridade { get; set; }
        public bool AumentarUmNivel { get; set; }

        public BaseDataAutomacao BaseData { get; set; } = BaseDataAutomacao.Hoje;
        public int Dias { get; set; }
        public bool DiasUteis { get; set; } = true;
        public bool AplicarEmDataInicio { get; set; }

        public string? CampoNome { get; set; }
        public string? Valor { get; set; }
        public bool SomenteSeVazio { get; set; } = true;

        public List<string> Itens { get; set; } = [];
        public string? Titulo { get; set; }

        public string? Texto { get; set; }
        public string? Assunto { get; set; }
        public DestinatariosAutomacao Destinatarios { get; set; } = DestinatariosAutomacao.Desenvolvedores;
        public List<int> DestinatarioIds { get; set; } = [];
        public string? EmailsFixos { get; set; }

        /// <summary>URL do webhook do canal do Teams (gerada pelo app Workflows no canal).</summary>
        public string? UrlWebhook { get; set; }

        public SeveridadeAlerta Severidade { get; set; } = SeveridadeAlerta.Aviso;

        public EstrategiaConflito Estrategia { get; set; } = EstrategiaConflito.MaisRecente;
        public ResolucaoConflito Resolucao { get; set; } = ResolucaoConflito.EmpurrarDatas;
        public ModoAplicacao Modo { get; set; } = ModoAplicacao.Sugerir;

        /// <summary>Resolver alertas: só os criados por esta regra (padrão) ou todos os alertas abertos do cartão.</summary>
        public bool TodosAlertasDoCartao { get; set; }
    }

    /// <summary>Mudança proposta para resolver um conflito de datas (aplicada direto ou via sugestão).</summary>
    public sealed class PropostaConflito
    {
        public DateTime? NovaDataInicio { get; set; }
        public DateTime? NovoPrazo { get; set; }
        public int? ListaId { get; set; }
        public List<int> ConflitaCom { get; set; } = [];
    }

    /// <summary>Operação inversa de uma alteração feita por automação.</summary>
    public sealed class OperacaoDesfazer
    {
        public string Tipo { get; set; } = string.Empty;
        public int CartaoId { get; set; }
        public int? Id { get; set; }
        public string? ValorAnterior { get; set; }
        public bool Flag { get; set; }

        public static class Tipos
        {
            public const string Lista = "lista";
            public const string Prioridade = "prioridade";
            public const string Prazo = "prazo";
            public const string DataInicio = "inicio";
            public const string DesenvolvedorAdicionado = "dev+";
            public const string DesenvolvedorRemovido = "dev-";
            public const string EtiquetaAdicionada = "etiqueta+";
            public const string EtiquetaRemovida = "etiqueta-";
            public const string ValorCampo = "campo";
            public const string AlertaCriado = "alerta+";
            public const string AlertaResolvido = "alerta-";
            public const string ItemCriado = "item+";
            public const string CartaoCriado = "cartao+";
            public const string ComentarioCriado = "comentario+";
            public const string SugestaoCriada = "sugestao+";
        }
    }

    public static class JsonAutomacao
    {
        public static readonly JsonSerializerOptions Opcoes = new()
        {
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static string Serializar<T>(T valor) => JsonSerializer.Serialize(valor, Opcoes);

        public static T Ler<T>(string? json) where T : new()
        {
            if (string.IsNullOrWhiteSpace(json)) return new T();
            try { return JsonSerializer.Deserialize<T>(json, Opcoes) ?? new T(); }
            catch (JsonException) { return new T(); }
        }
    }

    #endregion

    #region Services/Automacoes/ModelosProntosAutomacao.cs

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

    #endregion

    #region Services/Automacoes/MotorAutomacoes.cs

    public sealed record AlvoSimulacao(int? CartaoId, string Titulo, string Situacao, IReadOnlyList<string> Acoes);

    /// <summary>
    /// Executa regras de automação: QUANDO (evento ou tempo) + SE (condições) → ENTÃO (ações, na ordem).
    /// Cada execução fica registrada com o que mudou e as operações para desfazer.
    /// </summary>
    public sealed class MotorAutomacoes(
        KanbanDbContext db,
        MovimentacaoCartaoService movimentacao,
        IEmailSender emailSender,
        IConfiguration configuracao,
        UrlBaseAplicacao urlBase,
        MensagemTeams teams,
        KanbanDemandas.Web.Services.Portal.NotificadorSolicitante notificadorSolicitante,
        ILogger<MotorAutomacoes> logger)
    {
        private int MaxProfundidade => configuracao.GetValue("Automacao:ProfundidadeMaxima", 3);
        private decimal PontosPorDia => configuracao.GetValue("Automacao:PontosPorDia", 1m);
        private int LimiteEmailsPorHora => configuracao.GetValue("Automacao:LimiteEmailsPorRegraPorHora", 50);
        private int UsuarioSistemaId => configuracao.GetValue("Automacao:UsuarioSistemaId", 1);
        private int LimiteTeamsPorHora => configuracao.GetValue("Automacao:LimiteTeamsPorRegraPorHora", 100);
        private string? PrefixoCodigo => configuracao["Cartao:PrefixoCodigo"];
        private bool ConsiderarFeriados => configuracao.GetValue("Calendario:ConsiderarFeriados", true);

        private Task<FotoQuadro> CarregarFotoAsync(int quadroId) => FotoQuadro.CarregarAsync(db, quadroId, ConsiderarFeriados);

        private sealed class Contexto
        {
            public required RegraAutomacao Regra { get; init; }
            public required FotoQuadro Foto { get; set; }
            public int? CartaoId { get; init; }
            public int UsuarioId { get; init; }
            public EventoAutomacao? Evento { get; init; }
            public bool Simular { get; init; }
            public List<OperacaoDesfazer> Desfazer { get; } = [];
            public int EmailsEnviados { get; set; }
            public int MensagensTeams { get; set; }
        }

        // ───────────────────────── Eventos ─────────────────────────

        public async Task ProcessarEventoAsync(EventoAutomacao evento)
        {
            if (evento.Profundidade > MaxProfundidade)
            {
                logger.LogWarning("Evento {Gatilho} do cartão {Cartao} ignorado: cadeia de automações passou de {Max} níveis.", evento.Gatilho, evento.CartaoId, MaxProfundidade);
                return;
            }
            var quadroId = await db.Cartoes.Where(c => c.Id == evento.CartaoId && !c.Excluido).Select(c => (int?)c.Lista.QuadroId).FirstOrDefaultAsync();
            if (quadroId is null) return;
            if (evento.Gatilho == TipoGatilho.CartaoEntrouNaLista && evento.Profundidade == 0)
                await notificadorSolicitante.AoEntrarNaListaAsync(evento);

            var regras = await db.RegrasAutomacao.AsNoTracking().Include(r => r.Acoes)
                .Where(r => r.QuadroId == quadroId && r.Ativa && r.Gatilho == evento.Gatilho).ToListAsync();
            // Uma regra não é disparada de novo pela própria cadeia que ela iniciou.
            regras = regras.Where(r => !evento.RegrasNaCadeia.Contains(r.Id)).ToList();
            if (regras.Count == 0) return;

            var foto = await CarregarFotoAsync(quadroId.Value);
            var cartao = foto.Cartao(evento.CartaoId);
            if (cartao is null) return;

            // O evento pode ter esperado minutos na fila: só executa se a situação que o gerou ainda existe
            // (ex.: o cartão continua na lista em que entrou — um vai-e-volta dentro da espera não dispara nada).
            var ocorreu = evento.Gatilho switch
            {
                TipoGatilho.CartaoEntrouNaLista => evento.ListaId.HasValue && foto.ListaOuPaiEm(cartao.ListaId, [evento.ListaId.Value]),
                TipoGatilho.CartaoSaiuDaLista => evento.ListaId.HasValue && !foto.ListaOuPaiEm(cartao.ListaId, [evento.ListaId.Value]),
                TipoGatilho.CartaoAtribuido => evento.UsuarioAlvoId is null || cartao.Desenvolvedores.Any(d => d.UsuarioId == evento.UsuarioAlvoId),
                TipoGatilho.ChecklistConcluido => cartao.ItensTarefa.Count > 0 && cartao.ItensTarefa.All(i => i.Concluido),
                TipoGatilho.ConflitoDatas => foto.Conflitos(cartao).Count > 0,
                TipoGatilho.CartaoBloqueado => foto.EstaBloqueado(cartao),
                _ => true
            };
            if (!ocorreu) return;

            foreach (var regra in regras.Where(r => GatilhoCombina(r, evento, foto)))
            {
                if (!Atende(regra, cartao, foto)) continue;
                await ExecutarAsync(regra, foto, cartao.Id, evento.UsuarioId, null, evento, evento.Profundidade, evento.RegrasNaCadeia);
                foto = await CarregarFotoAsync(quadroId.Value);
            }
        }

        private static bool GatilhoCombina(RegraAutomacao regra, EventoAutomacao evento, FotoQuadro foto) => regra.Gatilho switch
        {
            TipoGatilho.CartaoEntrouNaLista or TipoGatilho.CartaoSaiuDaLista
                => regra.ListaId is null || evento.ListaId.HasValue && foto.ListaOuPaiEm(evento.ListaId.Value, [regra.ListaId.Value]),
            TipoGatilho.CampoAlterado => JsonAutomacao.Ler<ParametrosGatilho>(regra.ParametrosGatilhoJson).Campo is var campo
                && (campo == CampoMonitorado.Qualquer || campo == evento.Campo),
            _ => true
        };

        private bool Atende(RegraAutomacao regra, Cartao cartao, FotoQuadro foto)
            => AvaliadorCondicoes.Atende(JsonAutomacao.Ler<List<Condicao>>(regra.CondicoesJson), regra.ExigirTodasCondicoes, cartao, foto, PontosPorDia);

        // ───────────────────────── Tempo ─────────────────────────

        /// <summary>Verifica gatilhos de tempo de todas as regras ativas; cada disparo acontece uma única vez (chave de disparo).</summary>
        public async Task ProcessarGatilhosDeTempoAsync(DateTime agora)
        {
            var regras = await db.RegrasAutomacao.AsNoTracking().Include(r => r.Acoes)
                .Where(r => r.Ativa && (int)r.Gatilho >= 20).ToListAsync();
            if (regras.Count == 0) return;
            var feriados = ConsiderarFeriados ? await FeriadosService.CarregarDatasAsync(db, agora.Year, agora.Year) : [];
            foreach (var grupo in regras.GroupBy(r => r.QuadroId))
            {
                var foto = await CarregarFotoAsync(grupo.Key);
                foreach (var regra in grupo)
                {
                    if (regra.SomenteHorarioComercial && !DentroDoHorarioComercial(agora, feriados)) continue;
                    try
                    {
                        await ProcessarRegraDeTempoAsync(regra, foto, agora);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Falha ao verificar a regra de automação {Regra}.", regra.Id);
                    }
                }
            }
        }

        public bool DentroDoHorarioComercial(DateTime agora, IReadOnlySet<DateTime>? feriados = null)
        {
            var inicio = TimeSpan.TryParse(configuracao["Automacao:HorarioComercialInicio"], out var i) ? i : new TimeSpan(8, 0, 0);
            var fim = TimeSpan.TryParse(configuracao["Automacao:HorarioComercialFim"], out var f) ? f : new TimeSpan(18, 0, 0);
            return DiasUteis.EhUtil(agora, feriados) && agora.TimeOfDay >= inicio && agora.TimeOfDay <= fim;
        }

        private async Task ProcessarRegraDeTempoAsync(RegraAutomacao regra, FotoQuadro foto, DateTime agora)
        {
            var executadas = (await db.ExecucoesAutomacao.AsNoTracking()
                    .Where(e => e.RegraAutomacaoId == regra.Id && e.ChaveDisparo != null)
                    .Select(e => new { e.CartaoId, e.ChaveDisparo }).ToListAsync())
                .Select(e => (e.CartaoId, e.ChaveDisparo!)).ToHashSet();

            if (DescricaoAutomacao.EhGatilhoDeQuadro(regra.Gatilho))
            {
                foreach (var (chave, cartoes) in DisparosDeQuadro(regra, foto, agora))
                {
                    if (executadas.Contains((null, chave))) continue;
                    await ExecutarEmQuadroAsync(regra, foto, cartoes, UsuarioSistemaId, chave);
                }
                return;
            }

            foreach (var (cartao, chave) in DisparosPorCartao(regra, foto, agora).ToList())
            {
                if (executadas.Contains((cartao.Id, chave)) || !Atende(regra, cartao, foto)) continue;
                await ExecutarAsync(regra, foto, cartao.Id, UsuarioSistemaId, chave, null, 0, []);
            }
        }

        private static IEnumerable<(Cartao Cartao, string Chave)> DisparosPorCartao(RegraAutomacao regra, FotoQuadro foto, DateTime agora)
        {
            var p = JsonAutomacao.Ler<ParametrosGatilho>(regra.ParametrosGatilhoJson);
            var hoje = agora.Date;
            foreach (var cartao in foto.Cartoes)
            {
                switch (regra.Gatilho)
                {
                    case TipoGatilho.PrazoProximo when cartao.Prazo.HasValue && !foto.EstaConcluido(cartao):
                        var prazo = cartao.Prazo.Value.ToLocalTime().Date;
                        var faltam = (prazo - hoje).Days;
                        if (faltam >= 0 && faltam <= p.Dias) yield return (cartao, $"prazo-proximo:{prazo:yyyyMMdd}");
                        break;
                    case TipoGatilho.PrazoVencido when cartao.Prazo.HasValue && !foto.EstaConcluido(cartao):
                        var vencimento = cartao.Prazo.Value.ToLocalTime().Date;
                        var atraso = (hoje - vencimento).Days;
                        if (atraso > 0)
                            yield return (cartao, p.Dias > 0 ? $"vencido:{vencimento:yyyyMMdd}:{(atraso - 1) / p.Dias}" : $"vencido:{vencimento:yyyyMMdd}");
                        break;
                    case TipoGatilho.ParadoNaLista:
                        var naLista = regra.ListaId.HasValue ? foto.ListaOuPaiEm(cartao.ListaId, [regra.ListaId.Value]) : !foto.EstaConcluido(cartao);
                        if (naLista && foto.DiasNaLista(cartao) >= p.Dias && foto.EntradaNaListaAtual.TryGetValue(cartao.Id, out var entrada))
                            yield return (cartao, $"parado:{cartao.ListaId}:{entrada:yyyyMMddHHmmss}");
                        break;
                }
            }
        }

        private IEnumerable<(string Chave, List<Cartao> Cartoes)> DisparosDeQuadro(RegraAutomacao regra, FotoQuadro foto, DateTime agora)
        {
            var p = JsonAutomacao.Ler<ParametrosGatilho>(regra.ParametrosGatilhoJson);
            var hoje = agora.Date;
            List<Cartao> Elegiveis(IEnumerable<Cartao> cartoes) => cartoes.Where(c => !foto.EstaConcluido(c) && Atende(regra, c, foto)).ToList();

            switch (regra.Gatilho)
            {
                case TipoGatilho.Agendado:
                    var horario = TimeSpan.TryParse(p.Horario, out var h) ? h : new TimeSpan(8, 0, 0);
                    if (agora.TimeOfDay >= horario && (p.DiasSemana.Count == 0 || p.DiasSemana.Contains(agora.DayOfWeek)))
                        yield return ($"agendado:{hoje:yyyyMMdd}", Elegiveis(foto.Cartoes));
                    break;
                case TipoGatilho.SprintIniciada:
                    foreach (var sprint in foto.Sprints.Where(s => !s.Fechada && s.DataInicio.Date <= hoje && s.DataFim.Date >= hoje))
                        yield return ($"sprint:{sprint.Id}:inicio", Elegiveis(CartoesDaSprint(sprint, foto)));
                    break;
                case TipoGatilho.SprintEncerrando:
                    foreach (var sprint in foto.Sprints.Where(s => !s.Fechada && (s.DataFim.Date - hoje).Days is var dias && dias >= 0 && dias <= p.Dias))
                        yield return ($"sprint:{sprint.Id}:fim", Elegiveis(CartoesDaSprint(sprint, foto)));
                    break;
            }
        }

        private static IEnumerable<Cartao> CartoesDaSprint(Sprint sprint, FotoQuadro foto)
            => sprint.Cartoes.Select(sc => foto.Cartao(sc.CartaoId)).OfType<Cartao>();

        /// <summary>Gatilho de quadro: ações de quadro rodam uma vez; ações de cartão, em cada cartão elegível.</summary>
        private async Task ExecutarEmQuadroAsync(RegraAutomacao regra, FotoQuadro foto, List<Cartao> cartoes, int usuarioId, string? chave)
        {
            if (regra.Acoes.Any(a => !a.Excluido && !DescricaoAutomacao.EhAcaoDeQuadro(a.Tipo)))
            {
                foreach (var cartao in cartoes)
                {
                    await ExecutarAsync(regra, foto, cartao.Id, usuarioId, chave, null, 0, [], somenteAcoesDeCartao: true);
                    foto = await CarregarFotoAsync(foto.QuadroId);
                }
            }
            await ExecutarAsync(regra, foto, null, usuarioId, chave, null, 0, [], somenteAcoesDeCartao: false,
                resumoExtra: $"{cartoes.Count} cartão(ões) atenderam às condições.");
        }

        // ───────────────────────── Simulação e execução manual ─────────────────────────

        /// <summary>Mostra quais cartões seriam afetados agora e o que aconteceria com cada um, sem alterar nada.</summary>
        public async Task<List<AlvoSimulacao>> SimularAsync(int regraId)
        {
            var regra = await db.RegrasAutomacao.AsNoTracking().Include(r => r.Acoes).FirstAsync(r => r.Id == regraId);
            var foto = await CarregarFotoAsync(regra.QuadroId);
            var acoes = regra.Acoes.Where(a => !a.Excluido).OrderBy(a => a.Ordem).ToList();
            var resultado = new List<AlvoSimulacao>();

            foreach (var cartao in AlvosDeCartao(regra, foto))
            {
                var situacao = SituacaoDoGatilho(regra, cartao, foto);
                resultado.Add(new AlvoSimulacao(cartao.Id, cartao.Titulo, situacao,
                    acoes.Where(a => !DescricaoAutomacao.EhAcaoDeQuadro(a.Tipo))
                        .Select(a => DescricaoAutomacao.Acao(a.Tipo, JsonAutomacao.Ler<ParametrosAcao>(a.ParametrosJson), foto)).ToList()));
            }
            var acoesQuadro = acoes.Where(a => DescricaoAutomacao.EhAcaoDeQuadro(a.Tipo)).ToList();
            if (acoesQuadro.Count > 0)
                resultado.Add(new AlvoSimulacao(null, "Quadro inteiro", "executado uma vez por disparo",
                    acoesQuadro.Select(a => DescricaoAutomacao.Acao(a.Tipo, JsonAutomacao.Ler<ParametrosAcao>(a.ParametrosJson), foto)).ToList()));
            return resultado;
        }

        /// <summary>Cartões que a regra alcançaria agora: os que estão na situação do gatilho e atendem às condições.</summary>
        private List<Cartao> AlvosDeCartao(RegraAutomacao regra, FotoQuadro foto)
        {
            IEnumerable<Cartao> candidatos = regra.Gatilho switch
            {
                TipoGatilho.CartaoEntrouNaLista when regra.ListaId.HasValue => foto.Cartoes.Where(c => foto.ListaOuPaiEm(c.ListaId, [regra.ListaId.Value])),
                TipoGatilho.ConflitoDatas => foto.Cartoes.Where(c => foto.Conflitos(c).Count > 0),
                TipoGatilho.ChecklistConcluido => foto.Cartoes.Where(c => c.ItensTarefa.Count > 0 && c.ItensTarefa.All(i => i.Concluido)),
                TipoGatilho.CartaoBloqueado => foto.Cartoes.Where(foto.EstaBloqueado),
                TipoGatilho.PrazoProximo or TipoGatilho.PrazoVencido or TipoGatilho.ParadoNaLista
                    => DisparosPorCartao(regra, foto, DateTime.Now).Select(d => d.Cartao),
                TipoGatilho.SprintIniciada or TipoGatilho.SprintEncerrando or TipoGatilho.Agendado
                    => DisparosDeQuadro(regra, foto, DateTime.Now.Date.AddDays(1).AddSeconds(-1)).SelectMany(d => d.Cartoes),
                _ => foto.Cartoes.Where(c => !foto.EstaConcluido(c))
            };
            return candidatos.DistinctBy(c => c.Id).Where(c => Atende(regra, c, foto)).ToList();
        }

        private static string SituacaoDoGatilho(RegraAutomacao regra, Cartao cartao, FotoQuadro foto) => regra.Gatilho switch
        {
            TipoGatilho.ConflitoDatas => $"conflita com {string.Join(", ", foto.Conflitos(cartao).Select(c => c.Titulo))}",
            TipoGatilho.ParadoNaLista => $"{foto.DiasNaLista(cartao)} dia(s) em \"{foto.NomeLista(cartao.ListaId)}\"",
            TipoGatilho.PrazoProximo or TipoGatilho.PrazoVencido => $"prazo {cartao.Prazo?.ToLocalTime():dd/MM/yyyy}",
            _ => $"em \"{foto.NomeLista(cartao.ListaId)}\""
        };

        /// <summary>Executa a regra agora sobre todos os alvos da simulação, ignorando o controle de disparo único.</summary>
        public async Task<int> ExecutarAgoraAsync(int regraId, int usuarioId)
        {
            var regra = await db.RegrasAutomacao.AsNoTracking().Include(r => r.Acoes).FirstAsync(r => r.Id == regraId);
            var foto = await CarregarFotoAsync(regra.QuadroId);
            var alvos = AlvosDeCartao(regra, foto);
            await ExecutarEmQuadroAsync(regra, foto, alvos, usuarioId, null);
            return alvos.Count;
        }

        // ───────────────────────── Execução ─────────────────────────

        private async Task ExecutarAsync(RegraAutomacao regra, FotoQuadro foto, int? cartaoId, int usuarioId, string? chave,
            EventoAutomacao? evento, int profundidade, IReadOnlyList<int> cadeia, bool? somenteAcoesDeCartao = null, string? resumoExtra = null)
        {
            var contexto = new Contexto { Regra = regra, Foto = foto, CartaoId = cartaoId, UsuarioId = usuarioId, Evento = evento };
            var resumo = new List<string>();
            var erros = new List<string>();
            var acoes = regra.Acoes.Where(a => !a.Excluido).OrderBy(a => a.Ordem)
                .Where(a => somenteAcoesDeCartao is null || DescricaoAutomacao.EhAcaoDeQuadro(a.Tipo) != somenteAcoesDeCartao.Value)
                .ToList();
            if (acoes.Count == 0 && resumoExtra is null) return;

            using (ContextoAutomacao.Entrar(profundidade + 1, [.. cadeia, regra.Id]))
            {
                foreach (var acao in acoes)
                {
                    try
                    {
                        var feito = await ExecutarAcaoAsync(acao, contexto);
                        if (!string.IsNullOrWhiteSpace(feito)) resumo.Add(feito);
                        await db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        db.ChangeTracker.Clear();
                        erros.Add($"{DescricaoAutomacao.NomeAcao(acao.Tipo)}: {ex.Message}");
                        logger.LogWarning(ex, "Ação {Acao} da regra {Regra} falhou no cartão {Cartao}.", acao.Tipo, regra.Id, cartaoId);
                    }
                }
            }

            if (resumoExtra is not null) resumo.Insert(0, resumoExtra);
            db.ExecucoesAutomacao.Add(new ExecucaoAutomacao
            {
                RegraAutomacaoId = regra.Id, QuadroId = regra.QuadroId, CartaoId = cartaoId, Gatilho = regra.Gatilho, ChaveDisparo = chave,
                Status = erros.Count == 0 ? StatusExecucaoAutomacao.Sucesso : resumo.Count > 0 ? StatusExecucaoAutomacao.Parcial : StatusExecucaoAutomacao.Erro,
                Resumo = Limitar(resumo.Count == 0 ? "Nenhuma alteração necessária." : string.Join(" · ", resumo), 2000),
                Erro = erros.Count == 0 ? null : Limitar(string.Join(" | ", erros), 2000),
                DesfazerJson = contexto.Desfazer.Count == 0 ? null : JsonAutomacao.Serializar(contexto.Desfazer),
                EmailsEnviados = contexto.EmailsEnviados, MensagensTeams = contexto.MensagensTeams, UsuarioId = usuarioId, OcorridoEm = DateTime.UtcNow
            });
            var regraRastreada = await db.RegrasAutomacao.FirstOrDefaultAsync(r => r.Id == regra.Id);
            if (regraRastreada is not null) regraRastreada.UltimaExecucaoEm = DateTime.UtcNow;
            using (ContextoAutomacao.Entrar(int.MaxValue / 2, []))
                await db.SaveChangesAsync();
        }

        private static string Limitar(string texto, int maximo) => texto.Length <= maximo ? texto : texto[..(maximo - 3)] + "...";

        private async Task<string?> ExecutarAcaoAsync(AcaoAutomacao acao, Contexto ctx)
        {
            var p = JsonAutomacao.Ler<ParametrosAcao>(acao.ParametrosJson);
            if (acao.Tipo == TipoAcaoAutomacao.EnviarResumo) return await EnviarResumoAsync(p, ctx);
            if (acao.Tipo == TipoAcaoAutomacao.PostarResumoNoTeams) return await PostarResumoNoTeamsAsync(p, ctx);
            if (ctx.CartaoId is not int cartaoId) return null;

            return acao.Tipo switch
            {
                TipoAcaoAutomacao.MoverCartao => await MoverAsync(cartaoId, p, ctx),
                TipoAcaoAutomacao.NotificarResponsavel => await NotificarAsync(cartaoId, p, ctx),
                TipoAcaoAutomacao.PreencherCampo => await PreencherCampoAsync(cartaoId, p, ctx),
                TipoAcaoAutomacao.AtribuirDesenvolvedor => await AtribuirAsync(cartaoId, p, ctx),
                TipoAcaoAutomacao.RemoverDesenvolvedores => await RemoverDesenvolvedoresAsync(cartaoId, p, ctx),
                TipoAcaoAutomacao.AdicionarEtiqueta => await AlterarEtiquetaAsync(cartaoId, p, ctx, adicionar: true),
                TipoAcaoAutomacao.RemoverEtiqueta => await AlterarEtiquetaAsync(cartaoId, p, ctx, adicionar: false),
                TipoAcaoAutomacao.DefinirPrioridade => await DefinirPrioridadeAsync(cartaoId, p, ctx),
                TipoAcaoAutomacao.DefinirPrazo => await DefinirDataAsync(cartaoId, p, ctx),
                TipoAcaoAutomacao.AdicionarChecklist => await AdicionarChecklistAsync(cartaoId, p, ctx),
                TipoAcaoAutomacao.CriarCartaoFilho => await CriarCartaoFilhoAsync(cartaoId, p, ctx),
                TipoAcaoAutomacao.Comentar => await ComentarAsync(cartaoId, p, ctx),
                TipoAcaoAutomacao.EnviarEmail => await EnviarEmailAsync(cartaoId, p, ctx),
                TipoAcaoAutomacao.CriarAlerta => await CriarAlertaAsync(cartaoId, p, ctx),
                TipoAcaoAutomacao.ResolverAlertas => await ResolverAlertasAsync(cartaoId, p, ctx),
                TipoAcaoAutomacao.ResolverConflitoDatas => await ResolverConflitoAsync(cartaoId, p, ctx),
                TipoAcaoAutomacao.PostarNoTeams => await PostarNoTeamsAsync(cartaoId, p, ctx),
                _ => null
            };
        }

        private string Texto(string? modelo, int? cartaoId, Contexto ctx, bool html)
        {
            var cartao = cartaoId.HasValue ? ctx.Foto.Cartao(cartaoId.Value) : null;
            var link = cartao is null ? urlBase.Valor : urlBase.LinkCartao(ctx.Foto.QuadroId, cartao.Id);
            return MarcadoresAutomacao.Aplicar(modelo, cartao, ctx.Foto, ctx.Regra.Nome, link, html, PrefixoCodigo);
        }

        /// <summary>Automação não preenche o que a lista atual do cartão proíbe (a ação fica registrada como erro).</summary>
        private async Task GarantirPermitidoNaListaAsync(Cartao cartao, CamposFixosCartao campo)
        {
            var lista = await db.Listas.AsNoTracking().Where(l => l.Id == cartao.ListaId)
                .Select(l => new { l.Nome, l.CamposFixosBloqueados }).FirstAsync();
            if (lista.CamposFixosBloqueados.HasFlag(campo))
                throw new InvalidOperationException($"a lista \"{lista.Nome}\" não permite {RegrasEtapa.Nome(campo).ToLowerInvariant()}.");
        }

        private async Task<Cartao> CartaoRastreadoAsync(int cartaoId)
            => await db.Cartoes.Include(c => c.Desenvolvedores).Include(c => c.Etiquetas).FirstAsync(c => c.Id == cartaoId && !c.Excluido);

        private async Task RecarregarFotoAsync(Contexto ctx)
        {
            await db.SaveChangesAsync();
            ctx.Foto = await CarregarFotoAsync(ctx.Foto.QuadroId);
        }

        // ───────────────────────── Ações ─────────────────────────

        private async Task<string?> MoverAsync(int cartaoId, ParametrosAcao p, Contexto ctx)
        {
            if (p.ListaId is not int listaId) throw new InvalidOperationException("lista de destino não definida.");
            var cartao = ctx.Foto.Cartao(cartaoId)!;
            var destino = ListasQuadro.PrimeiraFolha(listaId, ctx.Foto.Listas);
            if (cartao.ListaId == destino) return null;

            var pendencias = await movimentacao.ObterPendenciasAsync(cartaoId, destino);
            if (pendencias is { Bloqueia: true, TemObrigatoriasFaltando: true })
                throw new InvalidOperationException($"\"{pendencias.ListaNome}\" bloqueia a entrada com pendências; o cartão não foi movido.");

            var alvo = p.NoTopo ? ctx.Foto.Cartoes.Where(c => c.ListaId == destino).OrderBy(c => c.Ordem).FirstOrDefault()?.Id : null;
            var origem = cartao.ListaId;
            await db.SaveChangesAsync();
            var resultado = await movimentacao.MoverAsync(cartaoId, destino, alvo, permitirEstorno: false)
                ?? throw new InvalidOperationException("não foi possível mover o cartão.");
            ctx.Desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.Lista, CartaoId = cartaoId, Id = origem });
            await RecarregarFotoAsync(ctx);
            return $"movido para \"{resultado.ListaDestinoNome}\"";
        }

        private async Task<string?> NotificarAsync(int? cartaoId, ParametrosAcao p, Contexto ctx)
        {
            var destinatarios = ResolverDestinatarios(p, cartaoId.HasValue ? ctx.Foto.Cartao(cartaoId.Value) : null, ctx);
            if (destinatarios.Count == 0) return null;
            var mensagem = Texto(string.IsNullOrWhiteSpace(p.Texto) ? "Automação \"{regra}\": cartão \"{titulo}\"." : p.Texto, cartaoId, ctx, html: false);
            db.Notificacoes.AddRange(destinatarios.Select(id => new Notificacao
            {
                DestinatarioId = id, CartaoOrigemId = cartaoId, Tipo = TipoNotificacao.AutomacaoRegra,
                Mensagem = Limitar(mensagem, 1000), CriadoPorId = ctx.UsuarioId, CriadoEm = DateTime.UtcNow
            }));
            await Task.CompletedTask;
            return $"notificou {string.Join(", ", destinatarios.Select(id => ctx.Foto.NomeUsuario(id)))}";
        }

        private List<int> ResolverDestinatarios(ParametrosAcao p, Cartao? cartao, Contexto ctx)
        {
            var ids = new List<int>();
            if (cartao is not null)
            {
                if (p.Destinatarios.HasFlag(DestinatariosAutomacao.Desenvolvedores)) ids.AddRange(cartao.Desenvolvedores.Select(d => d.UsuarioId));
                if (p.Destinatarios.HasFlag(DestinatariosAutomacao.DesenvolvedorPrincipal))
                    ids.AddRange(cartao.Desenvolvedores.Where(d => d.Principal).Select(d => d.UsuarioId).DefaultIfEmpty(cartao.Desenvolvedores.FirstOrDefault()?.UsuarioId ?? 0).Where(id => id > 0));
                if (p.Destinatarios.HasFlag(DestinatariosAutomacao.Solicitante) && cartao.SolicitanteId.HasValue) ids.Add(cartao.SolicitanteId.Value);
                if (p.Destinatarios.HasFlag(DestinatariosAutomacao.Criador)) ids.Add(cartao.CriadoPorId);
            }
            if (p.Destinatarios.HasFlag(DestinatariosAutomacao.UsuariosEspecificos)) ids.AddRange(p.DestinatarioIds);
            var ativos = ctx.Foto.Usuarios.Select(u => u.Id).ToHashSet();
            return ids.Where(ativos.Contains).Distinct().ToList();
        }

        private async Task<string?> PreencherCampoAsync(int cartaoId, ParametrosAcao p, Contexto ctx)
        {
            var cartao = ctx.Foto.Cartao(cartaoId)!;
            var definicoes = ctx.Foto.Listas.SelectMany(l => l.DefinicoesCampo)
                .Where(d => d.Nome.Trim().Equals(p.CampoNome?.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            var definicao = definicoes.FirstOrDefault(d => d.ListaId == cartao.ListaId) ?? definicoes.FirstOrDefault()
                ?? throw new InvalidOperationException($"campo \"{p.CampoNome}\" não existe no quadro.");

            var modelo = p.Valor ?? "";
            if (definicao.Tipo == TipoCampo.Data)
                modelo = modelo.Replace("{hoje}", DateTime.Today.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase);
            var valor = Texto(modelo, cartaoId, ctx, html: false);

            var atual = await db.ValoresCampoCartao
                .Where(v => v.CartaoId == cartaoId && v.DefinicaoCampoId == definicao.Id)
                .OrderByDescending(v => v.NumeroEntradaNaLista).ThenByDescending(v => v.DataPreenchimento)
                .FirstOrDefaultAsync();
            if (p.SomenteSeVazio && !string.IsNullOrWhiteSpace(atual?.Valor)) return null;
            if (atual?.Valor == valor) return null;

            if (atual is null)
            {
                var entradas = await db.HistoricoAtividades.CountAsync(h => h.CartaoId == cartaoId && h.ListaDestinoId == definicao.ListaId
                    && (h.Tipo == TipoHistoricoAtividade.CartaoMovido || h.Tipo == TipoHistoricoAtividade.CartaoCriado));
                atual = new ValorCampoCartao
                {
                    CartaoId = cartaoId, DefinicaoCampoId = definicao.Id, Valor = valor, NumeroEntradaNaLista = Math.Max(1, entradas),
                    DataPreenchimento = DateTime.UtcNow, PreenchidoPorId = ctx.UsuarioId, CriadoPorId = ctx.UsuarioId, CriadoEm = DateTime.UtcNow
                };
                db.ValoresCampoCartao.Add(atual);
                await db.SaveChangesAsync();
                ctx.Desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.ValorCampo, CartaoId = cartaoId, Id = atual.Id, Flag = true });
            }
            else
            {
                ctx.Desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.ValorCampo, CartaoId = cartaoId, Id = atual.Id, ValorAnterior = atual.Valor });
                atual.Valor = valor;
                atual.DataPreenchimento = DateTime.UtcNow;
                atual.PreenchidoPorId = ctx.UsuarioId;
            }
            return $"\"{definicao.Nome}\" = \"{CamposCartao.Formatar(definicao.Tipo, valor)}\"";
        }

        private async Task<string?> AtribuirAsync(int cartaoId, ParametrosAcao p, Contexto ctx)
        {
            var cartao = await CartaoRastreadoAsync(cartaoId);
            await GarantirPermitidoNaListaAsync(cartao, CamposFixosCartao.Desenvolvedor);
            var usuarioId = p.ModoAtribuicao switch
            {
                ModoAtribuicao.UsuarioEspecifico => p.UsuarioId,
                ModoAtribuicao.Solicitante => cartao.SolicitanteId,
                ModoAtribuicao.QuemDisparou => ctx.Evento?.UsuarioId,
                ModoAtribuicao.MenorCarga => MenorCarga(cartaoId, p, ctx.Foto),
                ModoAtribuicao.Revezamento => await ProximoDoRevezamentoAsync(p, ctx),
                _ => null
            };
            if (usuarioId is not int id || ctx.Foto.Usuarios.All(u => u.Id != id))
                throw new InvalidOperationException("nenhum usuário elegível para atribuição.");

            if (p.SubstituirAtuais)
            {
                foreach (var atual in cartao.Desenvolvedores.Where(d => d.UsuarioId != id).ToList())
                {
                    ctx.Desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.DesenvolvedorRemovido, CartaoId = cartaoId, Id = atual.UsuarioId, Flag = atual.Principal });
                    db.CartaoDesenvolvedores.Remove(atual);
                    cartao.Desenvolvedores.Remove(atual);
                }
            }
            if (cartao.Desenvolvedores.Any(d => d.UsuarioId == id)) return null;

            db.CartaoDesenvolvedores.Add(new CartaoDesenvolvedor { CartaoId = cartaoId, UsuarioId = id, Principal = cartao.Desenvolvedores.All(d => !d.Principal) });
            if (id != ctx.UsuarioId)
            {
                db.Notificacoes.Add(new Notificacao
                {
                    DestinatarioId = id, CartaoOrigemId = cartaoId, Tipo = TipoNotificacao.AtribuicaoCartao,
                    Mensagem = $"A automação \"{ctx.Regra.Nome}\" atribuiu você ao cartão '{cartao.Titulo}'.", CriadoPorId = ctx.UsuarioId, CriadoEm = DateTime.UtcNow
                });
            }
            ctx.Desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.DesenvolvedorAdicionado, CartaoId = cartaoId, Id = id });
            return $"atribuído a {ctx.Foto.NomeUsuario(id)}";
        }

        private static List<int> Candidatos(ParametrosAcao p, FotoQuadro foto)
            => (p.UsuarioIds.Count > 0 ? p.UsuarioIds.Where(id => foto.Usuarios.Any(u => u.Id == id)) : foto.Usuarios.Select(u => u.Id))
                .OrderBy(id => foto.NomeUsuario(id)).ToList();

        /// <summary>Carga = soma das estimativas (1 ponto para cartão sem estimativa) dos cartões ativos em que a pessoa atua.</summary>
        public static int? MenorCarga(int cartaoId, ParametrosAcao p, FotoQuadro foto)
        {
            var candidatos = Candidatos(p, foto);
            if (candidatos.Count == 0) return null;
            var ativos = foto.Cartoes.Where(c => c.Id != cartaoId && !foto.EstaConcluido(c)).ToList();
            return candidatos
                .Select(id => (Id: id, Carga: ativos.Where(c => c.Desenvolvedores.Any(d => d.UsuarioId == id)).Sum(c => c.Estimativa ?? 1m)))
                .OrderBy(x => x.Carga).ThenBy(x => foto.NomeUsuario(x.Id))
                .First().Id;
        }

        private async Task<int?> ProximoDoRevezamentoAsync(ParametrosAcao p, Contexto ctx)
        {
            var candidatos = Candidatos(p, ctx.Foto);
            if (candidatos.Count == 0) return null;
            var anteriores = await db.ExecucoesAutomacao.AsNoTracking()
                .Where(e => e.RegraAutomacaoId == ctx.Regra.Id && e.DesfazerJson != null)
                .OrderByDescending(e => e.Id).Select(e => e.DesfazerJson).Take(20).ToListAsync();
            var ultimo = anteriores.SelectMany(json => JsonAutomacao.Ler<List<OperacaoDesfazer>>(json))
                .FirstOrDefault(o => o.Tipo == OperacaoDesfazer.Tipos.DesenvolvedorAdicionado)?.Id;
            var indice = ultimo.HasValue ? candidatos.IndexOf(ultimo.Value) : -1;
            return candidatos[(indice + 1) % candidatos.Count];
        }

        private async Task<string?> RemoverDesenvolvedoresAsync(int cartaoId, ParametrosAcao p, Contexto ctx)
        {
            var cartao = await CartaoRastreadoAsync(cartaoId);
            var remover = cartao.Desenvolvedores.Where(d => p.UsuarioIds.Count == 0 || p.UsuarioIds.Contains(d.UsuarioId)).ToList();
            if (remover.Count == 0) return null;
            foreach (var dev in remover)
            {
                ctx.Desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.DesenvolvedorRemovido, CartaoId = cartaoId, Id = dev.UsuarioId, Flag = dev.Principal });
                db.CartaoDesenvolvedores.Remove(dev);
            }
            return $"removeu {string.Join(", ", remover.Select(d => ctx.Foto.NomeUsuario(d.UsuarioId)))}";
        }

        private async Task<string?> AlterarEtiquetaAsync(int cartaoId, ParametrosAcao p, Contexto ctx, bool adicionar)
        {
            if (p.EtiquetaId is not int etiquetaId) throw new InvalidOperationException("etiqueta não definida.");
            var nome = ctx.Foto.Etiquetas.FirstOrDefault(e => e.Id == etiquetaId)?.Nome ?? throw new InvalidOperationException("etiqueta não existe mais.");
            var existente = await db.CartaoEtiquetas.FirstOrDefaultAsync(e => e.CartaoId == cartaoId && e.EtiquetaId == etiquetaId);
            if (adicionar == (existente is not null)) return null;
            if (adicionar)
            {
                db.CartaoEtiquetas.Add(new CartaoEtiqueta { CartaoId = cartaoId, EtiquetaId = etiquetaId });
                ctx.Desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.EtiquetaAdicionada, CartaoId = cartaoId, Id = etiquetaId });
                return $"etiqueta \"{nome}\" adicionada";
            }
            db.CartaoEtiquetas.Remove(existente!);
            ctx.Desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.EtiquetaRemovida, CartaoId = cartaoId, Id = etiquetaId });
            return $"etiqueta \"{nome}\" removida";
        }

        private async Task<string?> DefinirPrioridadeAsync(int cartaoId, ParametrosAcao p, Contexto ctx)
        {
            var cartao = await CartaoRastreadoAsync(cartaoId);
            var nova = p.AumentarUmNivel
                ? cartao.Prioridade.HasValue ? (Prioridade)Math.Min((int)Prioridade.Critica, (int)cartao.Prioridade.Value + 1) : Prioridade.Media
                : p.Prioridade;
            if (nova == cartao.Prioridade) return null;
            ctx.Desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.Prioridade, CartaoId = cartaoId, ValorAnterior = ((int?)cartao.Prioridade)?.ToString() });
            cartao.Prioridade = nova;
            return $"prioridade {nova?.ToString() ?? "removida"}";
        }

        private async Task<string?> DefinirDataAsync(int cartaoId, ParametrosAcao p, Contexto ctx)
        {
            var cartao = await CartaoRastreadoAsync(cartaoId);
            if (p.BaseData != BaseDataAutomacao.Limpar)
                await GarantirPermitidoNaListaAsync(cartao, p.AplicarEmDataInicio ? CamposFixosCartao.DataInicio : CamposFixosCartao.Prazo);
            DateTime? nova = null;
            if (p.BaseData != BaseDataAutomacao.Limpar)
            {
                var baseData = p.BaseData == BaseDataAutomacao.PrazoAtual && cartao.Prazo.HasValue ? cartao.Prazo.Value.ToLocalTime().Date : DateTime.Today;
                nova = (p.DiasUteis ? DiasUteis.Adicionar(baseData, p.Dias, ctx.Foto.Feriados) : baseData.AddDays(p.Dias)).ToUniversalTime();
            }
            var atual = p.AplicarEmDataInicio ? cartao.DataInicio : cartao.Prazo;
            if (atual == nova) return null;
            ctx.Desfazer.Add(new OperacaoDesfazer
            {
                Tipo = p.AplicarEmDataInicio ? OperacaoDesfazer.Tipos.DataInicio : OperacaoDesfazer.Tipos.Prazo,
                CartaoId = cartaoId, ValorAnterior = atual?.ToString("o", CultureInfo.InvariantCulture)
            });
            if (p.AplicarEmDataInicio) cartao.DataInicio = nova; else cartao.Prazo = nova;
            var nome = p.AplicarEmDataInicio ? "início" : "prazo";
            return nova.HasValue ? $"{nome} definido para {nova.Value.ToLocalTime():dd/MM/yyyy}" : $"{nome} removido";
        }

        private async Task<string?> AdicionarChecklistAsync(int cartaoId, ParametrosAcao p, Contexto ctx)
        {
            var itens = p.Itens.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => Texto(i.Trim(), cartaoId, ctx, html: false)).ToList();
            if (itens.Count == 0) return null;
            var existentes = await db.ItensTarefa.Where(i => i.CartaoId == cartaoId && !i.Excluido).Select(i => new { i.Titulo, i.Ordem }).ToListAsync();
            var novos = itens.Where(i => existentes.All(e => !e.Titulo.Equals(i, StringComparison.OrdinalIgnoreCase)))
                .Select((titulo, indice) => new ItemTarefa
                {
                    CartaoId = cartaoId, Titulo = Limitar(titulo, 500), Ordem = (existentes.Select(e => (int?)e.Ordem).Max() ?? 0) + indice + 1,
                    CriadoPorId = ctx.UsuarioId, CriadoEm = DateTime.UtcNow
                }).ToList();
            if (novos.Count == 0) return null;
            db.ItensTarefa.AddRange(novos);
            await db.SaveChangesAsync();
            ctx.Desfazer.AddRange(novos.Select(n => new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.ItemCriado, CartaoId = cartaoId, Id = n.Id }));
            return $"{novos.Count} item(ns) adicionados ao checklist";
        }

        private async Task<string?> CriarCartaoFilhoAsync(int cartaoId, ParametrosAcao p, Contexto ctx)
        {
            var pai = ctx.Foto.Cartao(cartaoId)!;
            var listaId = ListasQuadro.PrimeiraFolha(p.ListaId ?? pai.ListaId, ctx.Foto.Listas);
            var titulo = Limitar(Texto(string.IsNullOrWhiteSpace(p.Titulo) ? "Subtarefa de {titulo}" : p.Titulo, cartaoId, ctx, html: false), 300);
            var ordem = (await db.Cartoes.Where(c => c.ListaId == listaId && !c.Excluido).Select(c => (int?)c.Ordem).MaxAsync() ?? 0) + 1;
            var filho = new Cartao
            {
                Titulo = titulo, ListaId = listaId, Ordem = ordem, CartaoPaiId = cartaoId, SistemaId = pai.SistemaId,
                SolicitanteId = pai.SolicitanteId, CriadoPorId = ctx.UsuarioId, CriadoEm = DateTime.UtcNow
            };
            db.Cartoes.Add(filho);
            await db.SaveChangesAsync();
            db.HistoricoAtividades.Add(new HistoricoAtividade
            {
                CartaoId = filho.Id, Tipo = TipoHistoricoAtividade.CartaoCriado, ListaDestinoId = listaId,
                Descricao = $"Cartão criado pela automação \"{ctx.Regra.Nome}\".", UsuarioId = ctx.UsuarioId, OcorridoEm = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            await movimentacao.AplicarEntradaNaCriacaoAsync(filho.Id);
            ctx.Desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.CartaoCriado, CartaoId = cartaoId, Id = filho.Id });
            return $"cartão filho \"{titulo}\" criado";
        }

        private async Task<string?> ComentarAsync(int cartaoId, ParametrosAcao p, Contexto ctx)
        {
            if (string.IsNullOrWhiteSpace(p.Texto)) return null;
            var comentario = new Comentario
            {
                CartaoId = cartaoId, Texto = Limitar($"[Automação: {ctx.Regra.Nome}] {Texto(p.Texto, cartaoId, ctx, html: false)}", 4000),
                AutorId = UsuarioSistemaId, DataHora = DateTime.UtcNow, CriadoPorId = UsuarioSistemaId, CriadoEm = DateTime.UtcNow
            };
            db.Comentarios.Add(comentario);
            await db.SaveChangesAsync();
            ctx.Desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.ComentarioCriado, CartaoId = cartaoId, Id = comentario.Id });
            return "comentário adicionado";
        }

        private async Task<int> EmailsNaUltimaHoraAsync(Contexto ctx)
        {
            var desde = DateTime.UtcNow.AddHours(-1);
            return await db.ExecucoesAutomacao.Where(e => e.RegraAutomacaoId == ctx.Regra.Id && e.OcorridoEm >= desde).SumAsync(e => e.EmailsEnviados) + ctx.EmailsEnviados;
        }

        private (List<string> Emails, List<string> Nomes) EnderecosEmail(ParametrosAcao p, Cartao? cartao, Contexto ctx)
        {
            var usuarios = ResolverDestinatarios(p, cartao, ctx)
                .Select(id => ctx.Foto.Usuarios.First(u => u.Id == id))
                .Where(u => u.ReceberEmailsAutomacao && !string.IsNullOrWhiteSpace(u.Email)).ToList();
            var fixos = (p.EmailsFixos ?? "").Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(e => System.Net.Mail.MailAddress.TryCreate(e, out _));
            var emails = usuarios.Select(u => u.Email).Concat(fixos).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return (emails, usuarios.Select(u => u.Nome).Concat(fixos).Distinct().ToList());
        }

        private async Task<string?> EnviarEmailAsync(int cartaoId, ParametrosAcao p, Contexto ctx)
        {
            var cartao = ctx.Foto.Cartao(cartaoId);
            var (emails, nomes) = EnderecosEmail(p, cartao, ctx);
            if (emails.Count == 0) return null;
            if (await EmailsNaUltimaHoraAsync(ctx) >= LimiteEmailsPorHora)
                throw new InvalidOperationException($"limite de {LimiteEmailsPorHora} e-mails por hora desta regra atingido.");

            var assunto = Limitar(Texto(string.IsNullOrWhiteSpace(p.Assunto) ? "[{regra}] {titulo}" : p.Assunto, cartaoId, ctx, html: false), 300);
            var corpo = Texto(string.IsNullOrWhiteSpace(p.Texto) ? "O cartão \"{titulo}\" ({lista}, prazo {prazo}) precisa de atenção.\n\n{link}" : p.Texto, cartaoId, ctx, html: true);
            var para = string.Join("; ", emails);
            var registro = new EmailCartao
            {
                CartaoId = cartaoId, Para = Limitar(para, 1000), Assunto = assunto, CorpoHtml = corpo,
                EnviadoPorId = ctx.UsuarioId, EnviadoEm = DateTime.UtcNow, CriadoPorId = ctx.UsuarioId, CriadoEm = DateTime.UtcNow
            };
            try
            {
                await emailSender.EnviarAsync(para, null, assunto, corpo);
                registro.Enviado = true;
            }
            catch (Exception ex)
            {
                registro.ErroEnvio = Limitar(ex.Message, 2000);
                db.EmailsCartao.Add(registro);
                throw new InvalidOperationException($"falha ao enviar e-mail: {ex.Message}");
            }
            db.EmailsCartao.Add(registro);
            ctx.EmailsEnviados++;
            return $"e-mail enviado para {string.Join(", ", nomes)}";
        }

        private async Task<string?> CriarAlertaAsync(int cartaoId, ParametrosAcao p, Contexto ctx)
        {
            var mensagem = Limitar(Texto(string.IsNullOrWhiteSpace(p.Texto) ? "Atenção: {regra}" : p.Texto, cartaoId, ctx, html: false), 500);
            var jaAberto = await db.AlertasCartao.AnyAsync(a => a.CartaoId == cartaoId && !a.Resolvido && a.RegraAutomacaoId == ctx.Regra.Id && a.Mensagem == mensagem);
            if (jaAberto) return null;
            var alerta = new AlertaCartao { CartaoId = cartaoId, RegraAutomacaoId = ctx.Regra.Id, Mensagem = mensagem, Severidade = p.Severidade, CriadoEm = DateTime.UtcNow };
            db.AlertasCartao.Add(alerta);
            await db.SaveChangesAsync();
            ctx.Desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.AlertaCriado, CartaoId = cartaoId, Id = alerta.Id });
            return $"alerta \"{mensagem}\"";
        }

        private async Task<string?> ResolverAlertasAsync(int cartaoId, ParametrosAcao p, Contexto ctx)
        {
            var alertas = await db.AlertasCartao
                .Where(a => a.CartaoId == cartaoId && !a.Resolvido && (p.TodosAlertasDoCartao || a.RegraAutomacaoId == ctx.Regra.Id)).ToListAsync();
            if (alertas.Count == 0) return null;
            foreach (var alerta in alertas)
            {
                alerta.Resolvido = true;
                alerta.ResolvidoEm = DateTime.UtcNow;
                alerta.ResolvidoPorId = ctx.UsuarioId;
                ctx.Desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.AlertaResolvido, CartaoId = cartaoId, Id = alerta.Id });
            }
            return $"{alertas.Count} alerta(s) resolvido(s)";
        }

        // ───────────────────────── Conflito de datas ─────────────────────────

        /// <summary>Qual dos dois cartões cede a vez; empates caem no mais recente.</summary>
        public static Cartao Cedente(Cartao a, Cartao b, EstrategiaConflito estrategia)
        {
            Cartao MaisRecente() => a.CriadoEm > b.CriadoEm || a.CriadoEm == b.CriadoEm && a.Id > b.Id ? a : b;
            static bool Iniciado(Cartao c) => c.DataInicio.HasValue && c.DataInicio.Value.ToLocalTime().Date <= DateTime.Today;
            return estrategia switch
            {
                EstrategiaConflito.MenorPrioridade when (int?)a.Prioridade != (int?)b.Prioridade
                    => ((int?)a.Prioridade ?? 0) < ((int?)b.Prioridade ?? 0) ? a : b,
                EstrategiaConflito.MaisAntigo => MaisRecente() == a ? b : a,
                EstrategiaConflito.NaoIniciado when Iniciado(a) != Iniciado(b) => Iniciado(a) ? b : a,
                _ => MaisRecente()
            };
        }

        /// <summary>
        /// Novo intervalo para o cartão que cede: começa no primeiro dia útil livre depois dos cartões com que conflita,
        /// mantém a duração (dias úteis entre início e prazo, ou a estimativa) e avança até não conflitar com mais ninguém.
        /// </summary>
        public static PropostaConflito CalcularNovasDatas(Cartao cedente, IReadOnlyCollection<Cartao> vencedores, FotoQuadro foto, decimal pontosPorDia)
        {
            var atual = ConflitosAgenda.Intervalo(cedente)!.Value;
            var feriados = foto.Feriados;
            var duracao = cedente.DataInicio.HasValue
                ? DiasUteis.Contar(atual.Inicio, atual.Fim, feriados)
                : cedente.Estimativa is > 0 ? (int)Math.Ceiling(cedente.Estimativa.Value / Math.Max(0.1m, pontosPorDia)) : 1;
            var devs = cedente.Desenvolvedores.Select(d => d.UsuarioId).ToHashSet();
            var outros = foto.Cartoes
                .Where(c => c.Id != cedente.Id && !foto.EstaConcluido(c) && devs.Overlaps(c.Desenvolvedores.Select(d => d.UsuarioId)))
                .Select(ConflitosAgenda.Intervalo).OfType<(DateTime Inicio, DateTime Fim)>().ToList();

            var limite = vencedores.Select(ConflitosAgenda.Intervalo).OfType<(DateTime Inicio, DateTime Fim)>().Select(i => i.Fim).DefaultIfEmpty(atual.Fim).Max();
            var inicio = DiasUteis.ProximoUtil(new[] { limite.AddDays(1), DateTime.Today }.Max(), feriados);
            var fim = DiasUteis.Adicionar(inicio, duracao - 1, feriados);
            for (var tentativa = 0; tentativa < 200; tentativa++)
            {
                var sobrepostos = outros.Where(o => ConflitosAgenda.Sobrepoe((inicio, fim), o)).ToList();
                if (sobrepostos.Count == 0) break;
                inicio = DiasUteis.ProximoUtil(sobrepostos.Max(o => o.Fim).AddDays(1), feriados);
                fim = DiasUteis.Adicionar(inicio, duracao - 1, feriados);
            }
            return new PropostaConflito
            {
                NovaDataInicio = inicio.ToUniversalTime(), NovoPrazo = fim.ToUniversalTime(),
                ConflitaCom = vencedores.Select(v => v.Id).ToList()
            };
        }

        private async Task<string?> ResolverConflitoAsync(int cartaoId, ParametrosAcao p, Contexto ctx)
        {
            var cartao = ctx.Foto.Cartao(cartaoId)!;
            var conflitos = ctx.Foto.Conflitos(cartao);
            if (conflitos.Count == 0) return null;

            var cedentes = conflitos
                .Select(outro => (Cedente: Cedente(cartao, outro, p.Estrategia), Outro: outro))
                .GroupBy(x => x.Cedente.Id)
                .Select(g => (Cedente: g.First().Cedente, Vencedores: g.Select(x => x.Cedente.Id == cartao.Id ? x.Outro : cartao).DistinctBy(c => c.Id).ToList()))
                .ToList();

            var feitos = new List<string>();
            foreach (var (cedente, vencedores) in cedentes)
            {
                if (p.Resolucao == ResolucaoConflito.ApenasAvisar)
                {
                    var envolvidos = vencedores.Append(cedente).SelectMany(c => c.Desenvolvedores.Select(d => d.UsuarioId)).Distinct().ToList();
                    db.Notificacoes.AddRange(envolvidos.Select(id => new Notificacao
                    {
                        DestinatarioId = id, CartaoOrigemId = cedente.Id, Tipo = TipoNotificacao.ConflitoData,
                        Mensagem = $"Conflito de datas: \"{cedente.Titulo}\" x {string.Join(", ", vencedores.Select(v => $"\"{v.Titulo}\""))}.",
                        CriadoPorId = ctx.UsuarioId, CriadoEm = DateTime.UtcNow
                    }));
                    feitos.Add($"conflito de \"{cedente.Titulo}\" avisado");
                    continue;
                }

                var proposta = p.Resolucao == ResolucaoConflito.EmpurrarDatas
                    ? CalcularNovasDatas(cedente, vencedores, ctx.Foto, PontosPorDia)
                    : new PropostaConflito { ConflitaCom = vencedores.Select(v => v.Id).ToList() };
                proposta.ListaId = p.ListaId;
                if (proposta.NovoPrazo is null && proposta.ListaId is null) throw new InvalidOperationException("escolha a lista para onde mover o cartão.");
                var descricao = DescreverProposta(cedente, proposta, vencedores, ctx.Foto);

                if (p.Modo == ModoAplicacao.Sugerir)
                {
                    var antigas = await db.SugestoesAutomacao
                        .Where(s => s.CartaoId == cedente.Id && s.RegraAutomacaoId == ctx.Regra.Id && s.Status == StatusSugestao.Pendente).ToListAsync();
                    foreach (var antiga in antigas) { antiga.Status = StatusSugestao.Descartada; antiga.DecididoEm = DateTime.UtcNow; }
                    var sugestao = new SugestaoAutomacao
                    {
                        RegraAutomacaoId = ctx.Regra.Id, QuadroId = ctx.Foto.QuadroId, CartaoId = cedente.Id,
                        Descricao = Limitar(descricao, 1000), PropostaJson = JsonAutomacao.Serializar(proposta), CriadoEm = DateTime.UtcNow
                    };
                    db.SugestoesAutomacao.Add(sugestao);
                    db.Notificacoes.AddRange(cedente.Desenvolvedores.Select(d => d.UsuarioId).Distinct().Select(id => new Notificacao
                    {
                        DestinatarioId = id, CartaoOrigemId = cedente.Id, Tipo = TipoNotificacao.ConflitoData,
                        Mensagem = Limitar($"Sugestão para resolver conflito: {descricao}", 1000), CriadoPorId = ctx.UsuarioId, CriadoEm = DateTime.UtcNow
                    }));
                    await db.SaveChangesAsync();
                    ctx.Desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.SugestaoCriada, CartaoId = cedente.Id, Id = sugestao.Id });
                    feitos.Add($"sugestão criada: {descricao}");
                }
                else
                {
                    await AplicarPropostaAsync(cedente.Id, proposta, ctx.Desfazer);
                    await RecarregarFotoAsync(ctx);
                    feitos.Add(descricao);
                }
            }
            return string.Join(" · ", feitos);
        }

        private static string DescreverProposta(Cartao cedente, PropostaConflito proposta, IEnumerable<Cartao> vencedores, FotoQuadro foto)
        {
            var partes = new List<string>();
            if (proposta.NovoPrazo.HasValue)
                partes.Add($"remarcar \"{cedente.Titulo}\" para {proposta.NovaDataInicio?.ToLocalTime():dd/MM/yyyy} – {proposta.NovoPrazo.Value.ToLocalTime():dd/MM/yyyy}");
            if (proposta.ListaId.HasValue)
                partes.Add($"{(partes.Count == 0 ? $"mover \"{cedente.Titulo}\"" : "e mover")} para \"{foto.NomeLista(proposta.ListaId.Value)}\"");
            return $"{string.Join(" ", partes)} (conflitava com {string.Join(", ", vencedores.Select(v => $"\"{v.Titulo}\""))})";
        }

        private async Task AplicarPropostaAsync(int cartaoId, PropostaConflito proposta, List<OperacaoDesfazer> desfazer)
        {
            if (proposta.NovoPrazo.HasValue)
            {
                var cartao = await db.Cartoes.FirstAsync(c => c.Id == cartaoId);
                desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.DataInicio, CartaoId = cartaoId, ValorAnterior = cartao.DataInicio?.ToString("o", CultureInfo.InvariantCulture) });
                desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.Prazo, CartaoId = cartaoId, ValorAnterior = cartao.Prazo?.ToString("o", CultureInfo.InvariantCulture) });
                cartao.DataInicio = proposta.NovaDataInicio;
                cartao.Prazo = proposta.NovoPrazo;
                await db.SaveChangesAsync();
            }
            if (proposta.ListaId is int listaId)
            {
                var origem = await db.Cartoes.Where(c => c.Id == cartaoId).Select(c => c.ListaId).FirstAsync();
                var pendencias = await movimentacao.ObterPendenciasAsync(cartaoId, listaId);
                if (pendencias is { Bloqueia: true, TemObrigatoriasFaltando: true })
                    throw new InvalidOperationException($"\"{pendencias.ListaNome}\" bloqueia a entrada com pendências; o cartão não foi movido.");
                var resultado = await movimentacao.MoverAsync(cartaoId, listaId, null, permitirEstorno: false);
                if (resultado is { MudouDeLista: true })
                    desfazer.Add(new OperacaoDesfazer { Tipo = OperacaoDesfazer.Tipos.Lista, CartaoId = cartaoId, Id = origem });
            }
        }

        // ───────────────────────── Sugestões ─────────────────────────

        public async Task AprovarSugestaoAsync(int sugestaoId, int usuarioId)
        {
            var sugestao = await db.SugestoesAutomacao.Include(s => s.RegraAutomacao).FirstAsync(s => s.Id == sugestaoId);
            if (sugestao.Status != StatusSugestao.Pendente) throw new InvalidOperationException("Esta sugestão já foi decidida.");
            var desfazer = new List<OperacaoDesfazer>();
            await AplicarPropostaAsync(sugestao.CartaoId, JsonAutomacao.Ler<PropostaConflito>(sugestao.PropostaJson), desfazer);

            sugestao.Status = StatusSugestao.Aprovada;
            sugestao.DecididoEm = DateTime.UtcNow;
            sugestao.DecididoPorId = usuarioId;
            db.ExecucoesAutomacao.Add(new ExecucaoAutomacao
            {
                RegraAutomacaoId = sugestao.RegraAutomacaoId, QuadroId = sugestao.QuadroId, CartaoId = sugestao.CartaoId,
                Gatilho = sugestao.RegraAutomacao.Gatilho, Status = StatusExecucaoAutomacao.Sucesso,
                Resumo = Limitar($"Sugestão aprovada: {sugestao.Descricao}", 2000),
                DesfazerJson = desfazer.Count == 0 ? null : JsonAutomacao.Serializar(desfazer), UsuarioId = usuarioId, OcorridoEm = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        public async Task RejeitarSugestaoAsync(int sugestaoId, int usuarioId)
        {
            var sugestao = await db.SugestoesAutomacao.FirstAsync(s => s.Id == sugestaoId);
            if (sugestao.Status != StatusSugestao.Pendente) return;
            sugestao.Status = StatusSugestao.Rejeitada;
            sugestao.DecididoEm = DateTime.UtcNow;
            sugestao.DecididoPorId = usuarioId;
            await db.SaveChangesAsync();
        }

        // ───────────────────────── Desfazer ─────────────────────────

        /// <summary>
        /// Reverte as alterações de uma execução, da última para a primeira. Notificações e e-mails já enviados não voltam.
        /// A reversão não dispara outras regras.
        /// </summary>
        public async Task DesfazerAsync(int execucaoId, int usuarioId)
        {
            var execucao = await db.ExecucoesAutomacao.FirstAsync(e => e.Id == execucaoId);
            if (execucao.DesfeitaEm.HasValue) throw new InvalidOperationException("Esta execução já foi desfeita.");
            var operacoes = JsonAutomacao.Ler<List<OperacaoDesfazer>>(execucao.DesfazerJson);

            using (ContextoAutomacao.Entrar(int.MaxValue / 2, []))
            {
                foreach (var op in Enumerable.Reverse(operacoes))
                {
                    await DesfazerOperacaoAsync(op, usuarioId);
                    await db.SaveChangesAsync();
                }
                execucao.Status = StatusExecucaoAutomacao.Desfeita;
                execucao.DesfeitaEm = DateTime.UtcNow;
                execucao.DesfeitaPorId = usuarioId;
                await db.SaveChangesAsync();
            }
        }

        private async Task DesfazerOperacaoAsync(OperacaoDesfazer op, int usuarioId)
        {
            DateTime? Data(string? valor) => DateTime.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d) ? d : null;
            switch (op.Tipo)
            {
                case OperacaoDesfazer.Tipos.Lista when op.Id.HasValue:
                    await db.SaveChangesAsync();
                    await movimentacao.MoverAsync(op.CartaoId, op.Id.Value, null, permitirEstorno: false);
                    break;
                case OperacaoDesfazer.Tipos.Prioridade:
                    (await db.Cartoes.FirstAsync(c => c.Id == op.CartaoId)).Prioridade = int.TryParse(op.ValorAnterior, out var prioridade) ? (Prioridade)prioridade : null;
                    break;
                case OperacaoDesfazer.Tipos.Prazo:
                    (await db.Cartoes.FirstAsync(c => c.Id == op.CartaoId)).Prazo = Data(op.ValorAnterior);
                    break;
                case OperacaoDesfazer.Tipos.DataInicio:
                    (await db.Cartoes.FirstAsync(c => c.Id == op.CartaoId)).DataInicio = Data(op.ValorAnterior);
                    break;
                case OperacaoDesfazer.Tipos.DesenvolvedorAdicionado:
                    var adicionado = await db.CartaoDesenvolvedores.FirstOrDefaultAsync(d => d.CartaoId == op.CartaoId && d.UsuarioId == op.Id);
                    if (adicionado is not null) db.CartaoDesenvolvedores.Remove(adicionado);
                    break;
                case OperacaoDesfazer.Tipos.DesenvolvedorRemovido when op.Id.HasValue:
                    if (!await db.CartaoDesenvolvedores.AnyAsync(d => d.CartaoId == op.CartaoId && d.UsuarioId == op.Id))
                        db.CartaoDesenvolvedores.Add(new CartaoDesenvolvedor { CartaoId = op.CartaoId, UsuarioId = op.Id.Value, Principal = op.Flag });
                    break;
                case OperacaoDesfazer.Tipos.EtiquetaAdicionada:
                    var etiqueta = await db.CartaoEtiquetas.FirstOrDefaultAsync(e => e.CartaoId == op.CartaoId && e.EtiquetaId == op.Id);
                    if (etiqueta is not null) db.CartaoEtiquetas.Remove(etiqueta);
                    break;
                case OperacaoDesfazer.Tipos.EtiquetaRemovida when op.Id.HasValue:
                    if (!await db.CartaoEtiquetas.AnyAsync(e => e.CartaoId == op.CartaoId && e.EtiquetaId == op.Id))
                        db.CartaoEtiquetas.Add(new CartaoEtiqueta { CartaoId = op.CartaoId, EtiquetaId = op.Id.Value });
                    break;
                case OperacaoDesfazer.Tipos.ValorCampo:
                    var valor = await db.ValoresCampoCartao.FirstOrDefaultAsync(v => v.Id == op.Id);
                    if (valor is null) break;
                    if (op.Flag) { valor.Excluido = true; valor.ExcluidoEm = DateTime.UtcNow; valor.ExcluidoPorId = usuarioId; }
                    else valor.Valor = op.ValorAnterior;
                    break;
                case OperacaoDesfazer.Tipos.AlertaCriado:
                case OperacaoDesfazer.Tipos.AlertaResolvido:
                    var alerta = await db.AlertasCartao.FirstOrDefaultAsync(a => a.Id == op.Id);
                    if (alerta is null) break;
                    alerta.Resolvido = op.Tipo == OperacaoDesfazer.Tipos.AlertaCriado;
                    alerta.ResolvidoEm = alerta.Resolvido ? DateTime.UtcNow : null;
                    alerta.ResolvidoPorId = alerta.Resolvido ? usuarioId : null;
                    break;
                case OperacaoDesfazer.Tipos.ItemCriado:
                    var item = await db.ItensTarefa.FirstOrDefaultAsync(i => i.Id == op.Id);
                    if (item is not null) { item.Excluido = true; item.ExcluidoEm = DateTime.UtcNow; item.ExcluidoPorId = usuarioId; }
                    break;
                case OperacaoDesfazer.Tipos.ComentarioCriado:
                    var comentario = await db.Comentarios.FirstOrDefaultAsync(c => c.Id == op.Id);
                    if (comentario is not null) { comentario.Excluido = true; comentario.ExcluidoEm = DateTime.UtcNow; comentario.ExcluidoPorId = usuarioId; }
                    break;
                case OperacaoDesfazer.Tipos.CartaoCriado when op.Id.HasValue:
                    await db.SaveChangesAsync();
                    await movimentacao.ExcluirAsync(op.Id.Value);
                    break;
                case OperacaoDesfazer.Tipos.SugestaoCriada:
                    var sugestao = await db.SugestoesAutomacao.FirstOrDefaultAsync(s => s.Id == op.Id && s.Status == StatusSugestao.Pendente);
                    if (sugestao is not null) { sugestao.Status = StatusSugestao.Descartada; sugestao.DecididoEm = DateTime.UtcNow; sugestao.DecididoPorId = usuarioId; }
                    break;
            }
        }

        // ───────────────────────── Teams ─────────────────────────

        private async Task GarantirLimiteTeamsAsync(Contexto ctx)
        {
            var desde = DateTime.UtcNow.AddHours(-1);
            var enviadas = await db.ExecucoesAutomacao.Where(e => e.RegraAutomacaoId == ctx.Regra.Id && e.OcorridoEm >= desde).SumAsync(e => e.MensagensTeams) + ctx.MensagensTeams;
            if (enviadas >= LimiteTeamsPorHora)
                throw new InvalidOperationException($"limite de {LimiteTeamsPorHora} mensagens no Teams por hora desta regra atingido.");
        }

        private async Task<string?> PostarNoTeamsAsync(int cartaoId, ParametrosAcao p, Contexto ctx)
        {
            if (!MensagemTeams.UrlValida(p.UrlWebhook)) throw new InvalidOperationException("URL do webhook do Teams não configurada ou inválida.");
            await GarantirLimiteTeamsAsync(ctx);
            var cartao = ctx.Foto.Cartao(cartaoId)!;
            var titulo = Texto(string.IsNullOrWhiteSpace(p.Titulo) ? "{codigo} · {titulo}" : p.Titulo, cartaoId, ctx, html: false);
            var texto = Texto(p.Texto, cartaoId, ctx, html: false);
            var fatos = new List<MensagemTeams.Fato>
            {
                new("Lista", ctx.Foto.NomeLista(cartao.ListaId)),
                new("Prazo", cartao.Prazo?.ToLocalTime().ToString("dd/MM/yyyy") ?? ""),
                new("Prioridade", cartao.Prioridade?.ToString() ?? ""),
                new("Desenvolvedores", string.Join(", ", cartao.Desenvolvedores.Select(d => ctx.Foto.NomeUsuario(d.UsuarioId)).OfType<string>())),
                new("Solicitante", ctx.Foto.NomeUsuario(cartao.SolicitanteId) ?? "")
            };
            var link = string.IsNullOrEmpty(urlBase.Valor) ? null : urlBase.LinkCartao(ctx.Foto.QuadroId, cartaoId);
            await teams.EnviarAsync(p.UrlWebhook!, MensagemTeams.Montar(titulo, texto, fatos, link, "Abrir cartão"));
            ctx.MensagensTeams++;
            return "mensagem postada no Teams";
        }

        private async Task<string?> PostarResumoNoTeamsAsync(ParametrosAcao p, Contexto ctx)
        {
            if (!MensagemTeams.UrlValida(p.UrlWebhook)) throw new InvalidOperationException("URL do webhook do Teams não configurada ou inválida.");
            await GarantirLimiteTeamsAsync(ctx);
            var foto = ctx.Foto;
            var hoje = DateTime.Today;
            var ativos = foto.Cartoes.Where(c => !foto.EstaConcluido(c)).ToList();
            var atrasados = ativos.Where(c => c.Prazo.HasValue && c.Prazo.Value.ToLocalTime().Date < hoje).OrderBy(c => c.Prazo).ToList();
            var fatos = new List<MensagemTeams.Fato>
            {
                new("Em andamento", ativos.Count.ToString()),
                new("Atrasados", atrasados.Count.ToString()),
                new("Vencem em até 3 dias", ativos.Count(c => c.Prazo.HasValue && (c.Prazo.Value.ToLocalTime().Date - hoje).Days is >= 0 and <= 3).ToString()),
                new("Bloqueados", ativos.Count(foto.EstaBloqueado).ToString()),
                new("Com conflito de datas", ativos.Count(c => foto.Conflitos(c).Count > 0).ToString()),
                new("Parados há 5+ dias", ativos.Count(c => foto.DiasNaLista(c) >= 5).ToString())
            };
            var itens = atrasados.Take(10)
                .Select(c => $"• {CodigoCartao.Formatar(c.Id, PrefixoCodigo)} {c.Titulo} — prazo {c.Prazo!.Value.ToLocalTime():dd/MM} ({string.Join(", ", c.Desenvolvedores.Select(d => foto.NomeUsuario(d.UsuarioId)).OfType<string>())})")
                .ToList();
            if (atrasados.Count > 10) itens.Add($"• e mais {atrasados.Count - 10} atrasado(s)");
            var titulo = Texto(string.IsNullOrWhiteSpace(p.Titulo) ? "Resumo do quadro — {hoje}" : p.Titulo, null, ctx, html: false);
            var link = string.IsNullOrEmpty(urlBase.Valor) ? null : $"{urlBase.Valor}/quadros/{foto.QuadroId}";
            await teams.EnviarAsync(p.UrlWebhook!, MensagemTeams.Montar(titulo, Texto(p.Texto, null, ctx, html: false), fatos,
                link, "Abrir quadro", itens.Count > 0 ? itens.Prepend("Atrasados:") : null));
            ctx.MensagensTeams++;
            return "resumo postado no Teams";
        }

        // ───────────────────────── Resumo por e-mail ─────────────────────────

        private async Task<string?> EnviarResumoAsync(ParametrosAcao p, Contexto ctx)
        {
            var foto = ctx.Foto;
            var ativos = foto.Cartoes.Where(c => !foto.EstaConcluido(c)).ToList();
            var destinatarios = new Dictionary<int, List<Cartao>>();
            void Incluir(int usuarioId, IEnumerable<Cartao> cartoes)
            {
                if (!destinatarios.TryGetValue(usuarioId, out var lista)) destinatarios[usuarioId] = lista = [];
                lista.AddRange(cartoes.Where(c => lista.All(x => x.Id != c.Id)));
            }
            if (p.Destinatarios.HasFlag(DestinatariosAutomacao.Desenvolvedores) || p.Destinatarios.HasFlag(DestinatariosAutomacao.DesenvolvedorPrincipal))
                foreach (var dev in ativos.SelectMany(c => c.Desenvolvedores.Select(d => d.UsuarioId)).Distinct())
                    Incluir(dev, ativos.Where(c => c.Desenvolvedores.Any(d => d.UsuarioId == dev)));
            if (p.Destinatarios.HasFlag(DestinatariosAutomacao.Solicitante))
                foreach (var solicitante in ativos.Where(c => c.SolicitanteId.HasValue).Select(c => c.SolicitanteId!.Value).Distinct())
                    Incluir(solicitante, ativos.Where(c => c.SolicitanteId == solicitante));
            if (p.Destinatarios.HasFlag(DestinatariosAutomacao.UsuariosEspecificos))
                foreach (var id in p.DestinatarioIds) Incluir(id, ativos);

            var usuarios = foto.Usuarios.Where(u => destinatarios.ContainsKey(u.Id) && u.ReceberEmailsAutomacao && !string.IsNullOrWhiteSpace(u.Email)).ToList();
            var enviados = new List<string>();
            foreach (var usuario in usuarios.Where(u => destinatarios[u.Id].Count > 0))
            {
                if (await EmailsNaUltimaHoraAsync(ctx) >= LimiteEmailsPorHora)
                    throw new InvalidOperationException($"limite de {LimiteEmailsPorHora} e-mails por hora desta regra atingido após {enviados.Count} envio(s).");
                var corpo = MontarResumoHtml(usuario, destinatarios[usuario.Id], foto, ctx.Regra.Nome);
                await emailSender.EnviarAsync(usuario.Email, null, $"Resumo do quadro — {DateTime.Today:dd/MM/yyyy}", corpo);
                ctx.EmailsEnviados++;
                enviados.Add(usuario.Nome);
            }
            return enviados.Count == 0 ? null : $"resumo enviado para {string.Join(", ", enviados)}";
        }

        private string MontarResumoHtml(Usuario usuario, List<Cartao> cartoes, FotoQuadro foto, string regra)
        {
            var hoje = DateTime.Today;
            var secoes = new (string Titulo, List<Cartao> Itens)[]
            {
                ("Atrasados", cartoes.Where(c => c.Prazo.HasValue && c.Prazo.Value.ToLocalTime().Date < hoje).ToList()),
                ("Vencem nos próximos 3 dias", cartoes.Where(c => c.Prazo.HasValue && (c.Prazo.Value.ToLocalTime().Date - hoje).Days is >= 0 and <= 3).ToList()),
                ("Bloqueados", cartoes.Where(foto.EstaBloqueado).ToList()),
                ("Com conflito de datas", cartoes.Where(c => foto.Conflitos(c).Count > 0).ToList()),
                ("Parados há 5 dias ou mais", cartoes.Where(c => foto.DiasNaLista(c) >= 5).ToList()),
                ("Todos os cartões ativos", cartoes)
            };
            var html = new StringBuilder();
            html.Append($"<p>Olá, {WebUtility.HtmlEncode(usuario.Nome)}. Este é o seu resumo de {hoje:dd/MM/yyyy}.</p>");
            foreach (var (titulo, itens) in secoes.Where(s => s.Itens.Count > 0))
            {
                html.Append($"<h3 style=\"margin:16px 0 6px\">{WebUtility.HtmlEncode(titulo)} ({itens.Count})</h3><ul>");
                foreach (var cartao in itens.OrderBy(c => c.Prazo ?? DateTime.MaxValue))
                {
                    var link = urlBase.LinkCartao(foto.QuadroId, cartao.Id);
                    var prazo = cartao.Prazo.HasValue ? $" — prazo {cartao.Prazo.Value.ToLocalTime():dd/MM/yyyy}" : "";
                    html.Append($"<li><a href=\"{WebUtility.HtmlEncode(link)}\">{WebUtility.HtmlEncode(cartao.Titulo)}</a> ({WebUtility.HtmlEncode(foto.NomeLista(cartao.ListaId))}){prazo}</li>");
                }
                html.Append("</ul>");
            }
            html.Append($"<p style=\"color:#777;font-size:12px\">Enviado pela automação \"{WebUtility.HtmlEncode(regra)}\".</p>");
            return html.ToString();
        }
    }

    #endregion

    #region Services/Automacoes/ProcessamentoAutomacoes.cs

    /// <summary>Fila em memória dos eventos de automação gerados ao gravar alterações.</summary>
    public sealed class FilaEventosAutomacao : IFilaEventosAutomacao
    {
        private readonly Channel<EventoAutomacao> _canal = Channel.CreateUnbounded<EventoAutomacao>(new UnboundedChannelOptions { SingleReader = true });

        public void Enfileirar(EventoAutomacao evento) => _canal.Writer.TryWrite(evento);

        public IAsyncEnumerable<EventoAutomacao> LerTodos(CancellationToken ct) => _canal.Reader.ReadAllAsync(ct);
    }

    /// <summary>
    /// Consome a fila em memória: eventos de pessoas vão para a espera; eventos gerados por automações
    /// (continuação de uma cadeia) rodam na hora. Cada evento roda como o usuário que o gerou.
    /// </summary>
    public sealed class ProcessadorEventosAutomacao(FilaEventosAutomacao fila, IServiceScopeFactory scopeFactory, IConfiguration configuracao,
        ILogger<ProcessadorEventosAutomacao> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var evento in fila.LerTodos(stoppingToken))
            {
                try
                {
                    var atraso = TimeSpan.FromMinutes(Math.Max(0, configuracao.GetValue("Automacao:AtrasoMinutos", 10)));
                    using var escopo = scopeFactory.CreateScope();
                    if (evento.Profundidade == 0 && atraso > TimeSpan.Zero)
                    {
                        await escopo.ServiceProvider.GetRequiredService<AgendaEventosAutomacao>().AgendarAsync(evento, atraso);
                        continue;
                    }
                    await ExecutarEventoAsync(escopo.ServiceProvider, evento);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Falha ao processar o evento de automação {Gatilho} do cartão {Cartao}.", evento.Gatilho, evento.CartaoId);
                }
            }
        }

        public static async Task ExecutarEventoAsync(IServiceProvider servicos, EventoAutomacao evento)
        {
            servicos.GetRequiredService<SessaoUsuario>().UsuarioId = evento.UsuarioId;
            using (ContextoAutomacao.Entrar(evento.Profundidade, evento.RegrasNaCadeia))
                await servicos.GetRequiredService<MotorAutomacoes>().ProcessarEventoAsync(evento);
        }
    }

    /// <summary>
    /// Verificação periódica: executa os eventos cuja espera terminou e os gatilhos de tempo
    /// (prazos, cartões parados, agendamentos e sprints). O intervalo é relido a cada ciclo.
    /// </summary>
    public sealed class AgendadorAutomacoes(IServiceScopeFactory scopeFactory, IConfiguration configuracao, ILogger<AgendadorAutomacoes> logger)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ExecutarCicloAsync();
                var intervalo = TimeSpan.FromSeconds(Math.Clamp(configuracao.GetValue("Automacao:IntervaloVerificacaoSegundos", 60), 15, 3600));
                try { await Task.Delay(intervalo, stoppingToken); }
                catch (TaskCanceledException) { break; }
            }
        }

        private async Task ExecutarCicloAsync()
        {
            try
            {
                List<EventoAutomacao> vencidos;
                using (var escopo = scopeFactory.CreateScope())
                    vencidos = await escopo.ServiceProvider.GetRequiredService<AgendaEventosAutomacao>().RetirarVencidosAsync(DateTime.UtcNow);
                foreach (var evento in vencidos)
                {
                    try
                    {
                        using var escopo = scopeFactory.CreateScope();
                        await ProcessadorEventosAutomacao.ExecutarEventoAsync(escopo.ServiceProvider, evento);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Falha ao executar o evento adiado {Gatilho} do cartão {Cartao}.", evento.Gatilho, evento.CartaoId);
                    }
                }

                using (var escopo = scopeFactory.CreateScope())
                {
                    escopo.ServiceProvider.GetRequiredService<SessaoUsuario>().UsuarioId = configuracao.GetValue("Automacao:UsuarioSistemaId", 1);
                    await escopo.ServiceProvider.GetRequiredService<MotorAutomacoes>().ProcessarGatilhosDeTempoAsync(DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha na verificação periódica de automações.");
            }
        }
    }

    #endregion
}

namespace KanbanDemandas.Web.Services.Integracoes
{
    #region Services/Integracoes/GitHubWebhookService.cs

    public sealed record ResultadoWebhookGitHub(int Vinculos, int Eventos, string Mensagem);

    /// <summary>
    /// Recebe webhooks do GitHub (push, create e pull_request) e vincula aos cartões citados pelo código
    /// (ex.: KB-123) em mensagens de commit, nomes de branch, títulos e descrições de pull requests.
    /// </summary>
    public sealed class GitHubWebhookService(KanbanDbContext db, IFilaEventosAutomacao fila, IConfiguration configuracao, ILogger<GitHubWebhookService> logger)
    {
        private string? Prefixo => configuracao["Cartao:PrefixoCodigo"];
        private int UsuarioSistemaId => configuracao.GetValue("Automacao:UsuarioSistemaId", 1);

        /// <summary>Confere o cabeçalho X-Hub-Signature-256 (HMAC-SHA256 do corpo com o segredo configurado).</summary>
        public static bool AssinaturaValida(string corpo, string? assinatura, string segredo)
        {
            if (string.IsNullOrWhiteSpace(assinatura) || !assinatura.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)) return false;
            var esperado = HMACSHA256.HashData(Encoding.UTF8.GetBytes(segredo), Encoding.UTF8.GetBytes(corpo));
            byte[] recebido;
            try { recebido = Convert.FromHexString(assinatura["sha256=".Length..]); }
            catch (FormatException) { return false; }
            return CryptographicOperations.FixedTimeEquals(esperado, recebido);
        }

        public async Task<ResultadoWebhookGitHub> ProcessarAsync(string tipoEvento, string corpo)
        {
            using var documento = JsonDocument.Parse(corpo);
            var raiz = documento.RootElement;
            var repositorio = Texto(raiz, "repository", "full_name") ?? "?";
            var urlRepositorio = Texto(raiz, "repository", "html_url") ?? "";
            var novos = new List<(VinculoGit Vinculo, TipoGatilho? Gatilho)>();

            switch (tipoEvento)
            {
                case "ping":
                    return new ResultadoWebhookGitHub(0, 0, "pong");

                case "create" when Texto(raiz, "ref_type") == "branch":
                {
                    var branch = Texto(raiz, "ref") ?? "";
                    foreach (var id in CodigoCartao.Extrair(branch, Prefixo))
                        novos.Add((await RegistrarAsync(id, TipoVinculoGit.Branch, repositorio, branch, $"Branch {branch}",
                            $"{urlRepositorio}/tree/{Uri.EscapeDataString(branch)}", Texto(raiz, "sender", "login"), null), null));
                    break;
                }

                case "push":
                {
                    var branch = (Texto(raiz, "ref") ?? "").Replace("refs/heads/", "");
                    var idsBranch = CodigoCartao.Extrair(branch, Prefixo);
                    foreach (var id in idsBranch)
                        novos.Add((await RegistrarAsync(id, TipoVinculoGit.Branch, repositorio, branch, $"Branch {branch}",
                            $"{urlRepositorio}/tree/{Uri.EscapeDataString(branch)}", Texto(raiz, "pusher", "name"), null), null));
                    if (raiz.TryGetProperty("commits", out var commits) && commits.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var commit in commits.EnumerateArray())
                        {
                            var mensagem = Texto(commit, "message") ?? "";
                            var sha = Texto(commit, "id") ?? "";
                            var titulo = mensagem.Split('\n')[0];
                            foreach (var id in CodigoCartao.Extrair(mensagem, Prefixo).Union(idsBranch))
                                novos.Add((await RegistrarAsync(id, TipoVinculoGit.Commit, repositorio, sha, $"{sha[..Math.Min(7, sha.Length)]} {titulo}",
                                    Texto(commit, "url") ?? "", Texto(commit, "author", "username") ?? Texto(commit, "author", "name"), null), TipoGatilho.CommitVinculado));
                        }
                    }
                    break;
                }

                case "pull_request":
                {
                    var acao = Texto(raiz, "action") ?? "";
                    if (!raiz.TryGetProperty("pull_request", out var pr)) break;
                    var numero = pr.TryGetProperty("number", out var n) ? n.GetInt32().ToString() : "?";
                    var mergeado = pr.TryGetProperty("merged", out var m) && m.ValueKind == JsonValueKind.True;
                    var estado = mergeado ? EstadoPullRequest.Mergeado : Texto(pr, "state") == "closed" ? EstadoPullRequest.Fechado : EstadoPullRequest.Aberto;
                    var textoBusca = $"{Texto(pr, "title")}\n{Texto(pr, "body")}\n{Texto(pr, "head", "ref")}";
                    TipoGatilho? gatilho = acao switch
                    {
                        "opened" or "reopened" => TipoGatilho.PullRequestAberto,
                        "closed" when mergeado => TipoGatilho.PullRequestMergeado,
                        _ => null
                    };
                    foreach (var id in CodigoCartao.Extrair(textoBusca, Prefixo))
                        novos.Add((await RegistrarAsync(id, TipoVinculoGit.PullRequest, repositorio, numero, $"#{numero} {Texto(pr, "title")}",
                            Texto(pr, "html_url") ?? "", Texto(pr, "user", "login"), estado), gatilho));
                    break;
                }

                default:
                    return new ResultadoWebhookGitHub(0, 0, $"evento '{tipoEvento}' ignorado");
            }

            var validos = novos.Where(x => x.Vinculo.CartaoId > 0).ToList();
            await db.SaveChangesAsync();
            var eventos = 0;
            foreach (var (vinculo, gatilho) in validos.Where(x => x.Gatilho.HasValue).DistinctBy(x => (x.Vinculo.CartaoId, x.Gatilho)))
            {
                fila.Enfileirar(new EventoAutomacao(gatilho!.Value, vinculo.CartaoId, UsuarioSistemaId, 0, []));
                eventos++;
            }
            logger.LogInformation("Webhook GitHub {Evento} de {Repositorio}: {Vinculos} vínculo(s), {Eventos} evento(s).", tipoEvento, repositorio, validos.Count, eventos);
            return new ResultadoWebhookGitHub(validos.Count, eventos, "ok");
        }

        /// <summary>Cria ou atualiza o vínculo; cartão inexistente devolve um vínculo com CartaoId 0 (descartado).</summary>
        private async Task<VinculoGit> RegistrarAsync(int cartaoId, TipoVinculoGit tipo, string repositorio, string identificador, string titulo,
            string url, string? autor, EstadoPullRequest? estado)
        {
            if (!await db.Cartoes.AnyAsync(c => c.Id == cartaoId && !c.Excluido)) return new VinculoGit();
            var vinculo = db.VinculosGit.Local.FirstOrDefault(v => v.CartaoId == cartaoId && v.Tipo == tipo && v.Repositorio == repositorio && v.Identificador == identificador)
                ?? await db.VinculosGit.FirstOrDefaultAsync(v => v.CartaoId == cartaoId && v.Tipo == tipo && v.Repositorio == repositorio && v.Identificador == identificador);
            if (vinculo is null)
            {
                vinculo = new VinculoGit
                {
                    CartaoId = cartaoId, Tipo = tipo, Repositorio = Limitar(repositorio, 300), Identificador = Limitar(identificador, 300), CriadoEm = DateTime.UtcNow
                };
                db.VinculosGit.Add(vinculo);
            }
            vinculo.Titulo = Limitar(titulo, 500);
            vinculo.Url = Limitar(url, 1000);
            vinculo.Autor = autor is null ? null : Limitar(autor, 200);
            vinculo.Estado = estado ?? vinculo.Estado;
            vinculo.AtualizadoEm = DateTime.UtcNow;
            return vinculo;
        }

        private static string Limitar(string texto, int maximo) => texto.Length <= maximo ? texto : texto[..maximo];

        private static string? Texto(JsonElement elemento, params string[] caminho)
        {
            foreach (var parte in caminho)
            {
                if (elemento.ValueKind != JsonValueKind.Object || !elemento.TryGetProperty(parte, out elemento)) return null;
            }
            return elemento.ValueKind == JsonValueKind.String ? elemento.GetString() : null;
        }
    }

    #endregion
}

namespace KanbanDemandas.Web.Services.Portal
{
    #region Services/Portal/NotificadorSolicitante.cs

    /// <summary>
    /// Mantém o solicitante informado por e-mail: mudança de status público e respostas públicas do time.
    /// Roda no processamento de eventos (depois da espera das automações), então um vai-e-volta não gera e-mail.
    /// </summary>
    public sealed class NotificadorSolicitante(KanbanDbContext db, IEmailSender emailSender, IConfiguration configuracao, ILogger<NotificadorSolicitante> logger)
    {
        private bool Ativo => configuracao.GetValue("Portal:NotificarSolicitante", true);
        private string UrlPortal => (configuracao["Portal:UrlBase"] ?? "").TrimEnd('/');
        private string? Prefixo => configuracao["Cartao:PrefixoCodigo"];

        public string LinkSolicitacao(int cartaoId) => string.IsNullOrEmpty(UrlPortal) ? "" : $"{UrlPortal}/solicitacoes/{cartaoId}";

        public async Task AoEntrarNaListaAsync(EventoAutomacao evento)
        {
            if (!Ativo) return;
            var cartao = await db.Cartoes.AsNoTracking().Include(c => c.Solicitante).Include(c => c.Lista)
                .FirstOrDefaultAsync(c => c.Id == evento.CartaoId && !c.Excluido);
            if (cartao?.Solicitante is not { } solicitante || cartao.ListaId != evento.ListaId) return;

            var listas = await db.Listas.AsNoTracking().Where(l => l.QuadroId == cartao.Lista.QuadroId).ToListAsync();
            var entradas = await db.HistoricoAtividades.AsNoTracking().Where(h => h.CartaoId == cartao.Id).ToListAsync();
            var historico = StatusPortal.Historico(cartao, entradas, listas, configuracao["Portal:StatusInicial"]);
            if (historico.Count < 2) return;

            // Só avisa se a mudança mais recente de status público foi causada por esta entrada na lista.
            var ultima = historico[^1];
            var entradaAtual = entradas.Where(h => h.ListaDestinoId == cartao.ListaId).MaxBy(h => h.OcorridoEm);
            if (entradaAtual is null || ultima.Em != entradaAtual.OcorridoEm) return;

            var codigo = CodigoCartao.Formatar(cartao.Id, Prefixo);
            await EnviarAsync(solicitante, $"[{codigo}] Sua solicitação agora está: {ultima.Status}",
                $"<p>Olá, {WebUtility.HtmlEncode(solicitante.Nome)}.</p>" +
                $"<p>A solicitação <b>{codigo} — {WebUtility.HtmlEncode(cartao.Titulo)}</b> mudou de <b>{WebUtility.HtmlEncode(historico[^2].Status)}</b> " +
                $"para <b>{WebUtility.HtmlEncode(ultima.Status)}</b>.</p>" + BotaoLink(cartao.Id));
        }

        /// <summary>Chamado quando alguém do time publica um comentário visível ao solicitante.</summary>
        public async Task AoResponderAsync(int cartaoId, int autorId, string texto)
        {
            if (!Ativo) return;
            var cartao = await db.Cartoes.AsNoTracking().Include(c => c.Solicitante).FirstOrDefaultAsync(c => c.Id == cartaoId && !c.Excluido);
            if (cartao?.Solicitante is not { } solicitante || solicitante.Id == autorId) return;
            var autor = await db.Usuarios.AsNoTracking().Where(u => u.Id == autorId).Select(u => u.Nome).FirstOrDefaultAsync() ?? "O time";
            var codigo = CodigoCartao.Formatar(cartao.Id, Prefixo);
            await EnviarAsync(solicitante, $"[{codigo}] Nova mensagem sobre sua solicitação",
                $"<p>Olá, {WebUtility.HtmlEncode(solicitante.Nome)}.</p>" +
                $"<p><b>{WebUtility.HtmlEncode(autor)}</b> escreveu sobre <b>{codigo} — {WebUtility.HtmlEncode(cartao.Titulo)}</b>:</p>" +
                $"<blockquote style=\"border-left:3px solid #1565c0;margin:0;padding:4px 12px;color:#333\">{WebUtility.HtmlEncode(texto).Replace("\n", "<br>")}</blockquote>" +
                BotaoLink(cartao.Id));
        }

        private string BotaoLink(int cartaoId)
        {
            var link = LinkSolicitacao(cartaoId);
            return string.IsNullOrEmpty(link) ? "" : $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Acompanhar no portal de solicitações</a></p>";
        }

        private async Task EnviarAsync(Usuario destinatario, string assunto, string corpo)
        {
            if (!destinatario.Ativo || !destinatario.ReceberEmailsAutomacao || string.IsNullOrWhiteSpace(destinatario.Email)) return;
            try
            {
                await emailSender.EnviarAsync(destinatario.Email, null, assunto, corpo);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao avisar o solicitante {Usuario} por e-mail.", destinatario.Id);
            }
        }
    }

    #endregion
}

namespace KanbanDemandas.Web.Services.Relatorios
{
    #region Services/Relatorios/RelatoriosService.cs

    public sealed record FiltroRelatorio(int QuadroId, DateTime De, DateTime Ate, int? SistemaId, int? DesenvolvedorId);

    public sealed record LinhaFluxo(DateTime Dia, IReadOnlyDictionary<string, int> PorLista);
    public sealed record LinhaVazao(DateTime Semana, int Cartoes, decimal Pontos);
    public sealed record LinhaCiclo(int CartaoId, string Titulo, string Sistema, string Desenvolvedores, DateTime CriadoEm, DateTime? IniciadoEm, DateTime ConcluidoEm, double LeadTimeDias, double? CicloDias);
    public sealed record LinhaEnvelhecimento(int CartaoId, string Titulo, string Lista, string Desenvolvedores, int DiasNaLista, int IdadeDias, DateTime? Prazo, bool Atrasado);
    public sealed record LinhaPrazo(string Grupo, int Entregues, int NoPrazo, int Atrasados, double PercentualNoPrazo, double AtrasoMedioDias);
    public sealed record LinhaDesenvolvedor(string Desenvolvedor, int Concluidos, decimal Pontos, double LeadTimeMedioDias, int Ativos, int AtivosAtrasados);
    public sealed record Percentis(double P50, double P85, double P95, int Amostras);
    public sealed record PrevisaoQuando(DateTime P50, DateTime P85, DateTime P95, int Restantes, int SemanasDeHistorico);
    public sealed record PrevisaoQuantos(int P50, int P85, int P95, DateTime DataAlvo);

    /// <summary>Dados de um quadro prontos para os relatórios, reconstruídos a partir do histórico de movimentações.</summary>
    public sealed class DadosRelatorio
    {
        public required FiltroRelatorio Filtro { get; init; }
        public required List<Lista> Listas { get; init; }
        public required List<Cartao> Cartoes { get; init; }
        public required ILookup<int, HistoricoAtividade> Entradas { get; init; }
        public required Dictionary<int, string> Usuarios { get; init; }

        /// <summary>Cartões não excluídos (o fluxo acumulado usa todos, pois os excluídos existiam antes da exclusão).</summary>
        public IEnumerable<Cartao> Validos => Cartoes.Where(c => !c.Excluido);

        public bool EhConcluida(int listaId) => ListasQuadro.EhConcluido(listaId, Listas);
        public bool EhBacklog(int listaId) => Listas.FirstOrDefault(l => l.Id == listaId)?.EhBacklog == true;

        /// <summary>Primeira entrada numa lista "Concluído" (se o cartão estiver lá sem histórico, usa a última alteração).</summary>
        public DateTime? ConcluidoEm(Cartao cartao)
        {
            var entrada = Entradas[cartao.Id].Where(h => h.ListaDestinoId.HasValue && EhConcluida(h.ListaDestinoId.Value)).MinBy(h => h.OcorridoEm);
            if (entrada is not null) return entrada.OcorridoEm;
            return EhConcluida(cartao.ListaId) ? cartao.AlteradoEm ?? cartao.CriadoEm : null;
        }

        /// <summary>Quando o trabalho começou: primeira entrada numa lista que não é backlog nem conclusão.</summary>
        public DateTime? IniciadoEm(Cartao cartao)
            => Entradas[cartao.Id]
                .Where(h => h.ListaDestinoId.HasValue && !EhBacklog(h.ListaDestinoId.Value) && !EhConcluida(h.ListaDestinoId.Value)
                    && h.Tipo == TipoHistoricoAtividade.CartaoMovido)
                .MinBy(h => h.OcorridoEm)?.OcorridoEm;

        /// <summary>Lista em que o cartão estava no fim do dia (pela última entrada registrada até então).</summary>
        public int? ListaNoDia(Cartao cartao, DateTime fimDoDia)
        {
            if (cartao.CriadoEm > fimDoDia) return null;
            if (cartao.Excluido && cartao.ExcluidoEm.HasValue && cartao.ExcluidoEm <= fimDoDia) return null;
            var ultima = Entradas[cartao.Id].Where(h => h.OcorridoEm <= fimDoDia && h.ListaDestinoId.HasValue).MaxBy(h => h.OcorridoEm);
            return ultima?.ListaDestinoId ?? cartao.ListaId;
        }

        public string Devs(Cartao cartao) => string.Join(", ", cartao.Desenvolvedores.OrderByDescending(d => d.Principal)
            .Select(d => Usuarios.GetValueOrDefault(d.UsuarioId)).OfType<string>());

        public string NomeLista(int listaId) => Listas.FirstOrDefault(l => l.Id == listaId) is { } lista ? ListasQuadro.NomeCompleto(lista, Listas) : "?";
    }

    /// <summary>Relatórios de fluxo (CFD, vazão, tempos, envelhecimento, previsão, prazos e pessoas).</summary>
    public sealed class RelatoriosService(KanbanDbContext db)
    {
        public const int MaximoSeriesFluxo = 8;

        public async Task<DadosRelatorio> CarregarAsync(FiltroRelatorio filtro)
        {
            var listas = await db.Listas.AsNoTracking().IgnoreQueryFilters().Where(l => l.QuadroId == filtro.QuadroId).ToListAsync();
            var idsListas = listas.Select(l => l.Id).ToList();
            listas = listas.Where(l => !l.Excluido).ToList();

            var consulta = db.Cartoes.AsNoTracking().IgnoreQueryFilters().Include(c => c.Desenvolvedores).Include(c => c.Sistema)
                .Where(c => idsListas.Contains(c.ListaId));
            if (filtro.SistemaId.HasValue) consulta = consulta.Where(c => c.SistemaId == filtro.SistemaId);
            if (filtro.DesenvolvedorId.HasValue) consulta = consulta.Where(c => c.Desenvolvedores.Any(d => d.UsuarioId == filtro.DesenvolvedorId));
            var cartoes = await consulta.AsSplitQuery().ToListAsync();
            var idsCartoes = cartoes.Select(c => c.Id).ToList();

            var entradas = await db.HistoricoAtividades.AsNoTracking()
                .Where(h => idsCartoes.Contains(h.CartaoId) && h.ListaDestinoId != null
                    && (h.Tipo == TipoHistoricoAtividade.CartaoMovido || h.Tipo == TipoHistoricoAtividade.CartaoCriado))
                .ToListAsync();

            return new DadosRelatorio
            {
                Filtro = filtro,
                Listas = listas,
                Cartoes = cartoes,
                Entradas = entradas.ToLookup(h => h.CartaoId),
                Usuarios = await db.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.Nome)
            };
        }

        /// <summary>
        /// Fluxo acumulado: cartões por lista no fim de cada dia do período. Com mais de 60 dias, amostra por semana.
        /// Listas além do limite de cores seguras viram "Outras".
        /// </summary>
        public static (List<string> Series, List<LinhaFluxo> Linhas) Fluxo(DadosRelatorio dados)
        {
            var folhas = ListasQuadro.FolhasEmOrdem(dados.Listas);
            var nomes = folhas.Take(MaximoSeriesFluxo - (folhas.Count > MaximoSeriesFluxo ? 1 : 0)).ToDictionary(l => l.Id, l => dados.NomeLista(l.Id));
            var series = nomes.Values.ToList();
            if (folhas.Count > MaximoSeriesFluxo) series.Add("Outras");

            var passo = (dados.Filtro.Ate - dados.Filtro.De).TotalDays > 60 ? 7 : 1;
            var linhas = new List<LinhaFluxo>();
            for (var dia = dados.Filtro.De.Date; dia <= dados.Filtro.Ate.Date; dia = dia.AddDays(passo))
            {
                var fim = dia.AddDays(1).ToUniversalTime();
                var contagem = series.ToDictionary(s => s, _ => 0);
                foreach (var cartao in dados.Cartoes)
                {
                    if (dados.ListaNoDia(cartao, fim) is not int listaId) continue;
                    var serie = nomes.TryGetValue(listaId, out var nome) ? nome : folhas.Any(f => f.Id == listaId) ? "Outras" : null;
                    if (serie is not null && contagem.ContainsKey(serie)) contagem[serie]++;
                }
                linhas.Add(new LinhaFluxo(dia, contagem));
            }
            return (series, linhas);
        }

        private static DateTime InicioDaSemana(DateTime data) => data.Date.AddDays(-(((int)data.DayOfWeek + 6) % 7));

        public static List<LinhaVazao> Vazao(DadosRelatorio dados)
        {
            var concluidos = dados.Validos.Select(c => (Cartao: c, Em: dados.ConcluidoEm(c)?.ToLocalTime()))
                .Where(x => x.Em.HasValue && x.Em.Value.Date >= dados.Filtro.De.Date && x.Em.Value.Date <= dados.Filtro.Ate.Date).ToList();
            var linhas = new List<LinhaVazao>();
            for (var semana = InicioDaSemana(dados.Filtro.De); semana <= dados.Filtro.Ate.Date; semana = semana.AddDays(7))
            {
                var daSemana = concluidos.Where(x => InicioDaSemana(x.Em!.Value) == semana).ToList();
                linhas.Add(new LinhaVazao(semana, daSemana.Count, daSemana.Sum(x => x.Cartao.Estimativa ?? 0)));
            }
            return linhas;
        }

        public static List<LinhaCiclo> Ciclos(DadosRelatorio dados)
            => dados.Validos
                .Select(c => (Cartao: c, Concluido: dados.ConcluidoEm(c), Iniciado: dados.IniciadoEm(c)))
                .Where(x => x.Concluido.HasValue && x.Concluido.Value.ToLocalTime().Date >= dados.Filtro.De.Date && x.Concluido.Value.ToLocalTime().Date <= dados.Filtro.Ate.Date)
                .Select(x => new LinhaCiclo(x.Cartao.Id, x.Cartao.Titulo, x.Cartao.Sistema?.Nome ?? "", dados.Devs(x.Cartao),
                    x.Cartao.CriadoEm.ToLocalTime(), x.Iniciado?.ToLocalTime(), x.Concluido!.Value.ToLocalTime(),
                    Math.Max(0, (x.Concluido.Value - x.Cartao.CriadoEm).TotalDays),
                    x.Iniciado.HasValue && x.Iniciado <= x.Concluido ? (x.Concluido.Value - x.Iniciado.Value).TotalDays : null))
                .OrderByDescending(l => l.LeadTimeDias).ToList();

        public static Percentis CalcularPercentis(IReadOnlyCollection<double> valores)
        {
            if (valores.Count == 0) return new Percentis(0, 0, 0, 0);
            var ordenados = valores.OrderBy(v => v).ToArray();
            double P(double p)
            {
                var posicao = (ordenados.Length - 1) * p;
                var baixo = (int)Math.Floor(posicao);
                var alto = (int)Math.Ceiling(posicao);
                return ordenados[baixo] + (ordenados[alto] - ordenados[baixo]) * (posicao - baixo);
            }
            return new Percentis(P(0.5), P(0.85), P(0.95), ordenados.Length);
        }

        /// <summary>Distribuição em faixas de dias (para o histograma do tempo de ciclo).</summary>
        public static List<(string Faixa, int Quantidade)> Histograma(IReadOnlyCollection<double> dias)
        {
            (string Rotulo, double Ate)[] faixas = [("até 1 d", 1), ("1–3 d", 3), ("3–7 d", 7), ("7–14 d", 14), ("14–30 d", 30), ("30–60 d", 60), ("60+ d", double.MaxValue)];
            var resultado = new List<(string, int)>();
            double anterior = -1;
            foreach (var (rotulo, ate) in faixas)
            {
                resultado.Add((rotulo, dias.Count(d => d > anterior && d <= ate)));
                anterior = ate;
            }
            return resultado;
        }

        public static List<LinhaEnvelhecimento> Envelhecimento(DadosRelatorio dados)
        {
            var hoje = DateTime.Today;
            return dados.Cartoes.Where(c => !c.Excluido && !dados.EhConcluida(c.ListaId) && dados.Listas.Any(l => l.Id == c.ListaId))
                .Select(c =>
                {
                    var entrada = dados.Entradas[c.Id].Where(h => h.ListaDestinoId == c.ListaId).MaxBy(h => h.OcorridoEm)?.OcorridoEm ?? c.CriadoEm;
                    var prazo = c.Prazo?.ToLocalTime().Date;
                    return new LinhaEnvelhecimento(c.Id, c.Titulo, dados.NomeLista(c.ListaId), dados.Devs(c),
                        (hoje - entrada.ToLocalTime().Date).Days, (hoje - c.CriadoEm.ToLocalTime().Date).Days, prazo, prazo < hoje);
                })
                .OrderByDescending(l => l.DiasNaLista).ToList();
        }

        public static List<LinhaPrazo> Prazos(DadosRelatorio dados, Func<Cartao, IEnumerable<string>> agrupar)
        {
            var entregues = dados.Validos
                .Select(c => (Cartao: c, Concluido: dados.ConcluidoEm(c)))
                .Where(x => x.Cartao.Prazo.HasValue && x.Concluido.HasValue
                    && x.Concluido.Value.ToLocalTime().Date >= dados.Filtro.De.Date && x.Concluido.Value.ToLocalTime().Date <= dados.Filtro.Ate.Date)
                .Select(x => (x.Cartao, Atraso: (x.Concluido!.Value.ToLocalTime().Date - x.Cartao.Prazo!.Value.ToLocalTime().Date).TotalDays))
                .ToList();
            return entregues
                .SelectMany(x => agrupar(x.Cartao).Select(grupo => (Grupo: grupo, x.Atraso)))
                .GroupBy(x => x.Grupo)
                .Select(g =>
                {
                    var atrasados = g.Where(x => x.Atraso > 0).ToList();
                    return new LinhaPrazo(g.Key, g.Count(), g.Count() - atrasados.Count, atrasados.Count,
                        100.0 * (g.Count() - atrasados.Count) / g.Count(), atrasados.Count == 0 ? 0 : atrasados.Average(x => x.Atraso));
                })
                .OrderBy(l => l.PercentualNoPrazo).ToList();
        }

        public static List<LinhaDesenvolvedor> Desenvolvedores(DadosRelatorio dados)
        {
            var ciclos = Ciclos(dados).ToDictionary(c => c.CartaoId);
            var hoje = DateTime.Today;
            return dados.Validos.SelectMany(c => c.Desenvolvedores.Select(d => (UsuarioId: d.UsuarioId, Cartao: c)))
                .GroupBy(x => x.UsuarioId)
                .Select(g =>
                {
                    var concluidos = g.Where(x => ciclos.ContainsKey(x.Cartao.Id)).ToList();
                    var ativos = g.Where(x => !x.Cartao.Excluido && !dados.EhConcluida(x.Cartao.ListaId)).ToList();
                    return new LinhaDesenvolvedor(dados.Usuarios.GetValueOrDefault(g.Key) ?? $"#{g.Key}", concluidos.Count,
                        concluidos.Sum(x => x.Cartao.Estimativa ?? 0),
                        concluidos.Count == 0 ? 0 : concluidos.Average(x => ciclos[x.Cartao.Id].LeadTimeDias),
                        ativos.Count, ativos.Count(x => x.Cartao.Prazo.HasValue && x.Cartao.Prazo.Value.ToLocalTime().Date < hoje));
                })
                .OrderByDescending(l => l.Concluidos).ThenBy(l => l.Desenvolvedor).ToList();
        }

        /// <summary>Entregas por semana nas últimas <paramref name="semanas"/> semanas completas (base da previsão).</summary>
        public static int[] AmostrasSemanais(DadosRelatorio dados, int semanas)
        {
            var atual = InicioDaSemana(DateTime.Today);
            var concluidos = dados.Validos.Select(dados.ConcluidoEm).OfType<DateTime>().Select(d => InicioDaSemana(d.ToLocalTime())).ToList();
            return Enumerable.Range(1, semanas).Select(i => concluidos.Count(s => s == atual.AddDays(-7 * i))).ToArray();
        }

        /// <summary>
        /// Monte Carlo: sorteia semanas passadas (com reposição) até zerar os cartões restantes.
        /// P85 = em 85% das simulações terminou até essa data.
        /// </summary>
        public static PrevisaoQuando? PreverQuando(int[] amostras, int restantes, int simulacoes = 10_000)
        {
            if (restantes <= 0 || amostras.Length == 0 || amostras.All(a => a == 0)) return null;
            var aleatorio = new Random(42);
            var semanas = new double[simulacoes];
            for (var i = 0; i < simulacoes; i++)
            {
                var falta = restantes;
                var n = 0;
                while (falta > 0 && n < 520)
                {
                    falta -= amostras[aleatorio.Next(amostras.Length)];
                    n++;
                }
                semanas[i] = n;
            }
            var p = CalcularPercentis(semanas);
            var hoje = DateTime.Today;
            return new PrevisaoQuando(hoje.AddDays(7 * Math.Ceiling(p.P50)), hoje.AddDays(7 * Math.Ceiling(p.P85)), hoje.AddDays(7 * Math.Ceiling(p.P95)), restantes, amostras.Length);
        }

        /// <summary>Quantos cartões ficam prontos até a data. P85 = quantidade atingida ou superada em 85% das simulações.</summary>
        public static PrevisaoQuantos? PreverQuantos(int[] amostras, DateTime dataAlvo, int simulacoes = 10_000)
        {
            var semanas = (int)Math.Floor((dataAlvo.Date - DateTime.Today).TotalDays / 7);
            if (semanas <= 0 || amostras.Length == 0 || amostras.All(a => a == 0)) return null;
            var aleatorio = new Random(42);
            var totais = new double[simulacoes];
            for (var i = 0; i < simulacoes; i++)
            {
                var soma = 0;
                for (var s = 0; s < semanas; s++) soma += amostras[aleatorio.Next(amostras.Length)];
                totais[i] = soma;
            }
            // Para "quantos", a leitura conservadora é o percentil baixo: 85% das simulações entregaram pelo menos isso.
            var ordenados = totais.OrderBy(t => t).ToArray();
            int Q(double p) => (int)ordenados[(int)Math.Floor((ordenados.Length - 1) * p)];
            return new PrevisaoQuantos(Q(0.5), Q(0.15), Q(0.05), dataAlvo);
        }
    }

    #endregion
}
