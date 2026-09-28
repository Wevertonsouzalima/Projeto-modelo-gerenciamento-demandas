using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class AlertaCartaoConfiguration : IEntityTypeConfiguration<AlertaCartao>
{
    public void Configure(EntityTypeBuilder<AlertaCartao> builder)
    {
        builder.ToTable("AlertasCartao");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Mensagem).HasMaxLength(500).IsRequired();

        builder.HasOne(a => a.Cartao)
               .WithMany(c => c.Alertas)
               .HasForeignKey(a => a.CartaoId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.RegraAutomacao)
               .WithMany()
               .HasForeignKey(a => a.RegraAutomacaoId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(a => new { a.CartaoId, a.Resolvido });
    }
}
