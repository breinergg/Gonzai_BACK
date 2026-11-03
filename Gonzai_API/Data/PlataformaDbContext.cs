using Gonzai_API.Models.Plataforma;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Data;

/// <summary>
/// Base de catálogo de la plataforma: la lista de empresas y a qué empresa pertenece cada correo.
/// Es independiente de la base de cada empresa, que sigue teniendo su propio <see cref="AppDbContext"/>.
/// </summary>
public class PlataformaDbContext : DbContext
{
    public PlataformaDbContext(DbContextOptions<PlataformaDbContext> options) : base(options) { }

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<UsuarioDirectorio> UsuariosDirectorio => Set<UsuarioDirectorio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Empresa>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.BaseDatos).IsUnique();
        });

        modelBuilder.Entity<UsuarioDirectorio>(e =>
        {
            // Un correo pertenece a una sola empresa: así el login no necesita preguntar cuál.
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Empresa)
                .WithMany()
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
