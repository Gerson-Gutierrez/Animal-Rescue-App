using AnimalRescue.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalRescue.DAL.Configurations;

internal class PublicacionConfiguration : IEntityTypeConfiguration<Publicacion>
{
    public void Configure(EntityTypeBuilder<Publicacion> b)
    {
        b.ToTable("publicaciones");
        b.HasKey(x => x.Id);
        b.Property(x => x.NombreMascota).HasMaxLength(80);
        b.Property(x => x.Color).HasMaxLength(60);
        b.Property(x => x.Tamano).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.Edad).HasMaxLength(30);
        b.Property(x => x.Descripcion).HasMaxLength(2000);
        b.Property(x => x.Recompensa).HasPrecision(12, 2);
        b.Property(x => x.UbicacionTexto).HasMaxLength(300);
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(12);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.EliminadaPor).WithMany().HasForeignKey(x => x.EliminadaPorId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.Imagenes).WithOne(x => x.Publicacion).HasForeignKey(x => x.PublicacionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal class ImagenPublicacionConfiguration : IEntityTypeConfiguration<ImagenPublicacion>
{
    public void Configure(EntityTypeBuilder<ImagenPublicacion> b)
    {
        b.ToTable("imagenes_publicacion");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.PublicacionId, x.Orden }).IsUnique();
    }
}
