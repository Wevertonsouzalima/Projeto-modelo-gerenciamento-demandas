using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class RelacaoCartaoConfiguration : IEntityTypeConfiguration<RelacaoCartao>
{
    public void Configure(EntityTypeBuilder<RelacaoCartao> builder)
    {
        builder.ToTable("RelacoesCartao");
        builder.HasKey(r => r.Id);

        // Dois caminhos para Cartao — sem cascade para evitar múltiplos delete paths
        builder.HasOne(r => r.CartaoOrigem)
               .WithMany(c => c.RelacoesOrigem)
               .HasForeignKey(r => r.CartaoOrigemId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.CartaoDestino)
               .WithMany(c => c.RelacoesDestino)
               .HasForeignKey(r => r.CartaoDestinoId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(r => !r.Excluido);
    }
}
