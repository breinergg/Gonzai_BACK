using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gonzai_API.Migrations
{
    /// <inheritdoc />
    public partial class VentasComprasLotesVariantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Los movimientos antiguos no tienen variante, existencia ni usuario: no se pueden conservar.
            // El stock actual de cada producto se reconstruye al final como inventario inicial.
            migrationBuilder.Sql("DELETE FROM \"MovimientoInventario\";");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientoInventario_Producto_ProductoId",
                table: "MovimientoInventario");

            migrationBuilder.DropTable(
                name: "MovimientoCliente");

            migrationBuilder.DropTable(
                name: "VentaDiaria");

            migrationBuilder.RenameColumn(
                name: "ProductoId",
                table: "MovimientoInventario",
                newName: "UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_MovimientoInventario_ProductoId",
                table: "MovimientoInventario",
                newName: "IX_MovimientoInventario_UsuarioId");

            migrationBuilder.AddColumn<string>(
                name: "Marca",
                table: "Producto",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompraProveedorId",
                table: "MovimientoInventario",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoUnitario",
                table: "MovimientoInventario",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ExistenciaLoteId",
                table: "MovimientoInventario",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Motivo",
                table: "MovimientoInventario",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProductoVarianteId",
                table: "MovimientoInventario",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StockResultante",
                table: "MovimientoInventario",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VentaClienteId",
                table: "MovimientoInventario",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductoVariante",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    Talla = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Color = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PrecioVenta = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    PrecioCompra = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    StockActual = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductoVariante", x => x.Id);
                    table.CheckConstraint("CK_ProductoVariante_Stock", "\"StockActual\" >= 0");
                    table.ForeignKey(
                        name: "FK_ProductoVariante_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Proveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Documento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Direccion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedor", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VentaCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontoPagado = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SaldoPendiente = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EstadoPago = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsuarioAnulacionId = table.Column<int>(type: "integer", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentaCliente", x => x.Id);
                    table.CheckConstraint("CK_VentaCliente_Montos", "\"MontoPagado\" >= 0 AND \"MontoPagado\" <= \"Total\"");
                    table.ForeignKey(
                        name: "FK_VentaCliente_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaCliente_Usuario_UsuarioAnulacionId",
                        column: x => x.UsuarioAnulacionId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaCliente_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompraProveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProveedorId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NumeroFactura = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    SubtotalProductos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CostoEnvio = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsuarioAnulacionId = table.Column<int>(type: "integer", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompraProveedor", x => x.Id);
                    table.CheckConstraint("CK_CompraProveedor_CostoEnvio", "\"CostoEnvio\" >= 0");
                    table.ForeignKey(
                        name: "FK_CompraProveedor_Proveedor_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompraProveedor_Usuario_UsuarioAnulacionId",
                        column: x => x.UsuarioAnulacionId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompraProveedor_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PagoCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VentaClienteId = table.Column<int>(type: "integer", nullable: false),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MetodoPago = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsuarioAnulacionId = table.Column<int>(type: "integer", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagoCliente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagoCliente_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagoCliente_Usuario_UsuarioAnulacionId",
                        column: x => x.UsuarioAnulacionId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagoCliente_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagoCliente_VentaCliente_VentaClienteId",
                        column: x => x.VentaClienteId,
                        principalTable: "VentaCliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompraProveedorDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompraProveedorId = table.Column<int>(type: "integer", nullable: false),
                    ProductoVarianteId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompraProveedorDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompraProveedorDetalle_CompraProveedor_CompraProveedorId",
                        column: x => x.CompraProveedorId,
                        principalTable: "CompraProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompraProveedorDetalle_ProductoVariante_ProductoVarianteId",
                        column: x => x.ProductoVarianteId,
                        principalTable: "ProductoVariante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VentaClienteDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VentaClienteId = table.Column<int>(type: "integer", nullable: false),
                    ProductoVarianteId = table.Column<int>(type: "integer", nullable: false),
                    CompraProveedorSolicitadaId = table.Column<int>(type: "integer", nullable: true),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioVentaUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CostoTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentaClienteDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VentaClienteDetalle_CompraProveedor_CompraProveedorSolicita~",
                        column: x => x.CompraProveedorSolicitadaId,
                        principalTable: "CompraProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaClienteDetalle_ProductoVariante_ProductoVarianteId",
                        column: x => x.ProductoVarianteId,
                        principalTable: "ProductoVariante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaClienteDetalle_VentaCliente_VentaClienteId",
                        column: x => x.VentaClienteId,
                        principalTable: "VentaCliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExistenciaLote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductoVarianteId = table.Column<int>(type: "integer", nullable: false),
                    CompraProveedorDetalleId = table.Column<int>(type: "integer", nullable: true),
                    Origen = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CantidadInicial = table.Column<int>(type: "integer", nullable: false),
                    CantidadDisponible = table.Column<int>(type: "integer", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FechaIngreso = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExistenciaLote", x => x.Id);
                    table.CheckConstraint("CK_ExistenciaLote_CantidadDisponible", "\"CantidadDisponible\" >= 0 AND \"CantidadDisponible\" <= \"CantidadInicial\"");
                    table.ForeignKey(
                        name: "FK_ExistenciaLote_CompraProveedorDetalle_CompraProveedorDetall~",
                        column: x => x.CompraProveedorDetalleId,
                        principalTable: "CompraProveedorDetalle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExistenciaLote_ProductoVariante_ProductoVarianteId",
                        column: x => x.ProductoVarianteId,
                        principalTable: "ProductoVariante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VentaDetalleExistencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VentaClienteDetalleId = table.Column<int>(type: "integer", nullable: false),
                    ExistenciaLoteId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentaDetalleExistencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VentaDetalleExistencia_ExistenciaLote_ExistenciaLoteId",
                        column: x => x.ExistenciaLoteId,
                        principalTable: "ExistenciaLote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaDetalleExistencia_VentaClienteDetalle_VentaClienteDeta~",
                        column: x => x.VentaClienteDetalleId,
                        principalTable: "VentaClienteDetalle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_CompraProveedorId",
                table: "MovimientoInventario",
                column: "CompraProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_ExistenciaLoteId",
                table: "MovimientoInventario",
                column: "ExistenciaLoteId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_Fecha",
                table: "MovimientoInventario",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_ProductoVarianteId",
                table: "MovimientoInventario",
                column: "ProductoVarianteId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_VentaClienteId",
                table: "MovimientoInventario",
                column: "VentaClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraProveedor_Codigo",
                table: "CompraProveedor",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompraProveedor_Fecha",
                table: "CompraProveedor",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_CompraProveedor_ProveedorId",
                table: "CompraProveedor",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraProveedor_UsuarioAnulacionId",
                table: "CompraProveedor",
                column: "UsuarioAnulacionId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraProveedor_UsuarioId",
                table: "CompraProveedor",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraProveedorDetalle_CompraProveedorId",
                table: "CompraProveedorDetalle",
                column: "CompraProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraProveedorDetalle_ProductoVarianteId",
                table: "CompraProveedorDetalle",
                column: "ProductoVarianteId");

            migrationBuilder.CreateIndex(
                name: "IX_ExistenciaLote_CompraProveedorDetalleId",
                table: "ExistenciaLote",
                column: "CompraProveedorDetalleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExistenciaLote_ProductoVarianteId_FechaIngreso",
                table: "ExistenciaLote",
                columns: new[] { "ProductoVarianteId", "FechaIngreso" });

            migrationBuilder.CreateIndex(
                name: "IX_PagoCliente_ClienteId",
                table: "PagoCliente",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PagoCliente_Fecha",
                table: "PagoCliente",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_PagoCliente_UsuarioAnulacionId",
                table: "PagoCliente",
                column: "UsuarioAnulacionId");

            migrationBuilder.CreateIndex(
                name: "IX_PagoCliente_UsuarioId",
                table: "PagoCliente",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PagoCliente_VentaClienteId",
                table: "PagoCliente",
                column: "VentaClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariante_ProductoId_Talla_Color",
                table: "ProductoVariante",
                columns: new[] { "ProductoId", "Talla", "Color" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariante_Sku",
                table: "ProductoVariante",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VentaCliente_ClienteId_Estado",
                table: "VentaCliente",
                columns: new[] { "ClienteId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_VentaCliente_Fecha",
                table: "VentaCliente",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_VentaCliente_UsuarioAnulacionId",
                table: "VentaCliente",
                column: "UsuarioAnulacionId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaCliente_UsuarioId",
                table: "VentaCliente",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaClienteDetalle_CompraProveedorSolicitadaId",
                table: "VentaClienteDetalle",
                column: "CompraProveedorSolicitadaId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaClienteDetalle_ProductoVarianteId",
                table: "VentaClienteDetalle",
                column: "ProductoVarianteId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaClienteDetalle_VentaClienteId",
                table: "VentaClienteDetalle",
                column: "VentaClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaDetalleExistencia_ExistenciaLoteId",
                table: "VentaDetalleExistencia",
                column: "ExistenciaLoteId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaDetalleExistencia_VentaClienteDetalleId",
                table: "VentaDetalleExistencia",
                column: "VentaClienteDetalleId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientoInventario_CompraProveedor_CompraProveedorId",
                table: "MovimientoInventario",
                column: "CompraProveedorId",
                principalTable: "CompraProveedor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientoInventario_ExistenciaLote_ExistenciaLoteId",
                table: "MovimientoInventario",
                column: "ExistenciaLoteId",
                principalTable: "ExistenciaLote",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientoInventario_ProductoVariante_ProductoVarianteId",
                table: "MovimientoInventario",
                column: "ProductoVarianteId",
                principalTable: "ProductoVariante",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientoInventario_Usuario_UsuarioId",
                table: "MovimientoInventario",
                column: "UsuarioId",
                principalTable: "Usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientoInventario_VentaCliente_VentaClienteId",
                table: "MovimientoInventario",
                column: "VentaClienteId",
                principalTable: "VentaCliente",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Cada producto existente pasa a tener una variante por defecto (sin talla ni color)
            // con su stock y costo de referencia; ese stock queda en una existencia de inventario inicial.
            migrationBuilder.Sql("""
                INSERT INTO "ProductoVariante" ("ProductoId", "PrecioCompra", "StockActual", "Activo", "FechaCreacion")
                SELECT "Id", "PrecioCompra", GREATEST("StockActual", 0), TRUE, now()
                FROM "Producto";

                INSERT INTO "ExistenciaLote"
                    ("ProductoVarianteId", "Origen", "CantidadInicial", "CantidadDisponible", "CostoUnitario", "FechaIngreso", "FechaCreacion")
                SELECT "Id", 'InventarioInicial', "StockActual", "StockActual", COALESCE("PrecioCompra", 0), now(), now()
                FROM "ProductoVariante"
                WHERE "StockActual" > 0;
                """);

            migrationBuilder.DropColumn(
                name: "PrecioCompra",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "StockActual",
                table: "Producto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientoInventario_CompraProveedor_CompraProveedorId",
                table: "MovimientoInventario");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientoInventario_ExistenciaLote_ExistenciaLoteId",
                table: "MovimientoInventario");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientoInventario_ProductoVariante_ProductoVarianteId",
                table: "MovimientoInventario");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientoInventario_Usuario_UsuarioId",
                table: "MovimientoInventario");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientoInventario_VentaCliente_VentaClienteId",
                table: "MovimientoInventario");

            migrationBuilder.DropTable(
                name: "PagoCliente");

            migrationBuilder.DropTable(
                name: "VentaDetalleExistencia");

            migrationBuilder.DropTable(
                name: "ExistenciaLote");

            migrationBuilder.DropTable(
                name: "VentaClienteDetalle");

            migrationBuilder.DropTable(
                name: "CompraProveedorDetalle");

            migrationBuilder.DropTable(
                name: "VentaCliente");

            migrationBuilder.DropTable(
                name: "CompraProveedor");

            migrationBuilder.DropTable(
                name: "ProductoVariante");

            migrationBuilder.DropTable(
                name: "Proveedor");

            migrationBuilder.DropIndex(
                name: "IX_MovimientoInventario_CompraProveedorId",
                table: "MovimientoInventario");

            migrationBuilder.DropIndex(
                name: "IX_MovimientoInventario_ExistenciaLoteId",
                table: "MovimientoInventario");

            migrationBuilder.DropIndex(
                name: "IX_MovimientoInventario_Fecha",
                table: "MovimientoInventario");

            migrationBuilder.DropIndex(
                name: "IX_MovimientoInventario_ProductoVarianteId",
                table: "MovimientoInventario");

            migrationBuilder.DropIndex(
                name: "IX_MovimientoInventario_VentaClienteId",
                table: "MovimientoInventario");

            migrationBuilder.DropColumn(
                name: "Marca",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "CompraProveedorId",
                table: "MovimientoInventario");

            migrationBuilder.DropColumn(
                name: "CostoUnitario",
                table: "MovimientoInventario");

            migrationBuilder.DropColumn(
                name: "ExistenciaLoteId",
                table: "MovimientoInventario");

            migrationBuilder.DropColumn(
                name: "Motivo",
                table: "MovimientoInventario");

            migrationBuilder.DropColumn(
                name: "ProductoVarianteId",
                table: "MovimientoInventario");

            migrationBuilder.DropColumn(
                name: "StockResultante",
                table: "MovimientoInventario");

            migrationBuilder.DropColumn(
                name: "VentaClienteId",
                table: "MovimientoInventario");

            migrationBuilder.RenameColumn(
                name: "UsuarioId",
                table: "MovimientoInventario",
                newName: "ProductoId");

            migrationBuilder.RenameIndex(
                name: "IX_MovimientoInventario_UsuarioId",
                table: "MovimientoInventario",
                newName: "IX_MovimientoInventario_ProductoId");

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioCompra",
                table: "Producto",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockActual",
                table: "Producto",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MovimientoCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TipoMovimiento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientoCliente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientoCliente_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VentaDiaria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentaDiaria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VentaDiaria_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoCliente_ClienteId",
                table: "MovimientoCliente",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaDiaria_UsuarioId",
                table: "VentaDiaria",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientoInventario_Producto_ProductoId",
                table: "MovimientoInventario",
                column: "ProductoId",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
