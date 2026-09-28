using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class CartaoDesenvolvedorConfiguration : IEntityTypeConfiguration<CartaoDesenvolvedor>
{
    public void Configure(EntityTypeBuilder<CartaoDesenvolvedor> builder)
    {
        builder.ToTable("CartaoDesenvolvedores");
        builder.HasKey(cd => new { cd.CartaoId, cd.UsuarioId });

        builder.HasOne(cd => cd.Cartao)
               .WithMany(c => c.Desenvolvedores)
               .HasForeignKey(cd => cd.CartaoId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cd => cd.Usuario)
               .WithMany(u => u.CartoesDesenvolvedor)
               .HasForeignKey(cd => cd.UsuarioId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
