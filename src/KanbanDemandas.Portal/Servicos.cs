using System.Threading.Channels;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Core.Regras;
using KanbanDemandas.Infrastructure.Data;
using KanbanDemandas.Infrastructure.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace KanbanDemandas.Portal.Services
{
    #region Services/Formatacao.cs

    public static class Formatacao
    {
        /// <summary>Tamanho de arquivo legível (B, KB, MB).</summary>
        public static string Tamanho(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024d:0.#} KB";
            return $"{bytes / 1024d / 1024d:0.#} MB";
        }
    }

    #endregion

    #region Services/PontesSistemaPrincipal.cs

    /// <summary>
    /// Eventos de automação gerados no portal (cartão criado, comentário...). O portal não roda automações:
    /// grava os eventos na fila do banco, que o sistema principal executa respeitando a mesma espera.
    /// </summary>
    public sealed class FilaEventosPortal : IFilaEventosAutomacao
    {
        private readonly Channel<EventoAutomacao> _canal = Channel.CreateUnbounded<EventoAutomacao>(new UnboundedChannelOptions { SingleReader = true });

        public void Enfileirar(EventoAutomacao evento) => _canal.Writer.TryWrite(evento);

        public IAsyncEnumerable<EventoAutomacao> LerTodos(CancellationToken ct) => _canal.Reader.ReadAllAsync(ct);
    }

    public sealed class GravadorEventosPortal(FilaEventosPortal fila, IServiceScopeFactory scopeFactory, IConfiguration configuracao, ILogger<GravadorEventosPortal> logger)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var evento in fila.LerTodos(stoppingToken))
            {
                try
                {
                    var atraso = TimeSpan.FromMinutes(Math.Max(0, configuracao.GetValue("Automacao:AtrasoMinutos", 10)));
                    using var escopo = scopeFactory.CreateScope();
                    await escopo.ServiceProvider.GetRequiredService<AgendaEventosAutomacao>().AgendarAsync(evento, atraso);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Falha ao gravar o evento de automação {Gatilho} do cartão {Cartao}.", evento.Gatilho, evento.CartaoId);
                }
            }
        }
    }

    /// <summary>
    /// Avisa o sistema principal (pelo hub de tempo real dele) que quadros ou notificações mudaram,
    /// para que telas abertas lá atualizem na hora. Falhas são ignoradas: o dado já está gravado no banco.
    /// </summary>
    public sealed class PublicadorViaSistemaPrincipal(IConfiguration configuracao, ILogger<PublicadorViaSistemaPrincipal> logger)
        : IPublicadorAlteracoes, IAsyncDisposable
    {
        private readonly SemaphoreSlim _trava = new(1, 1);
        private HubConnection? _conexao;

        public async Task PublicarAsync(IReadOnlyCollection<int> quadroIds, IReadOnlyCollection<int> destinatariosNotificacao)
        {
            var url = configuracao["Portal:UrlSistemaPrincipal"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                var conexao = await ConectarAsync(url);
                await conexao.InvokeAsync("AvisarAlteracao", quadroIds.ToArray(), destinatariosNotificacao.ToArray());
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Não foi possível avisar o sistema principal em {Url}.", url);
            }
        }

        private async Task<HubConnection> ConectarAsync(string url)
        {
            await _trava.WaitAsync();
            try
            {
                if (_conexao is null)
                {
                    _conexao = new HubConnectionBuilder().WithUrl($"{url}/hubs/quadro").WithAutomaticReconnect().Build();
                }
                if (_conexao.State == HubConnectionState.Disconnected)
                    await _conexao.StartAsync();
                return _conexao;
            }
            finally
            {
                _trava.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_conexao is not null) await _conexao.DisposeAsync();
        }
    }

    #endregion

    #region Services/PortalService.cs

    public sealed record ConfiguracaoDoPortal(
        int QuadroId, int ListaEntradaId, string StatusInicial, CamposPortal Campos, IReadOnlyList<string> Tipos,
        bool PermitirAnexos, bool PermitirComentarios, string? Prefixo, IReadOnlyCollection<string> ExtensoesPermitidas,
        int LimiteMbArquivo, int LimiteMbTotal)
    {
        public bool Configurado => QuadroId > 0 && ListaEntradaId > 0;
        public bool Ve(CamposPortal campo) => Campos.HasFlag(campo);
    }

    public sealed record ResumoSolicitacao(int Id, string Codigo, string Titulo, string? Tipo, string Status, bool Concluida, DateTime CriadaEm, DateTime AtualizadaEm);

    public sealed record MensagemSolicitacao(string Autor, bool DoSolicitante, string Texto, DateTime Em);

    public sealed record AnexoSolicitacao(int Id, string Nome, long Tamanho, bool DoSolicitante, DateTime Em);

    public sealed record DetalheSolicitacao(
        int Id, string Codigo, string Titulo, string? Descricao, string? Tipo, string Status, bool Concluida, DateTime CriadaEm,
        IReadOnlyList<MudancaStatusPortal> HistoricoStatus, IReadOnlyList<(string Nome, string Valor)> Informacoes,
        IReadOnlyList<MensagemSolicitacao> Mensagens, IReadOnlyList<AnexoSolicitacao> Anexos);

    public sealed record ArquivoEnviado(string Nome, string ContentType, long Tamanho, Func<Stream> Abrir);

    /// <summary>
    /// Tudo o que o solicitante faz no portal. Ele só enxerga cartões em que é o solicitante, e só as
    /// informações liberadas na Parametrização › Portal do sistema principal.
    /// </summary>
    public sealed class PortalService(
        KanbanDbContext db,
        MovimentacaoCartaoService movimentacao,
        IAnexoStorage anexoStorage,
        IConfiguration configuracao)
    {
        public ConfiguracaoDoPortal Configuracao => new(
            configuracao.GetValue("Portal:QuadroId", 0),
            configuracao.GetValue("Portal:ListaEntradaId", 0),
            configuracao["Portal:StatusInicial"] is { Length: > 0 } inicial ? inicial : StatusPortal.StatusInicialPadrao,
            ConfiguracaoPortal.LerCampos(configuracao["Portal:CamposVisiveis"]),
            ConfiguracaoPortal.LerTipos(configuracao["Portal:TiposSolicitacao"]),
            configuracao.GetValue("Portal:PermitirAnexos", true),
            configuracao.GetValue("Portal:PermitirComentarios", true),
            configuracao["Cartao:PrefixoCodigo"],
            (configuracao["Anexos:ExtensoesPermitidas"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase),
            configuracao.GetValue("Anexos:TamanhoMaximoMbPorArquivo", 20),
            configuracao.GetValue("Anexos:TamanhoMaximoMbTotal", 100));

        public static string NomePrioridade(Prioridade prioridade) => prioridade switch
        {
            Prioridade.Baixa => "Baixa",
            Prioridade.Media => "Média",
            Prioridade.Alta => "Alta",
            Prioridade.Critica => "Crítica",
            _ => prioridade.ToString()
        };

        public async Task<List<Sistema>> SistemasAsync() => await db.Sistemas.AsNoTracking().Where(s => s.Ativo).OrderBy(s => s.Nome).ToListAsync();

        /// <summary>Solicitações do usuário (todas as que ele pediu, em qualquer quadro), mais recentes primeiro.</summary>
        public async Task<List<ResumoSolicitacao>> ListarAsync(int usuarioId)
        {
            var cfg = Configuracao;
            var cartoes = await db.Cartoes.AsNoTracking().Include(c => c.Lista)
                .Where(c => c.SolicitanteId == usuarioId && !c.Excluido).ToListAsync();
            if (cartoes.Count == 0) return [];
            var ids = cartoes.Select(c => c.Id).ToList();
            var quadros = cartoes.Select(c => c.Lista.QuadroId).Distinct().ToList();
            var listas = await db.Listas.AsNoTracking().IgnoreQueryFilters().Where(l => quadros.Contains(l.QuadroId)).ToListAsync();
            var entradas = (await db.HistoricoAtividades.AsNoTracking().Where(h => ids.Contains(h.CartaoId)).ToListAsync()).ToLookup(h => h.CartaoId);
            var ultimaMensagem = (await db.Comentarios.AsNoTracking().Where(c => c.CartaoId != null && ids.Contains(c.CartaoId.Value) && c.Publico && !c.Excluido)
                    .GroupBy(c => c.CartaoId!.Value).Select(g => new { g.Key, Ultima = g.Max(c => c.DataHora) }).ToListAsync())
                .ToDictionary(x => x.Key, x => x.Ultima);

            return cartoes.Select(c =>
                {
                    var historico = StatusPortal.Historico(c, entradas[c.Id], listas, cfg.StatusInicial);
                    var atualizada = new[] { historico[^1].Em, ultimaMensagem.GetValueOrDefault(c.Id, c.CriadoEm) }.Max();
                    return new ResumoSolicitacao(c.Id, CodigoCartao.Formatar(c.Id, cfg.Prefixo), c.Titulo, c.TipoSolicitacao, historico[^1].Status,
                        ListasQuadro.EhConcluido(c.ListaId, listas), c.CriadoEm, atualizada);
                })
                .OrderByDescending(r => r.AtualizadaEm).ToList();
        }

        public async Task<DetalheSolicitacao?> ObterAsync(int cartaoId, int usuarioId)
        {
            var cfg = Configuracao;
            var cartao = await db.Cartoes.AsNoTracking().AsSplitQuery()
                .Include(c => c.Lista).Include(c => c.Sistema)
                .Include(c => c.Desenvolvedores).ThenInclude(d => d.Usuario)
                .Include(c => c.Etiquetas).ThenInclude(e => e.Etiqueta)
                .FirstOrDefaultAsync(c => c.Id == cartaoId && c.SolicitanteId == usuarioId && !c.Excluido);
            if (cartao is null) return null;

            var listas = await db.Listas.AsNoTracking().IgnoreQueryFilters().Where(l => l.QuadroId == cartao.Lista.QuadroId).ToListAsync();
            var entradas = await db.HistoricoAtividades.AsNoTracking().Where(h => h.CartaoId == cartaoId).ToListAsync();
            var historico = StatusPortal.Historico(cartao, entradas, listas, cfg.StatusInicial);

            var informacoes = new List<(string, string)>();
            if (!string.IsNullOrWhiteSpace(cartao.TipoSolicitacao)) informacoes.Add(("Tipo", cartao.TipoSolicitacao));
            if (cfg.Ve(CamposPortal.Sistema) && cartao.Sistema is not null) informacoes.Add(("Sistema", cartao.Sistema.Nome));
            if (cfg.Ve(CamposPortal.Prioridade) && cartao.Prioridade.HasValue) informacoes.Add(("Prioridade", NomePrioridade(cartao.Prioridade.Value)));
            if (cfg.Ve(CamposPortal.DataInicio) && cartao.DataInicio.HasValue) informacoes.Add(("Início", cartao.DataInicio.Value.ToLocalTime().ToString("dd/MM/yyyy")));
            if (cfg.Ve(CamposPortal.Prazo) && cartao.Prazo.HasValue) informacoes.Add(("Previsão de entrega", cartao.Prazo.Value.ToLocalTime().ToString("dd/MM/yyyy")));
            if (cfg.Ve(CamposPortal.Estimativa) && cartao.Estimativa.HasValue) informacoes.Add(("Estimativa", $"{cartao.Estimativa.Value:0.##} pontos"));
            if (cfg.Ve(CamposPortal.Desenvolvedores) && cartao.Desenvolvedores.Count > 0)
                informacoes.Add(("Responsáveis", string.Join(", ", cartao.Desenvolvedores.OrderByDescending(d => d.Principal).Select(d => d.Usuario.Nome))));
            if (cfg.Ve(CamposPortal.Etiquetas) && cartao.Etiquetas.Count > 0)
                informacoes.Add(("Etiquetas", string.Join(", ", cartao.Etiquetas.Select(e => e.Etiqueta.Nome))));

            var mensagens = new List<MensagemSolicitacao>();
            if (cfg.Ve(CamposPortal.Comentarios))
            {
                mensagens = await db.Comentarios.AsNoTracking().Include(c => c.Autor)
                    .Where(c => c.CartaoId == cartaoId && c.Publico && !c.Excluido)
                    .OrderBy(c => c.DataHora)
                    .Select(c => new MensagemSolicitacao(c.AutorId == usuarioId ? "Você" : c.Autor.Nome, c.AutorId == usuarioId, c.Texto, c.DataHora))
                    .ToListAsync();
            }

            var anexos = await db.Anexos.AsNoTracking()
                .Where(a => a.CartaoId == cartaoId && !a.Excluido && (a.EnviadoPorId == usuarioId || cfg.Campos.HasFlag(CamposPortal.AnexosDoTime)))
                .OrderByDescending(a => a.CriadoEm)
                .Select(a => new AnexoSolicitacao(a.Id, a.NomeOriginal, a.TamanhoBytes, a.EnviadoPorId == usuarioId, a.CriadoEm))
                .ToListAsync();

            return new DetalheSolicitacao(cartao.Id, CodigoCartao.Formatar(cartao.Id, cfg.Prefixo), cartao.Titulo, cartao.Descricao, cartao.TipoSolicitacao,
                historico[^1].Status, ListasQuadro.EhConcluido(cartao.ListaId, listas), cartao.CriadoEm,
                cfg.Ve(CamposPortal.HistoricoStatus) ? historico : [], informacoes, mensagens, anexos);
        }

        /// <summary>Abre a solicitação como cartão na lista de entrada configurada.</summary>
        public async Task<int> CriarAsync(int usuarioId, string titulo, string descricao, string? tipo, int? sistemaId, Prioridade? urgencia, IReadOnlyList<ArquivoEnviado> arquivos)
        {
            var cfg = Configuracao;
            if (!cfg.Configurado) throw new InvalidOperationException("O portal ainda não foi configurado (quadro e lista de entrada).");
            ValidarArquivos(arquivos, 0, cfg);

            var listas = await db.Listas.AsNoTracking().Where(l => l.QuadroId == cfg.QuadroId && !l.Excluido).ToListAsync();
            if (listas.All(l => l.Id != cfg.ListaEntradaId)) throw new InvalidOperationException("A lista de entrada do portal não existe mais. Avise o administrador.");
            var listaId = ListasQuadro.PrimeiraFolha(cfg.ListaEntradaId, listas);

            var ordem = (await db.Cartoes.Where(c => c.ListaId == listaId && !c.Excluido).Select(c => (int?)c.Ordem).MaxAsync() ?? 0) + 1;
            var cartao = new Cartao
            {
                Titulo = titulo.Trim(), Descricao = descricao.Trim(), ListaId = listaId, Ordem = ordem, SolicitanteId = usuarioId,
                SistemaId = sistemaId, Prioridade = urgencia, OrigemPortal = true,
                TipoSolicitacao = string.IsNullOrWhiteSpace(tipo) ? null : tipo.Trim(),
                CriadoPorId = usuarioId, CriadoEm = DateTime.UtcNow
            };
            db.Cartoes.Add(cartao);
            await db.SaveChangesAsync();
            db.HistoricoAtividades.Add(new HistoricoAtividade
            {
                CartaoId = cartao.Id, Tipo = TipoHistoricoAtividade.CartaoCriado, ListaDestinoId = listaId,
                Descricao = "Solicitação aberta pelo portal.", UsuarioId = usuarioId, OcorridoEm = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            await movimentacao.AplicarEntradaNaCriacaoAsync(cartao.Id);
            foreach (var arquivo in arquivos) await GravarAnexoAsync(cartao.Id, usuarioId, arquivo);
            return cartao.Id;
        }

        public async Task EnviarMensagemAsync(int cartaoId, int usuarioId, string texto)
        {
            var cfg = Configuracao;
            if (!cfg.PermitirComentarios || !cfg.Ve(CamposPortal.Comentarios)) throw new InvalidOperationException("O envio de mensagens está desativado.");
            var cartao = await CartaoDoSolicitanteAsync(cartaoId, usuarioId);
            db.Comentarios.Add(new Comentario
            {
                CartaoId = cartao.Id, Texto = texto.Trim(), AutorId = usuarioId, Publico = true, DataHora = DateTime.UtcNow,
                CriadoPorId = usuarioId, CriadoEm = DateTime.UtcNow
            });
            var solicitante = await db.Usuarios.AsNoTracking().Where(u => u.Id == usuarioId).Select(u => u.Nome).FirstAsync();
            var codigo = CodigoCartao.Formatar(cartao.Id, cfg.Prefixo);
            db.Notificacoes.AddRange(cartao.Desenvolvedores.Select(d => d.UsuarioId).Distinct().Select(dev => new Notificacao
            {
                DestinatarioId = dev, CartaoOrigemId = cartao.Id, Tipo = TipoNotificacao.MensagemSolicitante,
                Mensagem = $"{solicitante} enviou uma mensagem em {codigo} \"{cartao.Titulo}\".", CriadoPorId = usuarioId, CriadoEm = DateTime.UtcNow
            }));
            await db.SaveChangesAsync();
        }

        public async Task AnexarAsync(int cartaoId, int usuarioId, IReadOnlyList<ArquivoEnviado> arquivos)
        {
            var cfg = Configuracao;
            if (!cfg.PermitirAnexos) throw new InvalidOperationException("O envio de anexos está desativado.");
            var cartao = await CartaoDoSolicitanteAsync(cartaoId, usuarioId);
            var totalAtual = await db.Anexos.Where(a => a.CartaoId == cartao.Id && !a.Excluido).SumAsync(a => a.TamanhoBytes);
            ValidarArquivos(arquivos, totalAtual, cfg);
            foreach (var arquivo in arquivos) await GravarAnexoAsync(cartao.Id, usuarioId, arquivo);
        }

        /// <summary>Conteúdo de um anexo, apenas se o solicitante pode vê-lo.</summary>
        public async Task<(string Nome, string ContentType, Stream Conteudo)?> AbrirAnexoAsync(int anexoId, int usuarioId)
        {
            var cfg = Configuracao;
            var anexo = await db.Anexos.AsNoTracking().Include(a => a.Cartao)
                .FirstOrDefaultAsync(a => a.Id == anexoId && !a.Excluido && !a.Cartao.Excluido && a.Cartao.SolicitanteId == usuarioId);
            if (anexo is null || (anexo.EnviadoPorId != usuarioId && !cfg.Ve(CamposPortal.AnexosDoTime))) return null;
            return (anexo.NomeOriginal, anexo.ContentType, await anexoStorage.ObterAsync(anexo.Caminho));
        }

        /// <summary>Cartão aberto do solicitante; concluídas não recebem mais mensagens nem anexos.</summary>
        private async Task<Cartao> CartaoDoSolicitanteAsync(int cartaoId, int usuarioId)
        {
            var cartao = await db.Cartoes.AsNoTracking().Include(c => c.Desenvolvedores).Include(c => c.Lista)
                             .FirstOrDefaultAsync(c => c.Id == cartaoId && c.SolicitanteId == usuarioId && !c.Excluido)
                         ?? throw new InvalidOperationException("Solicitação não encontrada.");
            if (ListasQuadro.EhConcluido(cartao.ListaId, [cartao.Lista]))
                throw new InvalidOperationException("Esta solicitação já foi concluída. Abra uma nova se precisar de algo mais.");
            return cartao;
        }

        private static void ValidarArquivos(IReadOnlyList<ArquivoEnviado> arquivos, long totalAtual, ConfiguracaoDoPortal cfg)
        {
            foreach (var arquivo in arquivos)
            {
                var extensao = Path.GetExtension(arquivo.Nome);
                if (cfg.ExtensoesPermitidas.Count > 0 && !cfg.ExtensoesPermitidas.Contains(extensao))
                    throw new InvalidOperationException($"O tipo de arquivo \"{extensao}\" não é aceito. Aceitos: {string.Join(", ", cfg.ExtensoesPermitidas)}.");
                if (arquivo.Tamanho > cfg.LimiteMbArquivo * 1024L * 1024L)
                    throw new InvalidOperationException($"\"{arquivo.Nome}\" passa do limite de {cfg.LimiteMbArquivo} MB.");
            }
            if (totalAtual + arquivos.Sum(a => a.Tamanho) > cfg.LimiteMbTotal * 1024L * 1024L)
                throw new InvalidOperationException($"Os anexos desta solicitação passariam do limite total de {cfg.LimiteMbTotal} MB.");
        }

        private async Task GravarAnexoAsync(int cartaoId, int usuarioId, ArquivoEnviado arquivo)
        {
            var extensao = Path.GetExtension(arquivo.Nome);
            var nomeDesejado = (configuracao["Anexos:EstrategiaNome"] ?? "Guid").ToLowerInvariant() switch
            {
                "original" => arquivo.Nome,
                "cartaooriginal" => $"{cartaoId}_{arquivo.Nome}",
                _ => $"{Guid.NewGuid():N}{extensao}"
            };
            await using var conteudo = arquivo.Abrir();
            var armazenado = await anexoStorage.SalvarAsync(conteudo, nomeDesejado, arquivo.ContentType);
            db.Anexos.Add(new Anexo
            {
                CartaoId = cartaoId, NomeOriginal = arquivo.Nome, NomeArmazenado = Path.GetFileName(armazenado.Caminho), Caminho = armazenado.Caminho,
                ContentType = arquivo.ContentType, TamanhoBytes = arquivo.Tamanho, Comprimido = armazenado.Comprimido,
                TamanhoArmazenadoBytes = armazenado.TamanhoArmazenado, EnviadoPorId = usuarioId, CriadoPorId = usuarioId, CriadoEm = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    #endregion
}
