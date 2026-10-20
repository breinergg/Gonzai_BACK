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
    public DbSet<ProductoVariante> ProductoVariantes => Set<ProductoVariante>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<CompraProveedor> ComprasProveedor => Set<CompraProveedor>();
    public DbSet<CompraProveedorDetalle> ComprasProveedorDetalle => Set<CompraProveedorDetalle>();
    public DbSet<ExistenciaLote> ExistenciasLote => Set<ExistenciaLote>();
    public DbSet<VentaCliente> VentasCliente => Set<VentaCliente>();
    public DbSet<VentaClienteDetalle> VentasClienteDetalle => Set<VentaClienteDetalle>();
    public DbSet<VentaDetalleExistencia> VentasDetalleExistencia => Set<VentaDetalleExistencia>();
    public DbSet<PagoCliente> PagosCliente => Set<PagoCliente>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<ChatLog> ChatLogs => Set<ChatLog>();
    public DbSet<PreguntaNoReconocida> PreguntasNoReconocidas => Set<PreguntaNoReconocida>();
    public DbSet<Negocio> Negocios => Set<Negocio>();
    public DbSet<ComprobanteEnlace> ComprobantesEnlace => Set<ComprobanteEnlace>();

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

        modelBuilder.Entity<ProductoVariante>(e =>
        {
            e.HasOne(v => v.Producto).WithMany(p => p.Variantes)
                .HasForeignKey(v => v.ProductoId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(v => new { v.ProductoId, v.Talla, v.Color });
            e.HasIndex(v => v.Sku).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_ProductoVariante_Stock", "\"StockActual\" >= 0"));
        });

        // ChatLog -> Usuario: ON DELETE SET NULL
        modelBuilder.Entity<ChatLog>()
            .HasOne(c => c.Usuario)
            .WithMany(u => u.ChatLogs)
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        // Documentos de compra/venta, lotes y pagos nunca se borran en cascada:
        // se anulan para conservar la auditoría.
        modelBuilder.Entity<CompraProveedor>(e =>
        {
            e.HasOne(c => c.Proveedor).WithMany(p => p.Compras)
                .HasForeignKey(c => c.ProveedorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Usuario).WithMany()
                .HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.UsuarioAnulacion).WithMany()
                .HasForeignKey(c => c.UsuarioAnulacionId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(c => c.Fecha);
            e.HasIndex(c => c.Codigo).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_CompraProveedor_CostoEnvio", "\"CostoEnvio\" >= 0"));
        });

        modelBuilder.Entity<CompraProveedorDetalle>(e =>
        {
            e.HasOne(d => d.CompraProveedor).WithMany(c => c.Detalles)
                .HasForeignKey(d => d.CompraProveedorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.ProductoVariante).WithMany()
                .HasForeignKey(d => d.ProductoVarianteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExistenciaLote>(e =>
        {
            e.HasOne(l => l.ProductoVariante).WithMany(v => v.Existencias)
                .HasForeignKey(l => l.ProductoVarianteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.CompraProveedorDetalle).WithOne(d => d.Existencia)
                .HasForeignKey<ExistenciaLote>(l => l.CompraProveedorDetalleId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(l => new { l.ProductoVarianteId, l.FechaIngreso });
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_ExistenciaLote_CantidadDisponible",
                    "\"CantidadDisponible\" >= 0 AND \"CantidadDisponible\" <= \"CantidadInicial\"");
            });
        });

        modelBuilder.Entity<VentaCliente>(e =>
        {
            e.HasOne(v => v.Cliente).WithMany(c => c.Ventas)
                .HasForeignKey(v => v.ClienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(v => v.Usuario).WithMany()
                .HasForeignKey(v => v.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(v => v.UsuarioAnulacion).WithMany()
                .HasForeignKey(v => v.UsuarioAnulacionId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(v => v.Fecha);
            e.HasIndex(v => new { v.ClienteId, v.Estado });
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_VentaCliente_Montos",
                    "\"MontoPagado\" >= 0 AND \"MontoPagado\" <= \"Total\"");
            });
        });

        modelBuilder.Entity<VentaClienteDetalle>(e =>
        {
            e.HasOne(d => d.VentaCliente).WithMany(v => v.Detalles)
                .HasForeignKey(d => d.VentaClienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.ProductoVariante).WithMany()
                .HasForeignKey(d => d.ProductoVarianteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.CompraProveedorSolicitada).WithMany()
                .HasForeignKey(d => d.CompraProveedorSolicitadaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VentaDetalleExistencia>(e =>
        {
            e.HasOne(c => c.VentaClienteDetalle).WithMany(d => d.Consumos)
                .HasForeignKey(c => c.VentaClienteDetalleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.ExistenciaLote).WithMany()
                .HasForeignKey(c => c.ExistenciaLoteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PagoCliente>(e =>
        {
            e.HasOne(p => p.VentaCliente).WithMany(v => v.Pagos)
                .HasForeignKey(p => p.VentaClienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Cliente).WithMany(c => c.Pagos)
                .HasForeignKey(p => p.ClienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Usuario).WithMany()
                .HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.UsuarioAnulacion).WithMany()
                .HasForeignKey(p => p.UsuarioAnulacionId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(p => p.Fecha);
        });

        modelBuilder.Entity<MovimientoInventario>(e =>
        {
            e.HasOne(m => m.ProductoVariante).WithMany(v => v.MovimientosInventario)
                .HasForeignKey(m => m.ProductoVarianteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.ExistenciaLote).WithMany()
                .HasForeignKey(m => m.ExistenciaLoteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.CompraProveedor).WithMany()
                .HasForeignKey(m => m.CompraProveedorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.VentaCliente).WithMany()
                .HasForeignKey(m => m.VentaClienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Usuario).WithMany()
                .HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(m => m.Fecha);
        });

        modelBuilder.Entity<ComprobanteEnlace>(e =>
        {
            e.HasIndex(c => c.Token).IsUnique();
            e.HasOne(c => c.VentaCliente).WithMany()
                .HasForeignKey(c => c.VentaClienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.PagoCliente).WithMany()
                .HasForeignKey(c => c.PagoClienteId).OnDelete(DeleteBehavior.Restrict);
        });
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
