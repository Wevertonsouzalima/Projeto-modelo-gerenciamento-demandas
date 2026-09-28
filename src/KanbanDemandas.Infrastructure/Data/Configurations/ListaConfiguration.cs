using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

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
