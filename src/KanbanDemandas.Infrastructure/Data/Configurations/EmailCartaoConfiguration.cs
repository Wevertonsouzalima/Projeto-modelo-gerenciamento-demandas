using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class EmailCartaoConfiguration : IEntityTypeConfiguration<EmailCartao>
{
    public void Configure(EntityTypeBuilder<EmailCartao> builder)
    {
        builder.ToTable("EmailsCartao");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Para).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Cc).HasMaxLength(500);
        builder.Property(e => e.Assunto).HasMaxLength(500).IsRequired();
        builder.Property(e => e.CorpoHtml).IsRequired();
        builder.Property(e => e.ErroEnvio).HasMaxLength(2000);

        builder.HasOne(e => e.Cartao)
               .WithMany(c => c.Emails)
               .HasForeignKey(e => e.CartaoId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.EnviadoPor)
               .WithMany()
               .HasForeignKey(e => e.EnviadoPorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.Excluido);
    }
}
