using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class ItemTarefaConfiguration : IEntityTypeConfiguration<ItemTarefa>
{
    public void Configure(EntityTypeBuilder<ItemTarefa> builder)
    {
        builder.ToTable("ItensTarefa");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Titulo).HasMaxLength(500).IsRequired();

        builder.HasOne(i => i.Cartao)
               .WithMany(c => c.ItensTarefa)
               .HasForeignKey(i => i.CartaoId)
               .OnDelete(DeleteBehavior.Cascade);

        // Desenvolvedor restrito — sem cascade para não apagar itens ao remover dev
        builder.HasOne(i => i.Desenvolvedor)
               .WithMany()
               .HasForeignKey(i => i.DesenvolvedorId)
               .OnDelete(DeleteBehavior.SetNull);

        // Cartão promovido — sem cascade
        builder.HasOne(i => i.CartaoPromovido)
               .WithMany()
               .HasForeignKey(i => i.CartaoPromovidoId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasQueryFilter(i => !i.Excluido);
    }
}
