using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class QuadroConfiguration : IEntityTypeConfiguration<Quadro>
{
    public void Configure(EntityTypeBuilder<Quadro> builder)
    {
        builder.ToTable("Quadros");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Nome).HasMaxLength(200).IsRequired();
        builder.Property(q => q.Descricao).HasMaxLength(1000);
        builder.Property(q => q.Cor).HasMaxLength(20).HasDefaultValue("#1565C0");
        builder.HasQueryFilter(q => !q.Excluido);
    }
}
