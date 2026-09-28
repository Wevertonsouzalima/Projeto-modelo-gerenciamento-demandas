using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure.Data;
using KanbanDemandas.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KanbanDemandas.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext: registrado como Transient para permitir consultas simultâneas no Blazor Server
        services.AddDbContext<KanbanDbContext>((sp, options) =>
        {
            options.UseMySql(
                configuration.GetConnectionString("DefaultConnection"),
                new MySqlServerVersion(new Version(8, 0, 0)),
                mysql => mysql.MigrationsAssembly(typeof(KanbanDbContext).Assembly.FullName)
            );

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

    /// <summary>
    /// Aplica migrations pendentes e garante que o banco exista.
    /// Chamado uma vez na inicialização da aplicação.
    /// </summary>
    public static async Task MigrarBancoAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KanbanDbContext>();
        var pendentes = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pendentes.Count > 0)
        {
            await db.Database.MigrateAsync();
        }
    }
}