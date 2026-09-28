using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

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
