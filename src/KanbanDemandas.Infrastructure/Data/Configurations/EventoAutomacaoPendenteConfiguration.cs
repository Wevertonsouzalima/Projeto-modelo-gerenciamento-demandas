using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class EventoAutomacaoPendenteConfiguration : IEntityTypeConfiguration<EventoAutomacaoPendente>
{
    public void Configure(EntityTypeBuilder<EventoAutomacaoPendente> builder)
    {
        builder.ToTable("EventosAutomacaoPendentes");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.ExecutarEm);
        builder.HasIndex(e => e.CartaoId);
    }
}
