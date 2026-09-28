using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class SugestaoAutomacaoConfiguration : IEntityTypeConfiguration<SugestaoAutomacao>
{
    public void Configure(EntityTypeBuilder<SugestaoAutomacao> builder)
    {
        builder.ToTable("SugestoesAutomacao");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Descricao).HasMaxLength(1000).IsRequired();
        builder.Property(s => s.PropostaJson).IsRequired();

        builder.HasOne(s => s.RegraAutomacao)
               .WithMany()
               .HasForeignKey(s => s.RegraAutomacaoId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Cartao)
               .WithMany()
               .HasForeignKey(s => s.CartaoId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.QuadroId, s.Status });
        builder.HasIndex(s => new { s.CartaoId, s.Status });
    }
}
