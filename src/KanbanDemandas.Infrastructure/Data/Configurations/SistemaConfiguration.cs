using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class SistemaConfiguration : IEntityTypeConfiguration<Sistema>
{
    public void Configure(EntityTypeBuilder<Sistema> builder)
    {
        builder.ToTable("Sistemas");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Nome).HasMaxLength(200).IsRequired();
        builder.HasQueryFilter(s => !s.Excluido);
    }
}
