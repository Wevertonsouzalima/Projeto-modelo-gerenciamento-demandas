using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class ValorCampoCartaoConfiguration : IEntityTypeConfiguration<ValorCampoCartao>
{
    public void Configure(EntityTypeBuilder<ValorCampoCartao> builder)
    {
        builder.ToTable("ValoresCampoCartao");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Valor).HasMaxLength(2000);
        builder.Property(v => v.ValorAnteriorAutomatico).HasMaxLength(2000);

        builder.HasOne(v => v.HistoricoOrigem)
               .WithMany()
               .HasForeignKey(v => v.HistoricoOrigemId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(v => v.Cartao)
               .WithMany(c => c.ValoresCampo)
               .HasForeignKey(v => v.CartaoId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.DefinicaoCampo)
               .WithMany(d => d.Valores)
               .HasForeignKey(v => v.DefinicaoCampoId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.PreenchidoPor)
               .WithMany()
               .HasForeignKey(v => v.PreenchidoPorId)
               .OnDelete(DeleteBehavior.Restrict);

        // Índice para buscar o valor mais recente por (Cartao, DefinicaoCampo)
        builder.HasIndex(v => new { v.CartaoId, v.DefinicaoCampoId, v.NumeroEntradaNaLista });

        builder.HasQueryFilter(v => !v.Excluido);
    }
}
