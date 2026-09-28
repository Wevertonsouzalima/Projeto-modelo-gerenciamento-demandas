using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

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
