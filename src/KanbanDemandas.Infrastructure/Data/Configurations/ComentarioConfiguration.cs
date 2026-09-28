using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class ComentarioConfiguration : IEntityTypeConfiguration<Comentario>
{
    public void Configure(EntityTypeBuilder<Comentario> builder)
    {
        builder.ToTable("Comentarios");
        builder.HasKey(c => c.Id);
       builder.Property(c => c.Texto).IsRequired();

        builder.HasOne(c => c.Autor)
               .WithMany()
               .HasForeignKey(c => c.AutorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Cartao)
               .WithMany(ca => ca.Comentarios)
               .HasForeignKey(c => c.CartaoId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.ItemTarefa)
               .WithMany(i => i.Comentarios)
               .HasForeignKey(c => c.ItemTarefaId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasQueryFilter(c => !c.Excluido);
    }
}
