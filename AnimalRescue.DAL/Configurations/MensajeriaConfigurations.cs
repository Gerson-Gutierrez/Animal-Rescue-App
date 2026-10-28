using AnimalRescue.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalRescue.DAL.Configurations;

internal class ConversacionConfiguration : IEntityTypeConfiguration<Conversacion>
{
    public void Configure(EntityTypeBuilder<Conversacion> b)
    {
        b.ToTable("conversaciones");
        b.HasKey(x => x.Id);
        // La convención snake_case no separa el dígito de "Usuario1Id", se mapea a mano.
        b.Property(x => x.Usuario1Id).HasColumnName("usuario1_id");
        b.Property(x => x.Usuario2Id).HasColumnName("usuario2_id");
        b.Property(x => x.Usuario1UltimaLectura).HasColumnName("usuario1_ultima_lectura");
        b.Property(x => x.Usuario2UltimaLectura).HasColumnName("usuario2_ultima_lectura");
        b.Property(x => x.Usuario1EliminadaEn).HasColumnName("usuario1_eliminada_en");
        b.Property(x => x.Usuario2EliminadaEn).HasColumnName("usuario2_eliminada_en");
        b.HasOne(x => x.Usuario1).WithMany().HasForeignKey(x => x.Usuario1Id).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Usuario2).WithMany().HasForeignKey(x => x.Usuario2Id).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Publicacion).WithMany().HasForeignKey(x => x.PublicacionId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.Mensajes).WithOne(x => x.Conversacion).HasForeignKey(x => x.ConversacionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal class MensajeConfiguration : IEntityTypeConfiguration<Mensaje>
{
    public void Configure(EntityTypeBuilder<Mensaje> b)
    {
        b.ToTable("mensajes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Contenido).HasMaxLength(4000);
        b.HasOne(x => x.Remitente).WithMany().HasForeignKey(x => x.RemitenteId).OnDelete(DeleteBehavior.Cascade);
    }
}
