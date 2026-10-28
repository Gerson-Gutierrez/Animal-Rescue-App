using AnimalRescue.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnimalRescue.DAL.Context;

public class AnimalRescueDbContext(DbContextOptions<AnimalRescueDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Publicacion> Publicaciones => Set<Publicacion>();
    public DbSet<ImagenPublicacion> ImagenesPublicacion => Set<ImagenPublicacion>();
    public DbSet<Conversacion> Conversaciones => Set<Conversacion>();
    public DbSet<Mensaje> Mensajes => Set<Mensaje>();
    public DbSet<HogarTransito> HogaresTransito => Set<HogarTransito>();
    public DbSet<Refugio> Refugios => Set<Refugio>();
    public DbSet<Reporte> Reportes => Set<Reporte>();
    public DbSet<AuditoriaAdmin> AuditoriaAdmin => Set<AuditoriaAdmin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AnimalRescueDbContext).Assembly);
    }
}
