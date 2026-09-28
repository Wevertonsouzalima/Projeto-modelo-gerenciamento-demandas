using KanbanDemandas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KanbanDemandas.Infrastructure.Configuracao;

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
                .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)))
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
