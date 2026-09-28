using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class HistoricoAtividadeConfiguration : IEntityTypeConfiguration<HistoricoAtividade>
{
    public void Configure(EntityTypeBuilder<HistoricoAtividade> builder)
    {
        builder.ToTable("HistoricoAtividades");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Descricao).HasMaxLength(1000).IsRequired();
        builder.Property(h => h.ValorAnterior).HasMaxLength(2000);
        builder.Property(h => h.ValorNovo).HasMaxLength(2000);

        builder.HasOne(h => h.Cartao)
               .WithMany(c => c.Historico)
               .HasForeignKey(h => h.CartaoId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Usuario)
               .WithMany()
               .HasForeignKey(h => h.UsuarioId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.ListaOrigem)
               .WithMany()
               .HasForeignKey(h => h.ListaOrigemId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(h => h.ListaDestino)
               .WithMany()
               .HasForeignKey(h => h.ListaDestinoId)
               .OnDelete(DeleteBehavior.NoAction);

        // Índice para consultas de lead time e burndown
        builder.HasIndex(h => new { h.CartaoId, h.OcorridoEm });
        builder.HasIndex(h => new { h.ListaDestinoId, h.OcorridoEm });

        // Movimentos estornados (vai-e-volta corrigido) ficam só para auditoria; consultas normais os ignoram.
        builder.HasQueryFilter(h => !h.Estornado);
    }
}
