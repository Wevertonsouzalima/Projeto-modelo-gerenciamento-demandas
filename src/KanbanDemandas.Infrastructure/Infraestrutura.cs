using System.IO.Compression;
using System.Reflection;
using ClosedXML.Excel;
using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Core.Regras;
using KanbanDemandas.Infrastructure.Data;
using KanbanDemandas.Infrastructure.Services;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KanbanDemandas.Infrastructure.Configuracao
{
    #region Configuracao/ParametrosBancoConfiguration.cs

    /// <summary>
    /// Fonte de configuração que lê a tabela ParametrosSistema. Adicionada por último, sobrepõe o appsettings:
    /// o que foi alterado na tela vale; o resto continua vindo do arquivo.
    /// </summary>
    public sealed class ParametrosBancoConfigurationSource(string? connectionString) : IConfigurationSource
    {
        public ParametrosBancoConfigurationProvider Provider { get; } = new(connectionString);

        public IConfigurationProvider Build(IConfigurationBuilder builder) => Provider;
    }

    public sealed class ParametrosBancoConfigurationProvider(string? connectionString) : ConfigurationProvider
    {
        public override void Load()
        {
            Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(connectionString)) return;
            try
            {
                var opcoes = new DbContextOptionsBuilder<KanbanDbContext>()
                    .UseSqlServer(connectionString)
                    .Options;
                using var db = new KanbanDbContext(opcoes);
                foreach (var parametro in db.ParametrosSistema.AsNoTracking().ToList())
                    Data[parametro.Chave] = parametro.Valor;
            }
            catch (Exception)
            {
                // Banco indisponível ou tabela ainda não criada: segue só com o appsettings.
            }
        }

        /// <summary>Relê o banco e avisa quem observa a configuração; chamado depois de salvar na tela.</summary>
        public void Recarregar()
        {
            Load();
            OnReload();
        }
    }

    #endregion
}

namespace KanbanDemandas.Infrastructure.Data
{
    #region Data/AlteracoesTempoRealInterceptor.cs

    /// <summary>
    /// Após cada SaveChanges, identifica quais quadros e quais destinatários de notificação foram afetados
    /// e avisa o <see cref="IPublicadorAlteracoes"/>, para que toda alteração chegue em tempo real a quem
    /// está com o quadro aberto — sem depender de cada tela lembrar de disparar o aviso.
    /// </summary>
    public sealed class AlteracoesTempoRealInterceptor(ILogger<AlteracoesTempoRealInterceptor> logger, IPublicadorAlteracoes? publicador = null)
        : SaveChangesInterceptor
    {
        private readonly HashSet<int> _quadroIds = [];
        private readonly HashSet<int> _listaIds = [];
        private readonly HashSet<int> _cartaoIds = [];
        private readonly HashSet<int> _itemTarefaIds = [];
        private readonly HashSet<int> _destinatarios = [];

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Coletar(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Coletar(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            await PublicarAsync(eventData.Context);
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            PublicarAsync(eventData.Context).GetAwaiter().GetResult();
            return base.SavedChanges(eventData, result);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            Limpar();
            base.SaveChangesFailed(eventData);
        }

        private void Coletar(DbContext? context)
        {
            if (context is null || publicador is null) return;
            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;
                switch (entry.Entity)
                {
                    case Quadro q: _quadroIds.Add(q.Id); break;
                    case Lista l: _quadroIds.Add(l.QuadroId); break;
                    case Etiqueta e: _quadroIds.Add(e.QuadroId); break;
                    case Sprint s: _quadroIds.Add(s.QuadroId); break;
                    case DefinicaoCampo d: _listaIds.Add(d.ListaId); break;
                    case RegraAutomacao r: _quadroIds.Add(r.QuadroId); break;
                    case AlertaCartao al: _cartaoIds.Add(al.CartaoId); break;
                    case VinculoGit vg: _cartaoIds.Add(vg.CartaoId); break;
                    case SugestaoAutomacao sa: _cartaoIds.Add(sa.CartaoId); break;
                    case Cartao c:
                        _listaIds.Add(c.ListaId);
                        if (entry.State == EntityState.Modified && entry.Property(nameof(Cartao.ListaId)).OriginalValue is int listaOriginal)
                            _listaIds.Add(listaOriginal);
                        break;
                    case ItemTarefa i: _cartaoIds.Add(i.CartaoId); break;
                    case Anexo a: _cartaoIds.Add(a.CartaoId); break;
                    case EmailCartao m: _cartaoIds.Add(m.CartaoId); break;
                    case ValorCampoCartao v: _cartaoIds.Add(v.CartaoId); break;
                    case CartaoDesenvolvedor cd: _cartaoIds.Add(cd.CartaoId); break;
                    case CartaoEtiqueta ce: _cartaoIds.Add(ce.CartaoId); break;
                    case SprintCartao sc: _cartaoIds.Add(sc.CartaoId); break;
                    case RelacaoCartao rc: _cartaoIds.Add(rc.CartaoOrigemId); _cartaoIds.Add(rc.CartaoDestinoId); break;
                    case Comentario co:
                        if (co.CartaoId.HasValue) _cartaoIds.Add(co.CartaoId.Value);
                        if (co.ItemTarefaId.HasValue) _itemTarefaIds.Add(co.ItemTarefaId.Value);
                        break;
                    case Reuniao re:
                        if (re.CartaoId.HasValue) _cartaoIds.Add(re.CartaoId.Value);
                        if (re.ItemTarefaId.HasValue) _itemTarefaIds.Add(re.ItemTarefaId.Value);
                        break;
                    case Notificacao n: _destinatarios.Add(n.DestinatarioId); break;
                }
            }
        }

        private async Task PublicarAsync(DbContext? context)
        {
            if (context is null || publicador is null) return;
            try
            {
                if (_itemTarefaIds.Count > 0)
                    _cartaoIds.UnionWith(await context.Set<ItemTarefa>().IgnoreQueryFilters()
                        .Where(i => _itemTarefaIds.Contains(i.Id)).Select(i => i.CartaoId).ToListAsync());
                if (_cartaoIds.Count > 0)
                    _listaIds.UnionWith(await context.Set<Cartao>().IgnoreQueryFilters()
                        .Where(c => _cartaoIds.Contains(c.Id)).Select(c => c.ListaId).ToListAsync());
                if (_listaIds.Count > 0)
                    _quadroIds.UnionWith(await context.Set<Lista>().IgnoreQueryFilters()
                        .Where(l => _listaIds.Contains(l.Id)).Select(l => l.QuadroId).ToListAsync());

                if (_quadroIds.Count > 0 || _destinatarios.Count > 0)
                    await publicador.PublicarAsync(_quadroIds.ToList(), _destinatarios.ToList());
            }
            catch (Exception ex)
            {
                // Os dados já foram gravados; falhar o aviso em tempo real não deve quebrar a operação do usuário.
                logger.LogWarning(ex, "Falha ao publicar alterações em tempo real.");
            }
            finally
            {
                Limpar();
            }
        }

        private void Limpar()
        {
            _quadroIds.Clear();
            _listaIds.Clear();
            _cartaoIds.Clear();
            _itemTarefaIds.Clear();
            _destinatarios.Clear();
        }
    }

    #endregion

    #region Data/AuditoriaInterceptor.cs

    /// <summary>
    /// Interceptor que preenche automaticamente os campos de auditoria
    /// (CriadoEm, CriadoPorId, AlteradoEm, AlteradoPorId) antes de cada SaveChanges.
    /// </summary>
    public class AuditoriaInterceptor : SaveChangesInterceptor
    {
        private readonly SessaoUsuario _sessao;

