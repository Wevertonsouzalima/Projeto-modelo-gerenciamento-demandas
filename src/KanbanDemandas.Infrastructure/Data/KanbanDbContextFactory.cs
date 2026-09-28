using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KanbanDemandas.Infrastructure.Data;

public sealed class KanbanDbContextFactory : IDesignTimeDbContextFactory<KanbanDbContext>
{
    public KanbanDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<KanbanDbContext>()
            .UseMySql("Server=localhost;Port=3306;Database=KanbanDemandas;User=kanban;Password=change-me;", new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        return new KanbanDbContext(options);
    }
}
