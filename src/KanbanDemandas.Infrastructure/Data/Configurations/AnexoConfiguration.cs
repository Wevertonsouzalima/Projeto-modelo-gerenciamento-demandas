using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

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
