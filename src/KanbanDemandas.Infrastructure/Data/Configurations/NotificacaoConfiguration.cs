using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
{
    public void Configure(EntityTypeBuilder<Notificacao> builder)
    {
        builder.ToTable("Notificacoes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Mensagem).HasMaxLength(1000).IsRequired();

        builder.HasOne(n => n.Destinatario)
               .WithMany(u => u.Notificacoes)
               .HasForeignKey(n => n.DestinatarioId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.CartaoOrigem)
               .WithMany()
               .HasForeignKey(n => n.CartaoOrigemId)
               .OnDelete(DeleteBehavior.SetNull);

        // Índice para buscar notificações não lidas por usuário
        builder.HasIndex(n => new { n.DestinatarioId, n.Lida });

        builder.HasQueryFilter(n => !n.Excluido);
    }
}
