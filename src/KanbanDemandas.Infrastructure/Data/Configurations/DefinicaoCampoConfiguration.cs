using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class DefinicaoCampoConfiguration : IEntityTypeConfiguration<DefinicaoCampo>
{
    public void Configure(EntityTypeBuilder<DefinicaoCampo> builder)
    {
        builder.ToTable("DefinicoesCampo");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Nome).HasMaxLength(200).IsRequired();
        builder.Property(d => d.OpcoesSelecao).HasMaxLength(2000);

        builder.HasOne(d => d.Lista)
               .WithMany(l => l.DefinicoesCampo)
               .HasForeignKey(d => d.ListaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(d => !d.Excluido);
    }
}
