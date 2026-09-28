using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace KanbanDemandas.Infrastructure.Data;

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
