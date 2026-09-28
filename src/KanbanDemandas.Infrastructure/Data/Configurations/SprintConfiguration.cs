using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

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
