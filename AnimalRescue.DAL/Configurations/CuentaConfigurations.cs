using AnimalRescue.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalRescue.DAL.Configurations;

internal class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuarios");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nombre).HasMaxLength(100);
        b.Property(x => x.Email).HasMaxLength(254);
        b.Property(x => x.Rol).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => x.Email).IsUnique();
        b.HasOne(x => x.PromovidoPor).WithMany().HasForeignKey(x => x.PromovidoPorId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).HasMaxLength(128);
        b.Ignore(x => x.EstaActivo);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        // La relación le indica a EF que debe insertar el token nuevo antes de actualizar el reemplazado.
        b.HasOne(x => x.ReemplazadoPor).WithMany().HasForeignKey(x => x.ReemplazadoPorId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal class ReporteConfiguration : IEntityTypeConfiguration<Reporte>
{
    public void Configure(EntityTypeBuilder<Reporte> b)
    {
        b.ToTable("reportes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Motivo).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(12);
        b.Property(x => x.Comentario).HasMaxLength(500);
        b.Property(x => x.NotaRevision).HasMaxLength(500);
        b.HasIndex(x => new { x.PublicacionId, x.ReportadoPorId }).IsUnique();
        b.HasOne(x => x.Publicacion).WithMany().HasForeignKey(x => x.PublicacionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ReportadoPor).WithMany().HasForeignKey(x => x.ReportadoPorId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.RevisadoPor).WithMany().HasForeignKey(x => x.RevisadoPorId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal class AuditoriaAdminConfiguration : IEntityTypeConfiguration<AuditoriaAdmin>
{
    public void Configure(EntityTypeBuilder<AuditoriaAdmin> b)
    {
        b.ToTable("auditoria_admin");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Accion).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Entidad).HasMaxLength(40);
        b.Property(x => x.Detalle).HasColumnType("jsonb");
        b.HasOne(x => x.Admin).WithMany().HasForeignKey(x => x.AdminId).OnDelete(DeleteBehavior.SetNull);
    }
}
