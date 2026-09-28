using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class RegraAutomacaoConfiguration : IEntityTypeConfiguration<RegraAutomacao>
{
    public void Configure(EntityTypeBuilder<RegraAutomacao> builder)
    {
        builder.ToTable("RegrasAutomacao");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Nome).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Descricao).HasMaxLength(1000);
        builder.Property(r => r.ParametrosGatilhoJson).HasMaxLength(4000);

        builder.HasOne(r => r.Quadro)
               .WithMany()
               .HasForeignKey(r => r.QuadroId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Lista)
               .WithMany(l => l.RegrasAutomacao)
               .HasForeignKey(r => r.ListaId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(r => new { r.QuadroId, r.Gatilho });

        builder.HasQueryFilter(r => !r.Excluido);
    }
}
