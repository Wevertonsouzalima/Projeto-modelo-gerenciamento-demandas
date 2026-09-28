using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class CartaoEtiquetaConfiguration : IEntityTypeConfiguration<CartaoEtiqueta>
{
    public void Configure(EntityTypeBuilder<CartaoEtiqueta> builder)
    {
        builder.ToTable("CartaoEtiquetas");
        builder.HasKey(ce => new { ce.CartaoId, ce.EtiquetaId });

        builder.HasOne(ce => ce.Cartao)
               .WithMany(c => c.Etiquetas)
               .HasForeignKey(ce => ce.CartaoId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ce => ce.Etiqueta)
               .WithMany(e => e.CartaoEtiquetas)
               .HasForeignKey(ce => ce.EtiquetaId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
