using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class CartaoConfiguration : IEntityTypeConfiguration<Cartao>
{
    public void Configure(EntityTypeBuilder<Cartao> builder)
    {
        builder.ToTable("Cartoes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Titulo).HasMaxLength(500).IsRequired();
        builder.Property(c => c.TipoSolicitacao).HasMaxLength(100);
        builder.Property(c => c.Estimativa).HasColumnType("decimal(10,2)");

        // Hierarquia pai/filho (auto-referência)
        builder.HasOne(c => c.CartaoPai)
               .WithMany(c => c.CartosFilhos)
               .HasForeignKey(c => c.CartaoPaiId)
               .OnDelete(DeleteBehavior.Restrict);

        // Sistema e Solicitante sem cascade para evitar múltiplos caminhos
        builder.HasOne(c => c.Sistema)
               .WithMany(s => s.Cartoes)
               .HasForeignKey(c => c.SistemaId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.Solicitante)
               .WithMany()
               .HasForeignKey(c => c.SolicitanteId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.Lista)
               .WithMany(l => l.Cartoes)
               .HasForeignKey(c => c.ListaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.Excluido);
    }
}
