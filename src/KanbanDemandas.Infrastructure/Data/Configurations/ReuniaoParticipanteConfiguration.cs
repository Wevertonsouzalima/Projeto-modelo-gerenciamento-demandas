using KanbanDemandas.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanbanDemandas.Infrastructure.Data.Configurations;

public class ReuniaoParticipanteConfiguration : IEntityTypeConfiguration<ReuniaoParticipante>
{
    public void Configure(EntityTypeBuilder<ReuniaoParticipante> builder)
    {
        builder.ToTable("ReuniaoParticipantes");
        builder.HasKey(rp => new { rp.ReuniaoId, rp.UsuarioId });

        builder.HasOne(rp => rp.Reuniao)
               .WithMany(r => r.Participantes)
               .HasForeignKey(rp => rp.ReuniaoId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Usuario)
               .WithMany()
               .HasForeignKey(rp => rp.UsuarioId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