        public AuditoriaInterceptor(SessaoUsuario sessao)
        {
            _sessao = sessao;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result)
        {
            AplicarAuditoria(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            AplicarAuditoria(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void AplicarAuditoria(DbContext? context)
        {
            if (context is null) return;

            int usuarioId = _sessao.UsuarioId ?? 1;
            var agora = DateTime.UtcNow;

            foreach (var entry in context.ChangeTracker.Entries<EntidadeBase>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CriadoEm = agora;
                        entry.Entity.CriadoPorId = usuarioId;
                        break;

                    case EntityState.Modified:
                        entry.Entity.AlteradoEm = agora;
                        entry.Entity.AlteradoPorId = usuarioId;
                        break;
                }
            }
        }
    }

    #endregion

    #region Data/EventosAutomacaoInterceptor.cs

    /// <summary>
    /// Traduz o que foi gravado em eventos de automação (cartão criado, movido, campo alterado etc.).
    /// Detectar no SaveChanges garante que qualquer tela ou serviço dispare as regras, sem cada um lembrar de avisar.
    /// Os valores originais só existem antes de gravar; os IDs de registros novos, só depois — por isso a coleta
    /// acontece em duas fases.
    /// </summary>
    public sealed class EventosAutomacaoInterceptor(SessaoUsuario sessao, IFilaEventosAutomacao? fila = null) : SaveChangesInterceptor
    {
        private readonly List<Func<EventoAutomacao?>> _pendentes = [];

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Coletar(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Coletar(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            Publicar();
            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            Publicar();
            return base.SavedChanges(eventData, result);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            _pendentes.Clear();
            base.SaveChangesFailed(eventData);
        }

        private void Publicar()
        {
            if (fila is null) return;
            foreach (var evento in _pendentes.Select(p => p()).OfType<EventoAutomacao>().Distinct())
                fila.Enfileirar(evento);
            _pendentes.Clear();
        }

        private void Coletar(DbContext? context)
        {
            if (context is null || fila is null) return;
            var entradas = context.ChangeTracker.Entries().ToList();

            // Um estorno (vai-e-volta desfeito) não é uma movimentação nova: não dispara regras de entrada/saída.
            var cartoesEstornados = entradas
                .Where(e => e.State == EntityState.Modified && e.Entity is HistoricoAtividade { Estornado: true })
                .Select(e => ((HistoricoAtividade)e.Entity).CartaoId)
                .ToHashSet();

            foreach (var entrada in entradas)
            {
                switch (entrada.Entity)
                {
                    case Cartao cartao when entrada.State == EntityState.Added:
                        Adicionar(TipoGatilho.CartaoCriado, () => cartao.Id, listaId: () => cartao.ListaId);
                        if (cartao.Prazo.HasValue) Adicionar(TipoGatilho.ConflitoDatas, () => cartao.Id);
                        break;

                    case Cartao cartao when entrada.State == EntityState.Modified && !cartao.Excluido:
                        ColetarAlteracoesCartao(entrada, cartao, cartoesEstornados.Contains(cartao.Id));
                        break;

                    case CartaoDesenvolvedor cd when entrada.State == EntityState.Added:
                        var usuarioAlvo = cd.UsuarioId;
                        Adicionar(TipoGatilho.CartaoAtribuido, () => cd.CartaoId, usuarioAlvo: usuarioAlvo);
                        Adicionar(TipoGatilho.CampoAlterado, () => cd.CartaoId, campo: CampoMonitorado.Desenvolvedores);
                        Adicionar(TipoGatilho.ConflitoDatas, () => cd.CartaoId);
                        break;

                    case CartaoDesenvolvedor cd when entrada.State == EntityState.Deleted:
                        Adicionar(TipoGatilho.CampoAlterado, () => cd.CartaoId, campo: CampoMonitorado.Desenvolvedores);
                        break;

                    case CartaoEtiqueta ce when entrada.State is EntityState.Added or EntityState.Deleted:
                        Adicionar(TipoGatilho.CampoAlterado, () => ce.CartaoId, campo: CampoMonitorado.Etiquetas);
                        break;

                    case ValorCampoCartao v when entrada.State == EntityState.Added
                        || entrada.State == EntityState.Modified && entrada.Property(nameof(ValorCampoCartao.Valor)).IsModified:
                        Adicionar(TipoGatilho.CampoAlterado, () => v.CartaoId, campo: CampoMonitorado.CampoPersonalizado);
                        break;

                    case Comentario c when entrada.State == EntityState.Added && c.CartaoId.HasValue:
                        Adicionar(TipoGatilho.ComentarioAdicionado, () => c.CartaoId!.Value);
                        break;

                    case ItemTarefa i when entrada.State == EntityState.Modified && i.Concluido
                        && entrada.Property(nameof(ItemTarefa.Concluido)).OriginalValue is false:
                        Adicionar(TipoGatilho.ChecklistConcluido, () => i.CartaoId);
                        break;

                    case RelacaoCartao r when entrada.State == EntityState.Added:
                        if (r.Tipo == TipoRelacaoCartao.BloqueadoPor) Adicionar(TipoGatilho.CartaoBloqueado, () => r.CartaoOrigemId);
                        if (r.Tipo == TipoRelacaoCartao.Bloqueia) Adicionar(TipoGatilho.CartaoBloqueado, () => r.CartaoDestinoId);
                        break;
                }
            }
        }

        private void ColetarAlteracoesCartao(EntityEntry entrada, Cartao cartao, bool estornado)
        {
            var lista = entrada.Property(nameof(Cartao.ListaId));
            if (lista.IsModified && lista.OriginalValue is int origem && origem != cartao.ListaId && !estornado)
            {
                var destino = cartao.ListaId;
                Adicionar(TipoGatilho.CartaoSaiuDaLista, () => cartao.Id, listaId: () => origem);
                Adicionar(TipoGatilho.CartaoEntrouNaLista, () => cartao.Id, listaId: () => destino);
            }

            var campos = new (string Propriedade, CampoMonitorado Campo)[]
            {
                (nameof(Cartao.Prazo), CampoMonitorado.Prazo),
                (nameof(Cartao.DataInicio), CampoMonitorado.DataInicio),
                (nameof(Cartao.Prioridade), CampoMonitorado.Prioridade),
                (nameof(Cartao.SistemaId), CampoMonitorado.Sistema),
                (nameof(Cartao.SolicitanteId), CampoMonitorado.Solicitante),
                (nameof(Cartao.Estimativa), CampoMonitorado.Estimativa)
            };
            foreach (var (propriedade, campo) in campos)
            {
                var prop = entrada.Property(propriedade);
                if (prop.IsModified && !Equals(prop.OriginalValue, prop.CurrentValue))
                    Adicionar(TipoGatilho.CampoAlterado, () => cartao.Id, campo: campo);
            }

            if (Alterou(entrada, nameof(Cartao.Prazo)) || Alterou(entrada, nameof(Cartao.DataInicio)))
                Adicionar(TipoGatilho.ConflitoDatas, () => cartao.Id);
        }

        private static bool Alterou(EntityEntry entrada, string propriedade)
        {
            var prop = entrada.Property(propriedade);
            return prop.IsModified && !Equals(prop.OriginalValue, prop.CurrentValue);
        }

        private void Adicionar(TipoGatilho gatilho, Func<int> cartaoId, Func<int>? listaId = null,
            CampoMonitorado campo = CampoMonitorado.Qualquer, int? usuarioAlvo = null)
        {
            var usuarioId = sessao.UsuarioId ?? 1;
            var profundidade = ContextoAutomacao.Profundidade;
            var cadeia = ContextoAutomacao.RegrasNaCadeia;
            _pendentes.Add(() =>
            {
                var id = cartaoId();
                return id <= 0 ? null : new EventoAutomacao(gatilho, id, usuarioId, profundidade, cadeia, listaId?.Invoke(), campo, usuarioAlvo);
            });
        }
    }

    #endregion

    #region Data/KanbanDbContext.cs

    public class KanbanDbContext : DbContext
    {
        public KanbanDbContext(DbContextOptions<KanbanDbContext> options) : base(options) { }

        // ── Entidades principais ──────────────────────────────────────────────
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Sistema> Sistemas => Set<Sistema>();
        public DbSet<Quadro> Quadros => Set<Quadro>();
        public DbSet<Lista> Listas => Set<Lista>();
        public DbSet<Cartao> Cartoes => Set<Cartao>();
        public DbSet<CartaoDesenvolvedor> CartaoDesenvolvedores => Set<CartaoDesenvolvedor>();
        public DbSet<CartaoEtiqueta> CartaoEtiquetas => Set<CartaoEtiqueta>();
        public DbSet<Etiqueta> Etiquetas => Set<Etiqueta>();

        // ── Motor de campos ───────────────────────────────────────────────────
        public DbSet<DefinicaoCampo> DefinicoesCampo => Set<DefinicaoCampo>();
        public DbSet<ValorCampoCartao> ValoresCampoCartao => Set<ValorCampoCartao>();

        // ── Linha do tempo ────────────────────────────────────────────────────
        public DbSet<ItemTarefa> ItensTarefa => Set<ItemTarefa>();
        public DbSet<Comentario> Comentarios => Set<Comentario>();
        public DbSet<Reuniao> Reunioes => Set<Reuniao>();
        public DbSet<ReuniaoParticipante> ReuniaoParticipantes => Set<ReuniaoParticipante>();
        public DbSet<Anexo> Anexos => Set<Anexo>();
        public DbSet<EmailCartao> EmailsCartao => Set<EmailCartao>();

        // ── Relações e histórico ──────────────────────────────────────────────
        public DbSet<RelacaoCartao> RelacoesCartao => Set<RelacaoCartao>();
        public DbSet<HistoricoAtividade> HistoricoAtividades => Set<HistoricoAtividade>();

        // ── Sprint ────────────────────────────────────────────────────────────
        public DbSet<Sprint> Sprints => Set<Sprint>();
        public DbSet<SprintCartao> SprintCartoes => Set<SprintCartao>();

        // ── Automação ─────────────────────────────────────────────────────────
        public DbSet<RegraAutomacao> RegrasAutomacao => Set<RegraAutomacao>();
        public DbSet<AcaoAutomacao> AcoesAutomacao => Set<AcaoAutomacao>();
        public DbSet<ExecucaoAutomacao> ExecucoesAutomacao => Set<ExecucaoAutomacao>();
        public DbSet<SugestaoAutomacao> SugestoesAutomacao => Set<SugestaoAutomacao>();
        public DbSet<AlertaCartao> AlertasCartao => Set<AlertaCartao>();
        public DbSet<ParametroSistema> ParametrosSistema => Set<ParametroSistema>();
        public DbSet<EventoAutomacaoPendente> EventosAutomacaoPendentes => Set<EventoAutomacaoPendente>();
        public DbSet<Feriado> Feriados => Set<Feriado>();
        public DbSet<VinculoGit> VinculosGit => Set<VinculoGit>();

        // ── Notificações ──────────────────────────────────────────────────────
        public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

        // ── Templates ─────────────────────────────────────────────────────────
        public DbSet<TemplateCartao> TemplatesCartao => Set<TemplateCartao>();
        public DbSet<TemplateQuadro> TemplatesQuadro => Set<TemplateQuadro>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Aplica todas as configurações do assembly atual (IEntityTypeConfiguration<T>)
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Seed de dados iniciais
            SeedData.Aplicar(modelBuilder);
        }
    }

    #endregion

    #region Data/KanbanDbContextFactory.cs

    public sealed class KanbanDbContextFactory : IDesignTimeDbContextFactory<KanbanDbContext>
    {
        public KanbanDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<KanbanDbContext>()
                .UseSqlServer("Server=localhost;Database=KanbanDemandas;User Id=kanban;Password=change-me;TrustServerCertificate=True;")
                .Options;

            return new KanbanDbContext(options);
        }
    }

    #endregion

    #region Data/SeedData.cs

    /// <summary>
    /// Seed de dados iniciais aplicado via HasData no OnModelCreating.
    /// Fornece usuários, sistemas e um quadro de exemplo para primeiro uso.
    /// </summary>
    public static class SeedData
    {
        private static readonly DateTime DataSeed = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static void Aplicar(ModelBuilder modelBuilder)
        {
            SeedUsuarios(modelBuilder);
            SeedSistemas(modelBuilder);
            SeedQuadroExemplo(modelBuilder);
        }

