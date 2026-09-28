using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class ReuniaoConfiguration : IEntityTypeConfiguration<Reuniao>
{
    public void Configure(EntityTypeBuilder<Reuniao> builder)
    {
        builder.ToTable("Reunioes");
        builder.HasKey(r => r.Id);
       builder.Property(r => r.Ata).IsRequired();

        builder.HasOne(r => r.Autor)
               .WithMany()
               .HasForeignKey(r => r.AutorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Cartao)
               .WithMany(c => c.Reunioes)
               .HasForeignKey(r => r.CartaoId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ItemTarefa)
               .WithMany(i => i.Reunioes)
               .HasForeignKey(r => r.ItemTarefaId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasQueryFilter(r => !r.Excluido);
    }
}
