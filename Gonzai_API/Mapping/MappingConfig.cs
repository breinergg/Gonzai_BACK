using Gonzai_API.DTOs.Categoria;
using Gonzai_API.DTOs.ChatLog;
using Gonzai_API.DTOs.Cliente;
using Gonzai_API.DTOs.MovimientoCliente;
using Gonzai_API.DTOs.MovimientoInventario;
using Gonzai_API.DTOs.PreguntaNoReconocida;
using Gonzai_API.DTOs.Producto;
using Gonzai_API.DTOs.Usuario;
using Gonzai_API.DTOs.VentaDiaria;
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

        // Producto: mapear nombre de categoria desde navigation property
        config.NewConfig<Producto, ProductoResponseDto>()
            .Map(dest => dest.CategoriaNombre, src => src.Categoria != null ? src.Categoria.Nombre : null);
        config.NewConfig<ProductoCreateDto, Producto>();
        config.NewConfig<ProductoUpdateDto, Producto>();

        // MovimientoCliente: mapear nombre del cliente desde navigation property
        config.NewConfig<MovimientoCliente, MovimientoClienteResponseDto>()
            .Map(dest => dest.ClienteNombre, src => src.Cliente.Nombre);
        config.NewConfig<MovimientoClienteCreateDto, MovimientoCliente>();

        // MovimientoInventario: mapear nombre del producto desde navigation property
        config.NewConfig<MovimientoInventario, MovimientoInventarioResponseDto>()
            .Map(dest => dest.ProductoNombre, src => src.Producto.Nombre);
        config.NewConfig<MovimientoInventarioCreateDto, MovimientoInventario>();

        // VentaDiaria: mapear nombre del usuario desde navigation property
        config.NewConfig<VentaDiaria, VentaDiariaResponseDto>()
            .Map(dest => dest.UsuarioNombre, src => src.Usuario != null ? src.Usuario.Nombre : null);
        config.NewConfig<VentaDiariaCreateDto, VentaDiaria>();
        config.NewConfig<VentaDiariaUpdateDto, VentaDiaria>();

        // ChatLog
        config.NewConfig<ChatLog, ChatLogResponseDto>();
        config.NewConfig<ChatLogCreateDto, ChatLog>();

        // PreguntaNoReconocida
        config.NewConfig<PreguntaNoReconocida, PreguntaNoReconocidaResponseDto>();
        config.NewConfig<PreguntaNoReconocidaCreateDto, PreguntaNoReconocida>();
    }
}
