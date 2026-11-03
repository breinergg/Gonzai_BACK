using System.Data.Common;
using System.Runtime.CompilerServices;
using Gonzai_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;

namespace Gonzai_API.Data;

/// <summary>
/// Limpia la caché del dashboard cuando se guardan cambios que la afectan
/// (clientes, ventas, pagos, productos). Si hay una transacción abierta, espera al commit
/// para que ningún lector vuelva a cachear datos viejos entre el guardado y el commit.
/// </summary>
public class DashboardCacheInterceptor : SaveChangesInterceptor, IDbTransactionInterceptor
{
    public static readonly string[] Claves =
    [
        "dashboard:mayor-deuda",
        "dashboard:total-deuda",
        "dashboard:clientes-con-deuda",
        "dashboard:clientes-activos",
        "dashboard:resumen-mensual",
        "dashboard:productos-activos"
    ];

    private static readonly Type[] TiposRelevantes =
    [
        typeof(Cliente), typeof(VentaCliente), typeof(VentaClienteDetalle), typeof(PagoCliente),
        typeof(Producto), typeof(ProductoVariante)
    ];

    private readonly IMemoryCache _cache;
    private readonly ConditionalWeakTable<DbContext, StrongBox<bool>> _pendientes = new();

    public DashboardCacheInterceptor(IMemoryCache cache) => _cache = cache;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Marcar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Marcar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Confirmar(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Confirmar(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData) =>
        Invalidar(eventData.Context);

    public Task TransactionCommittedAsync(
        DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Invalidar(eventData.Context);
        return Task.CompletedTask;
    }

    private void Marcar(DbContext? contexto)
    {
        if (contexto is null) return;
        var afecta = contexto.ChangeTracker.Entries().Any(e =>
            e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
            && TiposRelevantes.Contains(e.Entity.GetType()));
        if (afecta) _pendientes.GetOrCreateValue(contexto).Value = true;
    }

    private void Confirmar(DbContext? contexto)
    {
        if (contexto is null) return;
        if (contexto.Database.CurrentTransaction is null) Invalidar(contexto);
    }

    private void Invalidar(DbContext? contexto)
    {
        if (contexto is null) return;
        if (!_pendientes.TryGetValue(contexto, out var marca) || !marca.Value) return;
        marca.Value = false;
        foreach (var clave in Claves) _cache.Remove(clave);
    }
}
