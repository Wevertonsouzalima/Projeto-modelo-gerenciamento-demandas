using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class TemplateQuadroConfiguration : IEntityTypeConfiguration<TemplateQuadro>
{
    public void Configure(EntityTypeBuilder<TemplateQuadro> builder)
    {
        builder.ToTable("TemplatesQuadro");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Nome).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Descricao).HasMaxLength(1000);
        builder.HasQueryFilter(t => !t.Excluido);
    }
}
