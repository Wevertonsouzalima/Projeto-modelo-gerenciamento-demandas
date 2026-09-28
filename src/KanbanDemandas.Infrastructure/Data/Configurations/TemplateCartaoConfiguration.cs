using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class TemplateCartaoConfiguration : IEntityTypeConfiguration<TemplateCartao>
{
    public void Configure(EntityTypeBuilder<TemplateCartao> builder)
    {
        builder.ToTable("TemplatesCartao");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Nome).HasMaxLength(200).IsRequired();
        builder.Property(t => t.TitulopadraO).HasMaxLength(500);

        builder.HasOne(t => t.Quadro)
               .WithMany(q => q.TemplatesCartao)
               .HasForeignKey(t => t.QuadroId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(t => !t.Excluido);
    }
}
