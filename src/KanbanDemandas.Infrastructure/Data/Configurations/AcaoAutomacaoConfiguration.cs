using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class AcaoAutomacaoConfiguration : IEntityTypeConfiguration<AcaoAutomacao>
{
    public void Configure(EntityTypeBuilder<AcaoAutomacao> builder)
    {
        builder.ToTable("AcoesAutomacao");
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.RegraAutomacao)
               .WithMany(r => r.Acoes)
               .HasForeignKey(a => a.RegraAutomacaoId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(a => !a.Excluido);
    }
}
