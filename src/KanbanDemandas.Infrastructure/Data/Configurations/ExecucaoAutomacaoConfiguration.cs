using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class ExecucaoAutomacaoConfiguration : IEntityTypeConfiguration<ExecucaoAutomacao>
{
    public void Configure(EntityTypeBuilder<ExecucaoAutomacao> builder)
    {
        builder.ToTable("ExecucoesAutomacao");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Resumo).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.Erro).HasMaxLength(2000);
        builder.Property(e => e.ChaveDisparo).HasMaxLength(200);

        builder.HasOne(e => e.RegraAutomacao)
               .WithMany()
               .HasForeignKey(e => e.RegraAutomacaoId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Cartao)
               .WithMany()
               .HasForeignKey(e => e.CartaoId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(e => new { e.RegraAutomacaoId, e.CartaoId, e.ChaveDisparo });
        builder.HasIndex(e => new { e.QuadroId, e.OcorridoEm });
    }
}
