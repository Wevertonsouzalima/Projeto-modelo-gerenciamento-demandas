using KanbanDemandas.Core.Regras;
using KanbanDemandas.Infrastructure.Services;
using System.Globalization;
using System.Net;
using System.Text;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KanbanDemandas.Web.Services.Automacoes;

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
