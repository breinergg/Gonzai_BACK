using Gonzai_API.DTOs.Categoria;
using Gonzai_API.DTOs.ChatLog;
using Gonzai_API.DTOs.Cliente;
using Gonzai_API.DTOs.CompraProveedor;
using Gonzai_API.DTOs.MovimientoInventario;
using Gonzai_API.DTOs.PagoCliente;
using Gonzai_API.DTOs.PreguntaNoReconocida;
using Gonzai_API.DTOs.Producto;
using Gonzai_API.DTOs.Proveedor;
using Gonzai_API.DTOs.Usuario;
using Gonzai_API.DTOs.VentaCliente;
using Gonzai_API.Models;
using Mapster;

namespace Gonzai_API.Mapping;

public class MappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Cliente
        config.NewConfig<Cliente, ClienteResponseDto>();
        config.NewConfig<ClienteCreateDto, Cliente>();
        config.NewConfig<ClienteUpdateDto, Cliente>();

        // Usuario (nunca mapear PasswordHash hacia afuera)
        config.NewConfig<Usuario, UsuarioResponseDto>();
        config.NewConfig<UsuarioCreateDto, Usuario>()
            .Ignore(dest => dest.PasswordHash);
        config.NewConfig<UsuarioUpdateDto, Usuario>()
            .Ignore(dest => dest.PasswordHash);

        // Categoria
        config.NewConfig<Categoria, CategoriaResponseDto>();
        config.NewConfig<CategoriaCreateDto, Categoria>();
        config.NewConfig<CategoriaUpdateDto, Categoria>();

        // Producto y variantes
        config.NewConfig<Producto, ProductoResponseDto>()
            .Map(dest => dest.CategoriaNombre, src => src.Categoria != null ? src.Categoria.Nombre : null)
            .Map(dest => dest.StockTotal, src => src.Variantes.Sum(v => v.StockActual));
        config.NewConfig<ProductoVariante, ProductoVarianteResponseDto>()
            .Map(dest => dest.ProductoNombre, src => src.Producto.Nombre)
            .Map(dest => dest.PrecioVenta, src => src.PrecioVenta ?? src.Producto.PrecioVenta)
            .Map(dest => dest.TienePrecioPropio, src => src.PrecioVenta != null);

        // Proveedor
        config.NewConfig<Proveedor, ProveedorResponseDto>();
        config.NewConfig<ProveedorCreateDto, Proveedor>();
        config.NewConfig<ProveedorUpdateDto, Proveedor>();

        // CompraProveedor
        config.NewConfig<CompraProveedor, CompraProveedorResponseDto>()
            .Map(dest => dest.ProveedorNombre, src => src.Proveedor.Nombre)
            .Map(dest => dest.UsuarioNombre, src => src.Usuario.Nombre)
            .Map(dest => dest.TotalUnidades, src => src.Detalles.Sum(d => d.Cantidad));
        config.NewConfig<CompraProveedorDetalle, CompraProveedorDetalleResponseDto>()
            .Map(dest => dest.ProductoId, src => src.ProductoVariante.ProductoId)
            .Map(dest => dest.ProductoNombre, src => src.ProductoVariante.Producto.Nombre)
            .Map(dest => dest.VarianteNombre, src => src.ProductoVariante.NombreCompleto)
            .Map(dest => dest.Talla, src => src.ProductoVariante.Talla)
            .Map(dest => dest.Color, src => src.ProductoVariante.Color)
            .Map(dest => dest.ExistenciaId, src => src.Existencia != null ? src.Existencia.Id : (int?)null)
            .Map(dest => dest.CantidadDisponible, src => src.Existencia != null ? src.Existencia.CantidadDisponible : (int?)null);

        // VentaCliente
        config.NewConfig<VentaCliente, VentaClienteResponseDto>()
            .Map(dest => dest.ClienteNombre, src => src.Cliente.Nombre)
            .Map(dest => dest.UsuarioNombre, src => src.Usuario.Nombre)
            .Map(dest => dest.CostoTotal, src => src.Detalles.Sum(d => d.CostoTotal));
        config.NewConfig<VentaClienteDetalle, VentaClienteDetalleResponseDto>()
            .Map(dest => dest.ProductoId, src => src.ProductoVariante.ProductoId)
            .Map(dest => dest.ProductoNombre, src => src.ProductoVariante.Producto.Nombre)
            .Map(dest => dest.VarianteNombre, src => src.ProductoVariante.NombreCompleto)
            .Map(dest => dest.Talla, src => src.ProductoVariante.Talla)
            .Map(dest => dest.Color, src => src.ProductoVariante.Color)
            .Map(dest => dest.LoteSolicitadoCodigo,
                src => src.CompraProveedorSolicitada != null ? src.CompraProveedorSolicitada.Codigo : null);
        config.NewConfig<VentaDetalleExistencia, VentaDetalleExistenciaResponseDto>()
            .Map(dest => dest.ExistenciaId, src => src.ExistenciaLoteId)
            .Map(dest => dest.CompraProveedorId,
                src => src.ExistenciaLote.CompraProveedorDetalle != null
                    ? src.ExistenciaLote.CompraProveedorDetalle.CompraProveedorId : (int?)null)
            .Map(dest => dest.LoteCodigo,
                src => src.ExistenciaLote.CompraProveedorDetalle != null
                    ? src.ExistenciaLote.CompraProveedorDetalle.CompraProveedor.Codigo : null);
        config.NewConfig<PagoCliente, VentaClientePagoResumenDto>();

        // PagoCliente
        config.NewConfig<PagoCliente, PagoClienteResponseDto>()
            .Map(dest => dest.ClienteNombre, src => src.Cliente.Nombre)
            .Map(dest => dest.UsuarioNombre, src => src.Usuario.Nombre)
            .Map(dest => dest.SaldoPendienteVenta, src => src.VentaCliente.SaldoPendiente);

        // MovimientoInventario: mapear nombres desde navigation properties
        config.NewConfig<MovimientoInventario, MovimientoInventarioResponseDto>()
            .Map(dest => dest.ProductoId, src => src.ProductoVariante.ProductoId)
            .Map(dest => dest.ProductoNombre, src => src.ProductoVariante.Producto.Nombre)
            .Map(dest => dest.VarianteNombre, src => src.ProductoVariante.NombreCompleto)
            .Map(dest => dest.ExistenciaId, src => src.ExistenciaLoteId)
            .Map(dest => dest.LoteCodigo,
                src => src.ExistenciaLote.CompraProveedorDetalle != null
                    ? src.ExistenciaLote.CompraProveedorDetalle.CompraProveedor.Codigo : null)
            .Map(dest => dest.UsuarioNombre, src => src.Usuario.Nombre);

        // ChatLog
        config.NewConfig<ChatLog, ChatLogResponseDto>();
        config.NewConfig<ChatLogCreateDto, ChatLog>();

        // PreguntaNoReconocida
        config.NewConfig<PreguntaNoReconocida, PreguntaNoReconocidaResponseDto>();
        config.NewConfig<PreguntaNoReconocidaCreateDto, PreguntaNoReconocida>();
    }
}