        private static void SeedUsuarios(ModelBuilder mb)
        {
            mb.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1, Nome = "Admin", Email = "admin@empresa.com",
                    Ativo = true, NotificarPorEmail = false,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Usuario
                {
                    Id = 2, Nome = "Ana Lima", Email = "ana.lima@empresa.com",
                    Ativo = true, NotificarPorEmail = true,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Usuario
                {
                    Id = 3, Nome = "Bruno Costa", Email = "bruno.costa@empresa.com",
                    Ativo = true, NotificarPorEmail = true,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Usuario
                {
                    Id = 4, Nome = "Carla Mendes", Email = "carla.mendes@empresa.com",
                    Ativo = true, NotificarPorEmail = false,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Usuario
                {
                    Id = 5, Nome = "Diego Santos", Email = "diego.santos@empresa.com",
                    Ativo = true, NotificarPorEmail = true,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                }
            );
        }

        private static void SeedSistemas(ModelBuilder mb)
        {
            mb.Entity<Sistema>().HasData(
                new Sistema
                {
                    Id = 1, Nome = "Portal do Cliente", Ativo = true,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Sistema
                {
                    Id = 2, Nome = "ERP Interno", Ativo = true,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Sistema
                {
                    Id = 3, Nome = "App Mobile", Ativo = true,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Sistema
                {
                    Id = 4, Nome = "Integrações", Ativo = true,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                }
            );
        }

        private static void SeedQuadroExemplo(ModelBuilder mb)
        {
            // Quadro de demonstração
            mb.Entity<Quadro>().HasData(new Quadro
            {
                Id = 1, Nome = "Time de Desenvolvimento", Descricao = "Quadro principal do time",
                Cor = "#1565C0", CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            });

            // Listas padrão
            mb.Entity<Lista>().HasData(
                new Lista
                {
                    Id = 1, Nome = "Backlog", QuadroId = 1, Ordem = 1,
                    EhBacklog = true, LimiteWip = null,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Lista
                {
                    Id = 2, Nome = "A Fazer", QuadroId = 1, Ordem = 2,
                    LimiteWip = 10,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Lista
                {
                    Id = 3, Nome = "Em Andamento", QuadroId = 1, Ordem = 3,
                    LimiteWip = 5,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Lista
                {
                    Id = 4, Nome = "Code Review", QuadroId = 1, Ordem = 4,
                    LimiteWip = 3,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Lista
                {
                    Id = 5, Nome = "Homologação", QuadroId = 1, Ordem = 5,
                    LimiteWip = null,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Lista
                {
                    Id = 6, Nome = "Pré-produção", QuadroId = 1, Ordem = 6,
                    LimiteWip = null,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Lista
                {
                    Id = 7, Nome = "Concluído", QuadroId = 1, Ordem = 7,
                    LimiteWip = null,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                }
            );

            // Campos configuráveis de exemplo: Homologação e Pré-produção
            mb.Entity<DefinicaoCampo>().HasData(
                new DefinicaoCampo
                {
                    Id = 1, Nome = "Data Homologado", Tipo = TipoCampo.Data,
                    ListaId = 5, Obrigatorio = true, PedirNovamenteACadaEntrada = true,
                    Ordem = 1, CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new DefinicaoCampo
                {
                    Id = 2, Nome = "Homologado Por", Tipo = TipoCampo.Texto,
                    ListaId = 5, Obrigatorio = false, PedirNovamenteACadaEntrada = false,
                    Ordem = 2, CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new DefinicaoCampo
                {
                    Id = 3, Nome = "Data Prevista de Change", Tipo = TipoCampo.Data,
                    ListaId = 6, Obrigatorio = true, PedirNovamenteACadaEntrada = false,
                    Ordem = 1, CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new DefinicaoCampo
                {
                    Id = 4, Nome = "Número do Change", Tipo = TipoCampo.Texto,
                    ListaId = 6, Obrigatorio = false, PedirNovamenteACadaEntrada = false,
                    Ordem = 2, CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                }
            );

            // Etiquetas padrão do quadro
            mb.Entity<Etiqueta>().HasData(
                new Etiqueta
                {
                    Id = 1, Nome = "Bug", Cor = "#E53935", QuadroId = 1,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Etiqueta
                {
                    Id = 2, Nome = "Feature", Cor = "#43A047", QuadroId = 1,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Etiqueta
                {
                    Id = 3, Nome = "Melhoria", Cor = "#1E88E5", QuadroId = 1,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Etiqueta
                {
                    Id = 4, Nome = "Urgente", Cor = "#FB8C00", QuadroId = 1,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                },
                new Etiqueta
                {
                    Id = 5, Nome = "Técnico", Cor = "#8E24AA", QuadroId = 1,
                    CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
                }
            );
        }
    }

    #endregion
}

namespace KanbanDemandas.Infrastructure.Data.Configurations
{
    #region Data/Configurations/AcaoAutomacaoConfiguration.cs

    public class AcaoAutomacaoConfiguration : IEntityTypeConfiguration<AcaoAutomacao>
    {
        public void Configure(EntityTypeBuilder<AcaoAutomacao> builder)
        {
            builder.ToTable("AcoesAutomacao");
            builder.HasKey(a => a.Id);

            builder.HasOne(a => a.RegraAutomacao)
                   .WithMany(r => r.Acoes)
                   .HasForeignKey(a => a.RegraAutomacaoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(a => !a.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/AlertaCartaoConfiguration.cs

    public class AlertaCartaoConfiguration : IEntityTypeConfiguration<AlertaCartao>
    {
        public void Configure(EntityTypeBuilder<AlertaCartao> builder)
        {
            builder.ToTable("AlertasCartao");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Mensagem).HasMaxLength(500).IsRequired();

            builder.HasOne(a => a.Cartao)
                   .WithMany(c => c.Alertas)
                   .HasForeignKey(a => a.CartaoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.RegraAutomacao)
                   .WithMany()
                   .HasForeignKey(a => a.RegraAutomacaoId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(a => new { a.CartaoId, a.Resolvido });
        }
    }

    #endregion

    #region Data/Configurations/AnexoConfiguration.cs

    public class AnexoConfiguration : IEntityTypeConfiguration<Anexo>
    {
        public void Configure(EntityTypeBuilder<Anexo> builder)
        {
            builder.ToTable("Anexos");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.NomeOriginal).HasMaxLength(500).IsRequired();
            builder.Property(a => a.NomeArmazenado).HasMaxLength(500).IsRequired();
            builder.Property(a => a.Caminho).HasMaxLength(1000).IsRequired();
            builder.Property(a => a.ContentType).HasMaxLength(200).IsRequired();

            builder.HasOne(a => a.Cartao)
                   .WithMany(c => c.Anexos)
                   .HasForeignKey(a => a.CartaoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.EnviadoPor)
                   .WithMany()
                   .HasForeignKey(a => a.EnviadoPorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(a => !a.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/CartaoConfiguration.cs

    public class CartaoConfiguration : IEntityTypeConfiguration<Cartao>
    {
        public void Configure(EntityTypeBuilder<Cartao> builder)
        {
            builder.ToTable("Cartoes");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Titulo).HasMaxLength(500).IsRequired();
            builder.Property(c => c.TipoSolicitacao).HasMaxLength(100);
            builder.Property(c => c.Estimativa).HasColumnType("decimal(10,2)");

            // Hierarquia pai/filho (auto-referência)
            builder.HasOne(c => c.CartaoPai)
                   .WithMany(c => c.CartosFilhos)
                   .HasForeignKey(c => c.CartaoPaiId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Sistema e Solicitante sem cascade para evitar múltiplos caminhos
            builder.HasOne(c => c.Sistema)
                   .WithMany(s => s.Cartoes)
                   .HasForeignKey(c => c.SistemaId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(c => c.Solicitante)
                   .WithMany()
                   .HasForeignKey(c => c.SolicitanteId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(c => c.Lista)
                   .WithMany(l => l.Cartoes)
                   .HasForeignKey(c => c.ListaId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(c => !c.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/CartaoDesenvolvedorConfiguration.cs

    public class CartaoDesenvolvedorConfiguration : IEntityTypeConfiguration<CartaoDesenvolvedor>
    {
        public void Configure(EntityTypeBuilder<CartaoDesenvolvedor> builder)
        {
            builder.ToTable("CartaoDesenvolvedores");
            builder.HasKey(cd => new { cd.CartaoId, cd.UsuarioId });

            builder.HasOne(cd => cd.Cartao)
                   .WithMany(c => c.Desenvolvedores)
                   .HasForeignKey(cd => cd.CartaoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cd => cd.Usuario)
                   .WithMany(u => u.CartoesDesenvolvedor)
                   .HasForeignKey(cd => cd.UsuarioId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    #endregion

    #region Data/Configurations/CartaoEtiquetaConfiguration.cs

    public class CartaoEtiquetaConfiguration : IEntityTypeConfiguration<CartaoEtiqueta>
    {
        public void Configure(EntityTypeBuilder<CartaoEtiqueta> builder)
        {
            builder.ToTable("CartaoEtiquetas");
            builder.HasKey(ce => new { ce.CartaoId, ce.EtiquetaId });

            builder.HasOne(ce => ce.Cartao)
                   .WithMany(c => c.Etiquetas)
                   .HasForeignKey(ce => ce.CartaoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ce => ce.Etiqueta)
                   .WithMany(e => e.CartaoEtiquetas)
                   .HasForeignKey(ce => ce.EtiquetaId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    #endregion

    #region Data/Configurations/ComentarioConfiguration.cs

    public class ComentarioConfiguration : IEntityTypeConfiguration<Comentario>
    {
        public void Configure(EntityTypeBuilder<Comentario> builder)
        {
            builder.ToTable("Comentarios");
            builder.HasKey(c => c.Id);
           builder.Property(c => c.Texto).IsRequired();

            builder.HasOne(c => c.Autor)
                   .WithMany()
                   .HasForeignKey(c => c.AutorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Cartao)
                   .WithMany(ca => ca.Comentarios)
                   .HasForeignKey(c => c.CartaoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.ItemTarefa)
                   .WithMany(i => i.Comentarios)
                   .HasForeignKey(c => c.ItemTarefaId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasQueryFilter(c => !c.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/DefinicaoCampoConfiguration.cs

    public class DefinicaoCampoConfiguration : IEntityTypeConfiguration<DefinicaoCampo>
    {
        public void Configure(EntityTypeBuilder<DefinicaoCampo> builder)
        {
            builder.ToTable("DefinicoesCampo");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Nome).HasMaxLength(200).IsRequired();
            builder.Property(d => d.OpcoesSelecao).HasMaxLength(2000);

            builder.HasOne(d => d.Lista)
                   .WithMany(l => l.DefinicoesCampo)
                   .HasForeignKey(d => d.ListaId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(d => !d.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/EmailCartaoConfiguration.cs

    public class EmailCartaoConfiguration : IEntityTypeConfiguration<EmailCartao>
    {
        public void Configure(EntityTypeBuilder<EmailCartao> builder)
        {
            builder.ToTable("EmailsCartao");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Para).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Cc).HasMaxLength(500);
            builder.Property(e => e.Assunto).HasMaxLength(500).IsRequired();
            builder.Property(e => e.CorpoHtml).IsRequired();
            builder.Property(e => e.ErroEnvio).HasMaxLength(2000);

            builder.HasOne(e => e.Cartao)
                   .WithMany(c => c.Emails)
                   .HasForeignKey(e => e.CartaoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.EnviadoPor)
                   .WithMany()
                   .HasForeignKey(e => e.EnviadoPorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(e => !e.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/EtiquetaConfiguration.cs

    public class EtiquetaConfiguration : IEntityTypeConfiguration<Etiqueta>
    {
        public void Configure(EntityTypeBuilder<Etiqueta> builder)
        {
            builder.ToTable("Etiquetas");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            builder.Property(e => e.Cor).HasMaxLength(20).HasDefaultValue("#607D8B");

            builder.HasOne(e => e.Quadro)
                   .WithMany(q => q.Etiquetas)
                   .HasForeignKey(e => e.QuadroId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(e => !e.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/EventoAutomacaoPendenteConfiguration.cs

    public class EventoAutomacaoPendenteConfiguration : IEntityTypeConfiguration<EventoAutomacaoPendente>
    {
        public void Configure(EntityTypeBuilder<EventoAutomacaoPendente> builder)
        {
            builder.ToTable("EventosAutomacaoPendentes");
            builder.HasKey(e => e.Id);
            builder.HasIndex(e => e.ExecutarEm);
            builder.HasIndex(e => e.CartaoId);
        }
    }

    #endregion

    #region Data/Configurations/ExecucaoAutomacaoConfiguration.cs

    public class ExecucaoAutomacaoConfiguration : IEntityTypeConfiguration<ExecucaoAutomacao>
    {
        public void Configure(EntityTypeBuilder<ExecucaoAutomacao> builder)
        {
            builder.ToTable("ExecucoesAutomacao");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Resumo).HasMaxLength(2000).IsRequired();
            builder.Property(e => e.Erro).HasMaxLength(2000);
            builder.Property(e => e.ChaveDisparo).HasMaxLength(200);

            builder.HasOne(e => e.RegraAutomacao)
                   .WithMany()
                   .HasForeignKey(e => e.RegraAutomacaoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Cartao)
                   .WithMany()
                   .HasForeignKey(e => e.CartaoId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(e => new { e.RegraAutomacaoId, e.CartaoId, e.ChaveDisparo });
            builder.HasIndex(e => new { e.QuadroId, e.OcorridoEm });
        }
    }

    #endregion

    #region Data/Configurations/FeriadoConfiguration.cs

    public class FeriadoConfiguration : IEntityTypeConfiguration<Feriado>
    {
        public void Configure(EntityTypeBuilder<Feriado> builder)
        {
            builder.ToTable("Feriados");
            builder.HasKey(f => f.Id);
            builder.Property(f => f.Nome).HasMaxLength(200).IsRequired();
            builder.Property(f => f.Data).HasColumnType("date");
            builder.HasIndex(f => f.Data);
            builder.HasQueryFilter(f => !f.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/HistoricoAtividadeConfiguration.cs

    public class HistoricoAtividadeConfiguration : IEntityTypeConfiguration<HistoricoAtividade>
    {
        public void Configure(EntityTypeBuilder<HistoricoAtividade> builder)
        {
            builder.ToTable("HistoricoAtividades");
            builder.HasKey(h => h.Id);
            builder.Property(h => h.Descricao).HasMaxLength(1000).IsRequired();
            builder.Property(h => h.ValorAnterior).HasMaxLength(2000);
            builder.Property(h => h.ValorNovo).HasMaxLength(2000);

            builder.HasOne(h => h.Cartao)
                   .WithMany(c => c.Historico)
                   .HasForeignKey(h => h.CartaoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(h => h.Usuario)
                   .WithMany()
                   .HasForeignKey(h => h.UsuarioId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(h => h.ListaOrigem)
                   .WithMany()
                   .HasForeignKey(h => h.ListaOrigemId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(h => h.ListaDestino)
                   .WithMany()
                   .HasForeignKey(h => h.ListaDestinoId)
                   .OnDelete(DeleteBehavior.NoAction);

            // Índice para consultas de lead time e burndown
            builder.HasIndex(h => new { h.CartaoId, h.OcorridoEm });
            builder.HasIndex(h => new { h.ListaDestinoId, h.OcorridoEm });

            // Movimentos estornados (vai-e-volta corrigido) ficam só para auditoria; consultas normais os ignoram.
            builder.HasQueryFilter(h => !h.Estornado);
        }
    }

    #endregion

    #region Data/Configurations/ItemTarefaConfiguration.cs

    public class ItemTarefaConfiguration : IEntityTypeConfiguration<ItemTarefa>
    {
        public void Configure(EntityTypeBuilder<ItemTarefa> builder)
        {
            builder.ToTable("ItensTarefa");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Titulo).HasMaxLength(500).IsRequired();

            builder.HasOne(i => i.Cartao)
                   .WithMany(c => c.ItensTarefa)
                   .HasForeignKey(i => i.CartaoId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Desenvolvedor restrito — sem cascade para não apagar itens ao remover dev
            builder.HasOne(i => i.Desenvolvedor)
                   .WithMany()
                   .HasForeignKey(i => i.DesenvolvedorId)
                   .OnDelete(DeleteBehavior.SetNull);

            // Cartão promovido — sem cascade
            builder.HasOne(i => i.CartaoPromovido)
                   .WithMany()
                   .HasForeignKey(i => i.CartaoPromovidoId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasQueryFilter(i => !i.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/ListaConfiguration.cs

    public class ListaConfiguration : IEntityTypeConfiguration<Lista>
    {
        public void Configure(EntityTypeBuilder<Lista> builder)
        {
            builder.ToTable("Listas");
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Nome).HasMaxLength(200).IsRequired();
            builder.Property(l => l.StatusPortal).HasMaxLength(100);

            // Auto-referência para sublistas
            builder.HasOne(l => l.ListaPai)
                   .WithMany(l => l.Sublistas)
                   .HasForeignKey(l => l.ListaPaiId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(l => l.Quadro)
                   .WithMany(q => q.Listas)
                   .HasForeignKey(l => l.QuadroId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(l => !l.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/NotificacaoConfiguration.cs

    public class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
    {
        public void Configure(EntityTypeBuilder<Notificacao> builder)
        {
            builder.ToTable("Notificacoes");
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Mensagem).HasMaxLength(1000).IsRequired();

            builder.HasOne(n => n.Destinatario)
                   .WithMany(u => u.Notificacoes)
                   .HasForeignKey(n => n.DestinatarioId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(n => n.CartaoOrigem)
                   .WithMany()
                   .HasForeignKey(n => n.CartaoOrigemId)
                   .OnDelete(DeleteBehavior.SetNull);

            // Índice para buscar notificações não lidas por usuário
            builder.HasIndex(n => new { n.DestinatarioId, n.Lida });

            builder.HasQueryFilter(n => !n.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/ParametroSistemaConfiguration.cs

    public class ParametroSistemaConfiguration : IEntityTypeConfiguration<ParametroSistema>
    {
        public void Configure(EntityTypeBuilder<ParametroSistema> builder)
        {
            builder.ToTable("ParametrosSistema");
            builder.HasKey(p => p.Chave);
            builder.Property(p => p.Chave).HasMaxLength(200);
            builder.Property(p => p.Valor).HasMaxLength(2000);
        }
    }

    #endregion

    #region Data/Configurations/QuadroConfiguration.cs

    public class QuadroConfiguration : IEntityTypeConfiguration<Quadro>
    {
        public void Configure(EntityTypeBuilder<Quadro> builder)
        {
            builder.ToTable("Quadros");
            builder.HasKey(q => q.Id);
            builder.Property(q => q.Nome).HasMaxLength(200).IsRequired();
            builder.Property(q => q.Descricao).HasMaxLength(1000);
            builder.Property(q => q.Cor).HasMaxLength(20).HasDefaultValue("#1565C0");
            builder.HasQueryFilter(q => !q.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/RegraAutomacaoConfiguration.cs

    public class RegraAutomacaoConfiguration : IEntityTypeConfiguration<RegraAutomacao>
    {
        public void Configure(EntityTypeBuilder<RegraAutomacao> builder)
        {
            builder.ToTable("RegrasAutomacao");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Nome).HasMaxLength(200).IsRequired();
            builder.Property(r => r.Descricao).HasMaxLength(1000);
            builder.Property(r => r.ParametrosGatilhoJson).HasMaxLength(4000);

            builder.HasOne(r => r.Quadro)
                   .WithMany()
                   .HasForeignKey(r => r.QuadroId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Lista)
                   .WithMany(l => l.RegrasAutomacao)
                   .HasForeignKey(r => r.ListaId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(r => new { r.QuadroId, r.Gatilho });

            builder.HasQueryFilter(r => !r.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/RelacaoCartaoConfiguration.cs

    public class RelacaoCartaoConfiguration : IEntityTypeConfiguration<RelacaoCartao>
    {
        public void Configure(EntityTypeBuilder<RelacaoCartao> builder)
        {
            builder.ToTable("RelacoesCartao");
            builder.HasKey(r => r.Id);

            // Dois caminhos para Cartao — sem cascade para evitar múltiplos delete paths
            builder.HasOne(r => r.CartaoOrigem)
                   .WithMany(c => c.RelacoesOrigem)
                   .HasForeignKey(r => r.CartaoOrigemId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.CartaoDestino)
                   .WithMany(c => c.RelacoesDestino)
                   .HasForeignKey(r => r.CartaoDestinoId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(r => !r.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/ReuniaoConfiguration.cs

    public class ReuniaoConfiguration : IEntityTypeConfiguration<Reuniao>
    {
        public void Configure(EntityTypeBuilder<Reuniao> builder)
        {
            builder.ToTable("Reunioes");
            builder.HasKey(r => r.Id);
           builder.Property(r => r.Ata).IsRequired();

            builder.HasOne(r => r.Autor)
                   .WithMany()
                   .HasForeignKey(r => r.AutorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Cartao)
                   .WithMany(c => c.Reunioes)
                   .HasForeignKey(r => r.CartaoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.ItemTarefa)
                   .WithMany(i => i.Reunioes)
                   .HasForeignKey(r => r.ItemTarefaId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasQueryFilter(r => !r.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/ReuniaoParticipanteConfiguration.cs

    public class ReuniaoParticipanteConfiguration : IEntityTypeConfiguration<ReuniaoParticipante>
    {
        public void Configure(EntityTypeBuilder<ReuniaoParticipante> builder)
        {
            builder.ToTable("ReuniaoParticipantes");
            builder.HasKey(rp => new { rp.ReuniaoId, rp.UsuarioId });

            builder.HasOne(rp => rp.Reuniao)
                   .WithMany(r => r.Participantes)
                   .HasForeignKey(rp => rp.ReuniaoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rp => rp.Usuario)
                   .WithMany()
                   .HasForeignKey(rp => rp.UsuarioId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    #endregion

    #region Data/Configurations/SistemaConfiguration.cs

    public class SistemaConfiguration : IEntityTypeConfiguration<Sistema>
    {
        public void Configure(EntityTypeBuilder<Sistema> builder)
        {
            builder.ToTable("Sistemas");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Nome).HasMaxLength(200).IsRequired();
            builder.HasQueryFilter(s => !s.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/SprintCartaoConfiguration.cs

    public class SprintCartaoConfiguration : IEntityTypeConfiguration<SprintCartao>
    {
        public void Configure(EntityTypeBuilder<SprintCartao> builder)
        {
            builder.ToTable("SprintCartoes");
            builder.HasKey(sc => new { sc.SprintId, sc.CartaoId });

            builder.HasOne(sc => sc.Sprint)
                   .WithMany(s => s.Cartoes)
                   .HasForeignKey(sc => sc.SprintId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sc => sc.Cartao)
                   .WithMany(c => c.Sprints)
                   .HasForeignKey(sc => sc.CartaoId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    #endregion

    #region Data/Configurations/SprintConfiguration.cs

    public class SprintConfiguration : IEntityTypeConfiguration<Sprint>
    {
        public void Configure(EntityTypeBuilder<Sprint> builder)
        {
            builder.ToTable("Sprints");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Nome).HasMaxLength(200).IsRequired();
            builder.Property(s => s.Meta).HasMaxLength(1000);

            builder.HasOne(s => s.Quadro)
                   .WithMany(q => q.Sprints)
                   .HasForeignKey(s => s.QuadroId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(s => !s.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/SugestaoAutomacaoConfiguration.cs

    public class SugestaoAutomacaoConfiguration : IEntityTypeConfiguration<SugestaoAutomacao>
    {
        public void Configure(EntityTypeBuilder<SugestaoAutomacao> builder)
        {
            builder.ToTable("SugestoesAutomacao");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Descricao).HasMaxLength(1000).IsRequired();
            builder.Property(s => s.PropostaJson).IsRequired();

            builder.HasOne(s => s.RegraAutomacao)
                   .WithMany()
                   .HasForeignKey(s => s.RegraAutomacaoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.Cartao)
                   .WithMany()
                   .HasForeignKey(s => s.CartaoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(s => new { s.QuadroId, s.Status });
            builder.HasIndex(s => new { s.CartaoId, s.Status });
        }
    }

    #endregion

    #region Data/Configurations/TemplateCartaoConfiguration.cs

    public class TemplateCartaoConfiguration : IEntityTypeConfiguration<TemplateCartao>
    {
        public void Configure(EntityTypeBuilder<TemplateCartao> builder)
        {
            builder.ToTable("TemplatesCartao");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Nome).HasMaxLength(200).IsRequired();
            builder.Property(t => t.TituloPadrao).HasMaxLength(500);

            builder.HasOne(t => t.Quadro)
                   .WithMany(q => q.TemplatesCartao)
                   .HasForeignKey(t => t.QuadroId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(t => !t.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/TemplateQuadroConfiguration.cs

    public class TemplateQuadroConfiguration : IEntityTypeConfiguration<TemplateQuadro>
    {
        public void Configure(EntityTypeBuilder<TemplateQuadro> builder)
        {
            builder.ToTable("TemplatesQuadro");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Nome).HasMaxLength(200).IsRequired();
            builder.Property(t => t.Descricao).HasMaxLength(1000);
            builder.HasQueryFilter(t => !t.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/UsuarioConfiguration.cs

    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuarios");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Nome).HasMaxLength(200).IsRequired();
            builder.Property(u => u.Email).HasMaxLength(300).IsRequired();
            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasQueryFilter(u => !u.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/ValorCampoCartaoConfiguration.cs

    public class ValorCampoCartaoConfiguration : IEntityTypeConfiguration<ValorCampoCartao>
    {
        public void Configure(EntityTypeBuilder<ValorCampoCartao> builder)
        {
            builder.ToTable("ValoresCampoCartao");
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Valor).HasMaxLength(2000);
            builder.Property(v => v.ValorAnteriorAutomatico).HasMaxLength(2000);

            builder.HasOne(v => v.HistoricoOrigem)
                   .WithMany()
                   .HasForeignKey(v => v.HistoricoOrigemId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(v => v.Cartao)
                   .WithMany(c => c.ValoresCampo)
                   .HasForeignKey(v => v.CartaoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(v => v.DefinicaoCampo)
                   .WithMany(d => d.Valores)
                   .HasForeignKey(v => v.DefinicaoCampoId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(v => v.PreenchidoPor)
                   .WithMany()
                   .HasForeignKey(v => v.PreenchidoPorId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Índice para buscar o valor mais recente por (Cartao, DefinicaoCampo)
            builder.HasIndex(v => new { v.CartaoId, v.DefinicaoCampoId, v.NumeroEntradaNaLista });

            builder.HasQueryFilter(v => !v.Excluido);
        }
    }

    #endregion

    #region Data/Configurations/VinculoGitConfiguration.cs

    public class VinculoGitConfiguration : IEntityTypeConfiguration<VinculoGit>
    {
        public void Configure(EntityTypeBuilder<VinculoGit> builder)
        {
            builder.ToTable("VinculosGit");
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Repositorio).HasMaxLength(300).IsRequired();
            builder.Property(v => v.Identificador).HasMaxLength(300).IsRequired();
            builder.Property(v => v.Titulo).HasMaxLength(500).IsRequired();
            builder.Property(v => v.Url).HasMaxLength(1000).IsRequired();
            builder.Property(v => v.Autor).HasMaxLength(200);

            builder.HasOne(v => v.Cartao)
                   .WithMany(c => c.VinculosGit)
                   .HasForeignKey(v => v.CartaoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(v => new { v.CartaoId, v.Tipo, v.Repositorio, v.Identificador }).IsUnique();
        }
    }

    #endregion
}

namespace KanbanDemandas.Infrastructure
{
    #region InfrastructureServiceExtensions.cs

    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // DbContext: registrado como Transient para permitir consultas simultâneas no Blazor Server
            services.AddDbContext<KanbanDbContext>((sp, options) =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

                // Interceptor de auditoria
                options.AddInterceptors(
                    sp.GetRequiredService<AuditoriaInterceptor>(),
                    sp.GetRequiredService<AlteracoesTempoRealInterceptor>(),
                    sp.GetRequiredService<EventosAutomacaoInterceptor>());
            }, ServiceLifetime.Transient, ServiceLifetime.Transient);

            // Interceptor como Transient
            services.AddTransient<AuditoriaInterceptor>();
            services.AddTransient<AlteracoesTempoRealInterceptor>();
            services.AddTransient<EventosAutomacaoInterceptor>();

            // Autenticação stub (v1 — plugável para AD/SSO depois)
            services.AddScoped<SessaoUsuario>();
            services.AddScoped<IUsuarioAtualProvider, FakeUsuarioAtualProvider>();

            // Serviços externos
            services.AddScoped<IEmailSender, MailKitEmailSender>();
            services.AddScoped<IAnexoStorage, LocalDiskAnexoStorage>();
            services.AddScoped<IExportacaoService, ExportacaoService>();

            return services;
        }
    }

    #endregion
}

namespace KanbanDemandas.Infrastructure.Services
{
    #region Services/AgendaEventosAutomacao.cs

    /// <summary>
    /// Espera das automações: eventos de alterações feitas por pessoas ficam gravados até o cartão passar
    /// "Automacao:AtrasoMinutos" sem nova alteração. Cada alteração no cartão reinicia a espera de todos os eventos dele.
    /// </summary>
    public sealed class AgendaEventosAutomacao(KanbanDbContext db)
    {
        public async Task AgendarAsync(EventoAutomacao evento, TimeSpan atraso)
        {
            var executarEm = DateTime.UtcNow.Add(atraso);
            var pendentes = await db.EventosAutomacaoPendentes.Where(p => p.CartaoId == evento.CartaoId).ToListAsync();
            foreach (var pendente in pendentes) pendente.ExecutarEm = executarEm;

            var repetido = pendentes.Any(p => p.Gatilho == evento.Gatilho && p.ListaId == evento.ListaId
                && p.Campo == evento.Campo && p.UsuarioAlvoId == evento.UsuarioAlvoId);
            if (!repetido)
            {
                db.EventosAutomacaoPendentes.Add(new EventoAutomacaoPendente
                {
                    CartaoId = evento.CartaoId, Gatilho = evento.Gatilho, ListaId = evento.ListaId, Campo = evento.Campo,
                    UsuarioAlvoId = evento.UsuarioAlvoId, UsuarioId = evento.UsuarioId, CriadoEm = DateTime.UtcNow, ExecutarEm = executarEm
                });
            }
            using (ContextoAutomacao.Entrar(int.MaxValue / 2, []))
                await db.SaveChangesAsync();
        }

        /// <summary>Retira da fila os eventos cuja espera terminou (ou todos, se <paramref name="agora"/> for nulo).</summary>
        public async Task<List<EventoAutomacao>> RetirarVencidosAsync(DateTime agora)
        {
            var vencidos = await db.EventosAutomacaoPendentes.Where(p => p.ExecutarEm <= agora).OrderBy(p => p.Id).ToListAsync();
            if (vencidos.Count == 0) return [];
            db.EventosAutomacaoPendentes.RemoveRange(vencidos);
            using (ContextoAutomacao.Entrar(int.MaxValue / 2, []))
                await db.SaveChangesAsync();
            return vencidos.Select(p => new EventoAutomacao(p.Gatilho, p.CartaoId, p.UsuarioId, 0, [], p.ListaId, p.Campo, p.UsuarioAlvoId)).ToList();
        }
    }

    #endregion

    #region Services/ExportacaoService.cs

    /// <summary>
    /// Gera exportações em PDF (QuestPDF) e Excel (ClosedXML) a partir de qualquer lista de objetos.
    /// </summary>
    public class ExportacaoService : IExportacaoService
    {
        public ExportacaoService()
        {
            // Licença community (gratuita para projetos internos)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public Task<byte[]> ExportarParaPdfAsync<T>(IEnumerable<T> dados, string titulo, CancellationToken ct = default)
        {
            var lista = dados.ToList();
            var propriedades = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);

                    page.Header().Text(titulo)
                        .SemiBold().FontSize(14).FontColor(Colors.Blue.Darken3);

                    page.Content().Table(table =>
                    {
                        // Colunas dinâmicas
                        table.ColumnsDefinition(cols =>
                        {
                            foreach (var _ in propriedades)
                                cols.RelativeColumn();
                        });

                        // Cabeçalho
                        table.Header(header =>
                        {
                            foreach (var prop in propriedades)
                            {
                                header.Cell().Background(Colors.Blue.Darken3)
                                    .Padding(4).Text(prop.Name)
                                    .FontColor(Colors.White).FontSize(9).Bold();
                            }
                        });

                        // Linhas
                        var linha = 0;
                        foreach (var item in lista)
                        {
                            var bg = linha++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                            foreach (var prop in propriedades)
                            {
                                var valor = prop.GetValue(item)?.ToString() ?? "";
                                table.Cell().Background(bg).Padding(3)
                                    .Text(valor).FontSize(8);
                            }
                        }
                    });

                    page.Footer().AlignRight()
                        .Text(text =>
                        {
                            text.Span("Gerado em ").FontSize(8);
                            text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8);
                            text.Span(" — Página ").FontSize(8);
                            text.CurrentPageNumber().FontSize(8);
                            text.Span(" de ").FontSize(8);
                            text.TotalPages().FontSize(8);
                        });
                });
            });

            var bytes = pdf.GeneratePdf();
            return Task.FromResult(bytes);
        }

        public Task<byte[]> ExportarParaExcelAsync<T>(IEnumerable<T> dados, string nomePlanilha, CancellationToken ct = default)
        {
            var lista = dados.ToList();
            var propriedades = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(nomePlanilha[..Math.Min(nomePlanilha.Length, 31)]);

            // Cabeçalho
            for (var col = 0; col < propriedades.Length; col++)
            {
                var cell = ws.Cell(1, col + 1);
                cell.Value = propriedades[col].Name;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
                cell.Style.Font.FontColor = XLColor.White;
            }

            // Dados
            for (var row = 0; row < lista.Count; row++)
            {
                for (var col = 0; col < propriedades.Length; col++)
                {
                    var valor = propriedades[col].GetValue(lista[row]);
                    var cell = ws.Cell(row + 2, col + 1);

                    cell.Value = valor switch
                    {
                        null => XLCellValue.FromObject(""),
                        bool b => b,
                        DateTime dt => dt,
                        int i => i,
                        long l => l,
                        decimal d => d,
                        double dbl => dbl,
                        _ => valor.ToString() ?? ""
                    };

                    if (row % 2 == 1)
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
                }
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return Task.FromResult(ms.ToArray());
        }
    }

    #endregion

    #region Services/FakeUsuarioAtualProvider.cs

    /// <summary>
    /// Implementação fake de IUsuarioAtualProvider para a v1 (sem autenticação real).
    /// O usuário selecionado é mantido por sessão via um estado Scoped simples.
    /// Para plugar AD/SSO depois: basta criar outra implementação de IUsuarioAtualProvider
    /// e registrá-la no DI — nenhuma outra mudança necessária.
    /// </summary>
    public class FakeUsuarioAtualProvider : IUsuarioAtualProvider
    {
        private readonly SessaoUsuario _sessao;
        private readonly KanbanDbContext _db;

        public FakeUsuarioAtualProvider(SessaoUsuario sessao, KanbanDbContext db)
        {
            _sessao = sessao;
            _db = db;
        }

        public Usuario ObterUsuarioAtual()
        {
            var id = _sessao.UsuarioId ?? 1; // fallback para Admin
            var usuario = _db.Usuarios
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == id)
                ?? throw new InvalidOperationException($"Usuário com Id={id} não encontrado.");
            return usuario;
        }

        public int ObterIdUsuarioAtual() => _sessao.UsuarioId ?? 1;
    }

    /// <summary>
    /// Estado de sessão do usuário selecionado. Registrado como Scoped (por circuito Blazor).
    /// Permite troca de usuário em tempo de execução sem reiniciar o app.
    /// </summary>
    public class SessaoUsuario
    {
        public int? UsuarioId { get; set; }

        public event Action? OnChange;
        public void NotificarMudanca() => OnChange?.Invoke();
    }

    #endregion

    #region Services/LocalDiskAnexoStorage.cs

    /// <summary>
    /// Implementação de IAnexoStorage que salva arquivos no disco local.
    /// Configuração: "Anexos:PastaBase", "Anexos:PoliticaColisao" (Renomear, Sobrescrever ou Rejeitar) e
    /// compactação: "Anexos:Comprimir", "Anexos:ExtensoesComprimir" e "Anexos:GanhoMinimoPercentual".
    /// Arquivos compactados ganham a extensão ".gz" e são descompactados na leitura.
    /// Pode ser substituída por uma implementação de blob storage sem alterar o restante.
    /// </summary>
    public class LocalDiskAnexoStorage : IAnexoStorage
    {
        private const string SufixoCompactado = ".gz";

        private readonly string _pastaBase;
        private readonly IConfiguration _config;
        private readonly ILogger<LocalDiskAnexoStorage> _logger;

        public LocalDiskAnexoStorage(IConfiguration config, ILogger<LocalDiskAnexoStorage> logger)
        {
            _pastaBase = Path.GetFullPath(config["Anexos:PastaBase"] ?? Path.Combine(AppContext.BaseDirectory, "uploads"));
            _config = config;
            _logger = logger;
            Directory.CreateDirectory(_pastaBase);
        }

        private string PoliticaColisao => _config["Anexos:PoliticaColisao"] ?? "Renomear";

        private bool DeveCompactar(string nome)
        {
            if (!bool.TryParse(_config["Anexos:Comprimir"], out var comprimir) || !comprimir) return false;
            var extensoes = (_config["Anexos:ExtensoesComprimir"] ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return extensoes.Contains(Path.GetExtension(nome), StringComparer.OrdinalIgnoreCase);
        }

        private double GanhoMinimo => Math.Clamp(int.TryParse(_config["Anexos:GanhoMinimoPercentual"], out var ganho) ? ganho : 10, 1, 90) / 100d;

        public async Task<ArquivoArmazenado> SalvarAsync(Stream conteudo, string nomeArmazenado, string contentType, CancellationToken ct = default)
        {
            var nome = NormalizarNome(nomeArmazenado);
            if (DeveCompactar(nome))
            {
                using var original = new MemoryStream();
                await conteudo.CopyToAsync(original, ct);
                var compactado = await CompactarEmMemoriaAsync(original, ct);
                if (compactado.Length <= original.Length * (1 - GanhoMinimo))
                    return await GravarAsync(compactado, nome + SufixoCompactado, comprimido: true, ct);
                original.Position = 0;
                return await GravarAsync(original, nome, comprimido: false, ct);
            }
            return await GravarAsync(conteudo, nome, comprimido: false, ct);
        }

        private async Task<ArquivoArmazenado> GravarAsync(Stream conteudo, string nome, bool comprimido, CancellationToken ct)
        {
            var caminho = ResolverCaminho(nome);
            long tamanho;
            await using (var arquivo = new FileStream(caminho, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await conteudo.CopyToAsync(arquivo, ct);
                tamanho = arquivo.Length;
            }
            _logger.LogInformation("Anexo salvo: {Caminho} ({Tamanho} bytes{Compactado})", caminho, tamanho, comprimido ? ", compactado" : "");
            return new ArquivoArmazenado(caminho, comprimido, tamanho);
        }

        private static async Task<MemoryStream> CompactarEmMemoriaAsync(Stream original, CancellationToken ct)
        {
            original.Position = 0;
            var destino = new MemoryStream();
            await using (var gzip = new GZipStream(destino, CompressionLevel.SmallestSize, leaveOpen: true))
                await original.CopyToAsync(gzip, ct);
            destino.Position = 0;
            return destino;
        }

        // Remove diretórios e caracteres inválidos, impedindo que o nome escape da pasta base.
        private static string NormalizarNome(string nome)
        {
            var somenteArquivo = Path.GetFileName(nome);
            var invalidos = Path.GetInvalidFileNameChars();
            var limpo = new string(somenteArquivo.Select(c => invalidos.Contains(c) ? '_' : c).ToArray()).Trim(' ', '.');
            return string.IsNullOrWhiteSpace(limpo) ? $"{Guid.NewGuid():N}" : limpo;
        }

        private string ResolverCaminho(string nome)
        {
            var caminho = Path.Combine(_pastaBase, nome);
            if (!File.Exists(caminho)) return caminho;

            switch (PoliticaColisao.ToLowerInvariant())
            {
                case "sobrescrever":
                    return caminho;
                case "rejeitar":
                    throw new IOException($"Já existe um anexo armazenado com o nome '{nome}'.");
                default:
                    var compactado = nome.EndsWith(SufixoCompactado, StringComparison.OrdinalIgnoreCase);
                    var semSufixo = compactado ? nome[..^SufixoCompactado.Length] : nome;
                    var baseNome = Path.GetFileNameWithoutExtension(semSufixo);
                    var extensao = Path.GetExtension(semSufixo) + (compactado ? SufixoCompactado : "");
                    for (var n = 1; ; n++)
                    {
                        var alternativo = Path.Combine(_pastaBase, $"{baseNome} ({n}){extensao}");
                        if (!File.Exists(alternativo)) return alternativo;
                    }
            }
        }

        public Task<Stream> ObterAsync(string caminho, CancellationToken ct = default)
        {
            if (!File.Exists(caminho))
                throw new FileNotFoundException("Anexo não encontrado.", caminho);

            Stream arquivo = File.OpenRead(caminho);
            return Task.FromResult(caminho.EndsWith(SufixoCompactado, StringComparison.OrdinalIgnoreCase)
                ? new GZipStream(arquivo, CompressionMode.Decompress)
                : arquivo);
        }

        public Task ExcluirAsync(string caminho, CancellationToken ct = default)
        {
            if (File.Exists(caminho))
                File.Delete(caminho);

            return Task.CompletedTask;
        }

        public async Task<ArquivoArmazenado?> CompactarAsync(string caminho, CancellationToken ct = default)
        {
            if (!File.Exists(caminho) || caminho.EndsWith(SufixoCompactado, StringComparison.OrdinalIgnoreCase) || !DeveCompactar(caminho))
                return null;

            using var original = new MemoryStream();
            await using (var arquivo = File.OpenRead(caminho))
                await arquivo.CopyToAsync(original, ct);
            var compactado = await CompactarEmMemoriaAsync(original, ct);
            if (compactado.Length > original.Length * (1 - GanhoMinimo)) return null;

            var destino = caminho + SufixoCompactado;
            await using (var arquivo = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None))
                await compactado.CopyToAsync(arquivo, ct);
            File.Delete(caminho);
            return new ArquivoArmazenado(destino, true, compactado.Length);
        }
    }

    #endregion

    #region Services/MailKitEmailSender.cs

    /// <summary>
    /// Implementação de IEmailSender usando MailKit.
    /// Configuração via appsettings: seção "Email".
    /// </summary>
    public class MailKitEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<MailKitEmailSender> _logger;

        public MailKitEmailSender(IConfiguration config, ILogger<MailKitEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task EnviarAsync(string para, string? cc, string assunto, string corpoHtml, CancellationToken ct = default)
        {
            var emailConfig = _config.GetSection("Email");
            var host = emailConfig["Host"] ?? "localhost";
            var port = int.TryParse(emailConfig["Port"], out var p) ? p : 25;
            var remetente = emailConfig["Remetente"] ?? "sistema@empresa.com";
            var nomeRemetente = emailConfig["NomeRemetente"] ?? "Sistema Kanban";
            var usuario = emailConfig["Usuario"];
            var senha = emailConfig["Senha"];

            var mensagem = new MimeMessage();
            mensagem.From.Add(new MailboxAddress(nomeRemetente, remetente));
            foreach (var addr in para.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                mensagem.To.Add(MailboxAddress.Parse(addr));

            if (!string.IsNullOrWhiteSpace(cc))
            {
                foreach (var addr in cc.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    mensagem.Cc.Add(MailboxAddress.Parse(addr));
            }

            mensagem.Subject = assunto;
            var builder = new BodyBuilder { HtmlBody = corpoHtml };
            mensagem.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.Auto, ct);

                if (!string.IsNullOrEmpty(usuario) && !string.IsNullOrEmpty(senha))
                    await client.AuthenticateAsync(usuario, senha, ct);

                await client.SendAsync(mensagem, ct);
                await client.DisconnectAsync(true, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar e-mail para {Para}", para);
                throw;
            }
        }
    }

    #endregion

    #region Services/MovimentacaoCartaoService.cs

    public sealed record ResultadoMovimentacao(
        bool MudouDeLista, int ListaDestinoId, string ListaDestinoNome, string? AvisoCaminho, bool Estornado = false,
        CamposFixosCartao CamposLimpos = CamposFixosCartao.Nenhum);

    public sealed class MovimentacaoCartaoService(
        KanbanDbContext db,
        IUsuarioAtualProvider usuarioProvider,
        IConfiguration configuracao)
    {
        private TimeSpan JanelaCorrecao => TimeSpan.FromMinutes(int.TryParse(configuracao["Movimentacao:JanelaCorrecaoMinutos"], out var minutos) ? Math.Max(0, minutos) : 10);

        /// <summary>
        /// O que falta para o cartão cumprir a etapa da lista (resolvida para a sublista que recebe cartões).
        /// Só considera campos manuais: os automáticos são preenchidos pelo próprio movimento.
        /// </summary>
        public async Task<PendenciasEtapa?> ObterPendenciasAsync(int cartaoId, int listaDestinoId)
        {
            var cartao = await db.Cartoes.AsNoTracking().Include(c => c.Lista).Include(c => c.Desenvolvedores)
                .FirstOrDefaultAsync(c => c.Id == cartaoId && !c.Excluido);
            if (cartao is null) return null;
            var listasQuadro = await db.Listas.AsNoTracking().Where(l => l.QuadroId == cartao.Lista.QuadroId && !l.Excluido).ToListAsync();
            var listaId = ListasQuadro.PrimeiraFolha(listaDestinoId, listasQuadro);
            var lista = await db.Listas.AsNoTracking().Include(l => l.DefinicoesCampo.Where(d => !d.Excluido))
                .FirstOrDefaultAsync(l => l.Id == listaId && l.QuadroId == cartao.Lista.QuadroId);
            if (lista is null) return null;

            var manuais = lista.DefinicoesCampo.Where(RegrasEtapa.EhManual).OrderBy(c => c.Ordem).ToList();
            var valores = await db.ValoresCampoCartao.AsNoTracking().Where(v => v.CartaoId == cartaoId).ToListAsync();
            var entraAgora = cartao.ListaId != lista.Id;

            var obrigatoriosVazios = manuais
                .Where(c => c.Obrigatorio && (c.PedirNovamenteACadaEntrada && entraAgora
                    || string.IsNullOrWhiteSpace(CamposCartao.ValorMaisRecente(valores, c.Id))))
                .ToList();
            var recorrentes = entraAgora ? manuais.Where(c => c.PedirNovamenteACadaEntrada).ToList() : [];

            return new PendenciasEtapa(lista.Id, lista.Nome, lista.BloquearEntradaComPendencias,
                RegrasEtapa.FixosFaltando(cartao, lista), obrigatoriosVazios, recorrentes,
                entraAgora ? RegrasEtapa.BloqueadosPreenchidos(cartao, lista) : CamposFixosCartao.Nenhum);
        }

        /// <summary>
        /// Move o cartão para a lista de destino, posicionando-o antes de <paramref name="cartaoAlvoId"/>
        /// (ou no fim da lista quando nulo). Retorna nulo se o cartão ou a lista não forem válidos.
        /// Voltar para a lista de origem logo após um movimento é tratado como correção: o movimento anterior é
        /// estornado e seus preenchimentos automáticos desfeitos, para o vai-e-volta não sujar histórico e métricas.
        /// Regras de automação da lista de destino disparam pelos eventos gerados ao gravar.
        /// </summary>
        public async Task<ResultadoMovimentacao?> MoverAsync(int cartaoId, int listaDestinoId, int? cartaoAlvoId, bool permitirEstorno = true)
        {
            if (cartaoAlvoId == cartaoId) return null;

            var cartao = await db.Cartoes.Include(c => c.Lista).FirstOrDefaultAsync(c => c.Id == cartaoId && !c.Excluido);
            if (cartao is null) return null;

            var listasQuadro = await db.Listas.AsNoTracking().Where(l => l.QuadroId == cartao.Lista.QuadroId && !l.Excluido).ToListAsync();
            var folhaDestino = ListasQuadro.PrimeiraFolha(listaDestinoId, listasQuadro);
            if (folhaDestino != listaDestinoId)
            {
                listaDestinoId = folhaDestino;
                cartaoAlvoId = null;
            }

            var listaDestino = await db.Listas
                .Include(l => l.DefinicoesCampo.Where(d => !d.Excluido))
                .FirstOrDefaultAsync(l => l.Id == listaDestinoId && !l.Excluido && l.QuadroId == cartao.Lista.QuadroId);
            if (listaDestino is null) return null;

            var usuarioId = usuarioProvider.ObterIdUsuarioAtual();
            var origemListaId = cartao.ListaId;
            var moveuDeLista = origemListaId != listaDestino.Id;

            var destino = await db.Cartoes
                .Where(c => c.ListaId == listaDestino.Id && !c.Excluido && c.Id != cartaoId)
                .OrderBy(c => c.Ordem).ToListAsync();
            var indice = cartaoAlvoId.HasValue ? destino.FindIndex(c => c.Id == cartaoAlvoId.Value) : -1;
            destino.Insert(indice < 0 ? destino.Count : indice, cartao);
            for (var i = 0; i < destino.Count; i++) destino[i].Ordem = i;

            var estornado = false;
            var limpos = CamposFixosCartao.Nenhum;
            if (moveuDeLista)
            {
                cartao.ListaId = listaDestino.Id;

                var restantes = await db.Cartoes
                    .Where(c => c.ListaId == origemListaId && !c.Excluido && c.Id != cartaoId)
                    .OrderBy(c => c.Ordem).ToListAsync();
                for (var i = 0; i < restantes.Count; i++) restantes[i].Ordem = i;

                var movimentoAnterior = await db.HistoricoAtividades
                    .Where(h => h.CartaoId == cartaoId && h.Tipo == TipoHistoricoAtividade.CartaoMovido)
                    .OrderByDescending(h => h.OcorridoEm).ThenByDescending(h => h.Id)
                    .FirstOrDefaultAsync();
                estornado = permitirEstorno && movimentoAnterior is not null
                    && movimentoAnterior.ListaOrigemId == listaDestino.Id && movimentoAnterior.ListaDestinoId == origemListaId
                    && movimentoAnterior.UsuarioId == usuarioId
                    && DateTime.UtcNow - movimentoAnterior.OcorridoEm <= JanelaCorrecao;

                if (estornado)
                {
                    await EstornarAsync(movimentoAnterior!, cartao);
                }
                else
                {
                    var numeroEntrada = await db.HistoricoAtividades.CountAsync(h => h.CartaoId == cartaoId && h.ListaDestinoId == listaDestino.Id
                        && (h.Tipo == TipoHistoricoAtividade.CartaoMovido || h.Tipo == TipoHistoricoAtividade.CartaoCriado)) + 1;
                    var entrada = new HistoricoAtividade
                    {
                        CartaoId = cartaoId, Tipo = TipoHistoricoAtividade.CartaoMovido,
                        ListaOrigemId = origemListaId, ListaDestinoId = listaDestino.Id,
                        Descricao = $"Cartão movido para {listaDestino.Nome}.", UsuarioId = usuarioId, OcorridoEm = DateTime.UtcNow
                    };
                    db.HistoricoAtividades.Add(entrada);
                    await AplicarPreenchimentoAutomaticoAsync(cartao, listaDestino, entrada, numeroEntrada);
                    await NotificarPendenciasAsync(cartao, listaDestino);
                }
                limpos = await LimparBloqueadosAsync(cartao, listaDestino, usuarioId);
                await SincronizarItemPromovidoAsync(cartao, listasQuadro);
            }

            await db.SaveChangesAsync();

            var aviso = moveuDeLista && !estornado ? CaminhoQuadro.AvisoMovimentacao(origemListaId, listaDestino.Id, listasQuadro) : null;

            return new ResultadoMovimentacao(moveuDeLista, listaDestino.Id, listaDestino.Nome, aviso, estornado, limpos);
        }

        /// <summary>Aplica os campos automáticos da lista em que o cartão acabou de ser criado.</summary>
        public async Task AplicarEntradaNaCriacaoAsync(int cartaoId)
        {
            var cartao = await db.Cartoes.FirstOrDefaultAsync(c => c.Id == cartaoId && !c.Excluido);
            var criacao = await db.HistoricoAtividades
                .FirstOrDefaultAsync(h => h.CartaoId == cartaoId && h.Tipo == TipoHistoricoAtividade.CartaoCriado);
            if (cartao is null || criacao is null) return;
            var lista = await db.Listas.Include(l => l.DefinicoesCampo.Where(d => !d.Excluido)).FirstAsync(l => l.Id == cartao.ListaId);
            await AplicarPreenchimentoAutomaticoAsync(cartao, lista, criacao, 1);
            await LimparBloqueadosAsync(cartao, lista, usuarioProvider.ObterIdUsuarioAtual());
            await NotificarPendenciasAsync(cartao, lista);
            await db.SaveChangesAsync();
        }

        /// <summary>Quantos cartões da lista têm campos que ela não permite (para oferecer a limpeza ao mudar a regra).</summary>
        public async Task<int> ContarComBloqueadosAsync(int listaId)
        {
            var lista = await db.Listas.AsNoTracking().FirstOrDefaultAsync(l => l.Id == listaId);
            if (lista is null || lista.CamposFixosBloqueados == CamposFixosCartao.Nenhum) return 0;
            var cartoes = await db.Cartoes.AsNoTracking().Include(c => c.Desenvolvedores).Where(c => c.ListaId == listaId && !c.Excluido).ToListAsync();
            return cartoes.Count(c => RegrasEtapa.BloqueadosPreenchidos(c, lista) != CamposFixosCartao.Nenhum);
        }

        /// <summary>Limpa, nos cartões que já estão na lista, os campos que ela não permite. Retorna quantos cartões mudaram.</summary>
        public async Task<int> LimparBloqueadosDaListaAsync(int listaId)
        {
            var lista = await db.Listas.FirstOrDefaultAsync(l => l.Id == listaId);
            if (lista is null) return 0;
            var usuarioId = usuarioProvider.ObterIdUsuarioAtual();
            var alterados = 0;
            foreach (var cartao in await db.Cartoes.Where(c => c.ListaId == listaId && !c.Excluido).ToListAsync())
                if (await LimparBloqueadosAsync(cartao, lista, usuarioId) != CamposFixosCartao.Nenhum) alterados++;
            await db.SaveChangesAsync();
            return alterados;
        }

        /// <summary>
        /// Remove do cartão o que a lista não permite (ex.: desenvolvedor no Backlog) e registra no histórico.
        /// O solicitante de uma solicitação do portal nunca é removido (ele deixaria de enxergá-la).
        /// </summary>
        private async Task<CamposFixosCartao> LimparBloqueadosAsync(Cartao cartao, Lista lista, int usuarioId)
        {
            var bloqueados = lista.CamposFixosBloqueados;
            if (bloqueados == CamposFixosCartao.Nenhum) return CamposFixosCartao.Nenhum;
            var limpos = CamposFixosCartao.Nenhum;
            if (bloqueados.HasFlag(CamposFixosCartao.Desenvolvedor))
            {
                var desenvolvedores = await db.CartaoDesenvolvedores.Where(d => d.CartaoId == cartao.Id).ToListAsync();
                if (desenvolvedores.Count > 0)
                {
                    db.CartaoDesenvolvedores.RemoveRange(desenvolvedores);
                    limpos |= CamposFixosCartao.Desenvolvedor;
                }
            }
            if (bloqueados.HasFlag(CamposFixosCartao.Prazo) && cartao.Prazo.HasValue) { cartao.Prazo = null; limpos |= CamposFixosCartao.Prazo; }
            if (bloqueados.HasFlag(CamposFixosCartao.DataInicio) && cartao.DataInicio.HasValue) { cartao.DataInicio = null; limpos |= CamposFixosCartao.DataInicio; }
            if (bloqueados.HasFlag(CamposFixosCartao.Estimativa) && cartao.Estimativa.HasValue) { cartao.Estimativa = null; limpos |= CamposFixosCartao.Estimativa; }
            if (bloqueados.HasFlag(CamposFixosCartao.Sistema) && cartao.SistemaId.HasValue) { cartao.SistemaId = null; limpos |= CamposFixosCartao.Sistema; }
            if (bloqueados.HasFlag(CamposFixosCartao.Solicitante) && cartao.SolicitanteId.HasValue && !cartao.OrigemPortal)
            {
                cartao.SolicitanteId = null;
                limpos |= CamposFixosCartao.Solicitante;
            }
            if (limpos != CamposFixosCartao.Nenhum)
            {
                db.HistoricoAtividades.Add(new HistoricoAtividade
                {
                    CartaoId = cartao.Id, Tipo = TipoHistoricoAtividade.CampoAlterado,
                    Descricao = $"Removido ao entrar em {lista.Nome} (não permitido nesta etapa): {RegrasEtapa.Descrever(limpos)}.",
                    UsuarioId = usuarioId, OcorridoEm = DateTime.UtcNow
                });
            }
            return limpos;
        }

        /// <summary>Exclusão lógica do cartão; retorna false se ele não existir.</summary>
        public async Task<bool> ExcluirAsync(int cartaoId)
        {
            var cartao = await db.Cartoes.FirstOrDefaultAsync(c => c.Id == cartaoId && !c.Excluido);
            if (cartao is null) return false;

            cartao.Excluido = true;
            cartao.ExcluidoEm = DateTime.UtcNow;
            cartao.ExcluidoPorId = usuarioProvider.ObterIdUsuarioAtual();

            var restantes = await db.Cartoes
                .Where(c => c.ListaId == cartao.ListaId && !c.Excluido && c.Id != cartaoId)
                .OrderBy(c => c.Ordem).ToListAsync();
            for (var i = 0; i < restantes.Count; i++) restantes[i].Ordem = i;

            await db.SaveChangesAsync();
            return true;
        }

        private async Task AplicarPreenchimentoAutomaticoAsync(Cartao cartao, Lista lista, HistoricoAtividade entrada, int numeroEntrada)
        {
            var automaticos = lista.DefinicoesCampo.Where(c => !c.Excluido && !RegrasEtapa.EhManual(c)).ToList();
            if (automaticos.Count == 0) return;
            var usuario = usuarioProvider.ObterUsuarioAtual();

            foreach (var campo in automaticos)
            {
                var valor = campo.Preenchimento == PreenchimentoAutomatico.DataEntrada
                    ? DateTime.Now.ToString("yyyy-MM-dd")
                    : usuario.Nome;
                var atual = await db.ValoresCampoCartao
                    .Where(v => v.CartaoId == cartao.Id && v.DefinicaoCampoId == campo.Id)
                    .OrderByDescending(v => v.NumeroEntradaNaLista).ThenByDescending(v => v.DataPreenchimento)
                    .FirstOrDefaultAsync();

                var aplicou = campo.RegraReentrada switch
                {
                    RegraReentrada.ManterPrimeiro when !string.IsNullOrWhiteSpace(atual?.Valor) => false,
                    RegraReentrada.NovoRegistro => Criar(),
                    _ when atual is null => Criar(),
                    _ => Atualizar(atual)
                };

                if (aplicou && campo.DefineDataInicioCartao && campo.Tipo == TipoCampo.Data
                    && (campo.RegraReentrada != RegraReentrada.ManterPrimeiro || !cartao.DataInicio.HasValue))
                {
                    entrada.AlterouDataInicio = true;
                    entrada.DataInicioAnterior = cartao.DataInicio;
                    cartao.DataInicio = DateTime.Today.ToUniversalTime();
                }

                bool Criar()
                {
                    db.ValoresCampoCartao.Add(new ValorCampoCartao
                    {
                        CartaoId = cartao.Id, DefinicaoCampoId = campo.Id, Valor = valor, NumeroEntradaNaLista = numeroEntrada,
                        DataPreenchimento = DateTime.UtcNow, PreenchidoPorId = usuario.Id, HistoricoOrigem = entrada,
                        CriadoAutomaticamente = true, CriadoPorId = usuario.Id, CriadoEm = DateTime.UtcNow
                    });
                    return true;
                }

                bool Atualizar(ValorCampoCartao existente)
                {
                    existente.ValorAnteriorAutomatico = existente.Valor;
                    existente.Valor = valor;
                    existente.DataPreenchimento = DateTime.UtcNow;
                    existente.PreenchidoPorId = usuario.Id;
                    existente.HistoricoOrigem = entrada;
                    existente.CriadoAutomaticamente = false;
                    return true;
                }
            }
        }

        /// <summary>Desfaz o movimento anterior: marca como estornado e reverte o que ele preencheu automaticamente.</summary>
        private async Task EstornarAsync(HistoricoAtividade movimento, Cartao cartao)
        {
            movimento.Estornado = true;
            var preenchidos = await db.ValoresCampoCartao.Where(v => v.HistoricoOrigemId == movimento.Id).ToListAsync();
            foreach (var valor in preenchidos)
            {
                if (valor.CriadoAutomaticamente)
                {
                    valor.Excluido = true;
                    valor.ExcluidoEm = DateTime.UtcNow;
                    valor.ExcluidoPorId = usuarioProvider.ObterIdUsuarioAtual();
                }
                else
                {
                    valor.Valor = valor.ValorAnteriorAutomatico;
                    valor.ValorAnteriorAutomatico = null;
                }
                valor.HistoricoOrigemId = null;
            }
            if (movimento.AlterouDataInicio) cartao.DataInicio = movimento.DataInicioAnterior;
        }

        /// <summary>Avisa os responsáveis quando o cartão entra numa etapa sem o que ela exige.</summary>
        private async Task NotificarPendenciasAsync(Cartao cartao, Lista lista)
        {
            var desenvolvedores = await db.CartaoDesenvolvedores.Where(d => d.CartaoId == cartao.Id).Select(d => d.UsuarioId).ToListAsync();
            var cartaoComDevs = new Cartao
            {
                Prazo = cartao.Prazo, Estimativa = cartao.Estimativa, SistemaId = cartao.SistemaId, SolicitanteId = cartao.SolicitanteId,
                Desenvolvedores = desenvolvedores.Select(id => new CartaoDesenvolvedor { UsuarioId = id }).ToList()
            };
            var pendentes = RegrasEtapa.Separar(RegrasEtapa.FixosFaltando(cartaoComDevs, lista)).Select(RegrasEtapa.Nome).ToList();

            var obrigatorios = lista.DefinicoesCampo.Where(c => !c.Excluido && c.Obrigatorio && RegrasEtapa.EhManual(c)).ToList();
            if (obrigatorios.Count > 0)
            {
                var valores = await db.ValoresCampoCartao.Where(v => v.CartaoId == cartao.Id).ToListAsync();
                pendentes.AddRange(obrigatorios
                    .Where(c => c.PedirNovamenteACadaEntrada || string.IsNullOrWhiteSpace(CamposCartao.ValorMaisRecente(valores, c.Id)))
                    .Select(c => c.Nome));
            }
            if (pendentes.Count == 0) return;

            var destinatarios = desenvolvedores.Count > 0
                ? desenvolvedores
                : cartao.SolicitanteId.HasValue ? [cartao.SolicitanteId.Value] : [];
            var usuarioId = usuarioProvider.ObterIdUsuarioAtual();
            db.Notificacoes.AddRange(destinatarios.Select(destinatario => new Notificacao
            {
                DestinatarioId = destinatario, CartaoOrigemId = cartao.Id, Tipo = TipoNotificacao.CampoPendente,
                Mensagem = $"O cartão '{cartao.Titulo}' entrou em '{lista.Nome}' com pendências: {string.Join(", ", pendentes)}.",
                CriadoPorId = usuarioId, CriadoEm = DateTime.UtcNow
            }));
        }

        /// <summary>
        /// O item de tarefa que originou um cartão filho reflete o status desse filho: fica concluído
        /// enquanto o filho estiver em uma lista "Concluído" e volta a pendente se o filho sair dela.
        /// </summary>
        private async Task SincronizarItemPromovidoAsync(Cartao cartao, IReadOnlyCollection<Lista> listasQuadro)
        {
            var itens = await db.ItensTarefa.Where(i => i.CartaoPromovidoId == cartao.Id && !i.Excluido).ToListAsync();
            if (itens.Count == 0) return;
            var concluido = ListasQuadro.EhConcluido(cartao.ListaId, listasQuadro);
            foreach (var item in itens) item.Concluido = concluido;
        }
    }

    #endregion
}
