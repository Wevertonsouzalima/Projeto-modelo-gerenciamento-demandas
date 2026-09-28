using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class FeriadoConfiguration : IEntityTypeConfiguration<Feriado>
{
    public void Configure(EntityTypeBuilder<Feriado> builder)
    {
        builder.ToTable("Feriados");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Nome).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Data).HasColumnType("date");
        builder.HasIndex(f => f.Data);
        builder.HasQueryFilter(f => !f.Excluido);
    }
}
