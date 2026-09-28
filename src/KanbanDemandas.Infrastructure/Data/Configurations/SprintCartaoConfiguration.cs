using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

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
