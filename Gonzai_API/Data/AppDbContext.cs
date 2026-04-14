using Gonzai_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoCliente> MovimientosCliente => Set<MovimientoCliente>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<VentaDiaria> VentasDiarias => Set<VentaDiaria>();
    public DbSet<ChatLog> ChatLogs => Set<ChatLog>();
    public DbSet<PreguntaNoReconocida> PreguntasNoReconocidas => Set<PreguntaNoReconocida>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Usuario: email único
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Producto -> Categoria: ON DELETE SET NULL
        modelBuilder.Entity<Producto>()
            .HasOne(p => p.Categoria)
            .WithMany(c => c.Productos)
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.SetNull);

        // MovimientoCliente -> Cliente: ON DELETE RESTRICT
        modelBuilder.Entity<MovimientoCliente>()
            .HasOne(m => m.Cliente)
            .WithMany(c => c.MovimientosCliente)
            .HasForeignKey(m => m.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        // MovimientoInventario -> Producto: ON DELETE RESTRICT
        modelBuilder.Entity<MovimientoInventario>()
            .HasOne(m => m.Producto)
            .WithMany(p => p.MovimientosInventario)
            .HasForeignKey(m => m.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        // VentaDiaria -> Usuario: ON DELETE SET NULL
        modelBuilder.Entity<VentaDiaria>()
            .HasOne(v => v.Usuario)
            .WithMany(u => u.VentasDiarias)
            .HasForeignKey(v => v.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        // ChatLog -> Usuario: ON DELETE SET NULL
        modelBuilder.Entity<ChatLog>()
            .HasOne(c => c.Usuario)
            .WithMany(u => u.ChatLogs)
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified)
            {
                var prop = entry.Properties
                    .FirstOrDefault(p => p.Metadata.Name == "FechaActualizacion");

                if (prop is not null)
                    prop.CurrentValue = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
