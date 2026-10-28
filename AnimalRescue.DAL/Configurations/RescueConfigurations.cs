using AnimalRescue.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalRescue.DAL.Configurations;

internal class HogarTransitoConfiguration : IEntityTypeConfiguration<HogarTransito>
{
    public void Configure(EntityTypeBuilder<HogarTransito> b)
    {
        b.ToTable("hogares_transito");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nombre).HasMaxLength(100);
        b.Property(x => x.Telefono).HasMaxLength(30);
        b.Property(x => x.Email).HasMaxLength(254);
        b.Property(x => x.Ciudad).HasMaxLength(100);
        b.Property(x => x.Pais).HasMaxLength(100);
        b.HasIndex(x => x.UsuarioId).IsUnique();
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal class RefugioConfiguration : IEntityTypeConfiguration<Refugio>
{
    public void Configure(EntityTypeBuilder<Refugio> b)
    {
        b.ToTable("refugios");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nombre).HasMaxLength(150);
        b.Property(x => x.Direccion).HasMaxLength(300);
        b.HasOne(x => x.CreadoPor).WithMany().HasForeignKey(x => x.CreadoPorId).OnDelete(DeleteBehavior.SetNull);
    }
}
