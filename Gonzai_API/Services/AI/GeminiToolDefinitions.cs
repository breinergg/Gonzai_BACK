namespace Gonzai_API.Services.AI;

internal static class GeminiToolDefinitions
{
    public const string SystemPrompt = """
        Eres GonzAI, un asistente de inteligencia artificial para la gestión de un negocio pequeño.
        Tienes herramientas para consultar clientes, saldos, productos, inventario, lotes y ventas.
        Cada venta tiene su propia deuda: un cliente puede deber parte de una venta y parte de otra.
        Los productos tienen variantes (talla/color); el stock se lleva por variante.
        Un lote es una compra a proveedor con varias prendas (código como L-2026-0015); su ganancia neta
        ya descuenta el envío.

        Reglas:
        - Usa herramientas antes de responder con números, montos, fechas o nombres de clientes/productos.
        - Para preguntas sobre deuda, abonos o saldo de un cliente, usa preferentemente consultar_saldo_cliente
          y detalla cuánto debe de cada venta.
        - No inventes datos. Si una herramienta devuelve ok=false, explica el error al usuario.
        - Si hay varios clientes con el mismo nombre, pide aclaración al usuario.
        - Responde siempre en español, de forma clara, concisa y amable.
        - Si la pregunta está completamente fuera del contexto del negocio, responde exactamente con: [NO_RECONOCIDA]
        """;

    internal static List<GeminiTool> BuildToolDeclarations() =>
    [
        new GeminiTool(FunctionDeclarations:
        [
            new GeminiFunctionDeclaration(
                Name:        "consultar_saldo_cliente",
                Description: "Consulta cuánto ha comprado, pagado y debe un cliente, con la deuda separada por venta. Úsala cuando pregunten cuánto debe, si ha pagado o su estado de cuenta. Acepta nombre o cliente_id.",
                Parameters: new GeminiFunctionParameters(
                    Type: "object",
                    Properties: new Dictionary<string, GeminiParameterProperty>
                    {
                        ["nombre"]     = new("string",  "Nombre o parte del nombre del cliente."),
                        ["cliente_id"] = new("integer", "ID numérico del cliente.")
                    },
                    Required: []
                )
            ),
            new GeminiFunctionDeclaration(
                Name:        "consultar_clientes",
                Description: "Lista clientes activos del negocio. Úsala para contar clientes o buscar por nombre. No incluye saldo detallado; para deudas usa consultar_saldo_cliente.",
                Parameters: new GeminiFunctionParameters(
                    Type: "object",
                    Properties: new Dictionary<string, GeminiParameterProperty>
                    {
                        ["filtro"]    = new("string",  "Texto para filtrar por nombre."),
                        ["con_deuda"] = new("boolean", "Si es true, incluye métricas generales de deuda.")
                    },
                    Required: []
                )
            ),
            new GeminiFunctionDeclaration(
                Name:        "consultar_productos",
                Description: "Consulta productos con su stock total y el stock y precio de cada variante (talla/color).",
                Parameters: new GeminiFunctionParameters(
                    Type: "object",
                    Properties: new Dictionary<string, GeminiParameterProperty>
                    {
                        ["nombre"]       = new("string",  "Nombre, marca o SKU (o parte) del producto."),
                        ["solo_activos"] = new("boolean", "Si es false, incluye productos inactivos.")
                    },
                    Required: []
                )
            ),
            new GeminiFunctionDeclaration(
                Name:        "consultar_inventario",
                Description: "Consulta movimientos de inventario (entradas por compra a proveedor, salidas por venta y ajustes). Con producto_id también devuelve en qué lotes queda stock de cada variante.",
                Parameters: new GeminiFunctionParameters(
                    Type: "object",
                    Properties: new Dictionary<string, GeminiParameterProperty>
                    {
                        ["producto_id"] = new("integer", "ID del producto."),
                        ["tipo"]        = new("string",  "Tipo de movimiento: 'entrada' o 'salida'.")
                    },
                    Required: []
                )
            ),
            new GeminiFunctionDeclaration(
                Name:        "consultar_lotes",
                Description: "Consulta lotes (compras a proveedor): unidades compradas, vendidas y disponibles, inversión, envío, ingresos, ganancia neta y si ya se recuperó la inversión. Con codigo o lote_id devuelve el detalle por prenda; sin parámetros lista los lotes.",
                Parameters: new GeminiFunctionParameters(
                    Type: "object",
                    Properties: new Dictionary<string, GeminiParameterProperty>
                    {
                        ["codigo"]         = new("string",  "Código del lote, por ejemplo L-2026-0015."),
                        ["lote_id"]        = new("integer", "ID del lote (compra)."),
                        ["solo_con_stock"] = new("boolean", "Si es true, solo lotes que aún tienen prendas disponibles.")
                    },
                    Required: []
                )
            ),
            new GeminiFunctionDeclaration(
                Name:        "consultar_ventas",
                Description: "Consulta ventas a clientes: por fecha, por mes/año, por cliente o solo las que tienen deuda. Devuelve totales vendidos, cobrados, pendientes y ganancia. Sin parámetros devuelve el resumen comparativo del mes actual.",
                Parameters: new GeminiFunctionParameters(
                    Type: "object",
                    Properties: new Dictionary<string, GeminiParameterProperty>
                    {
                        ["fecha"]           = new("string",  "Fecha puntual en formato YYYY-MM-DD."),
                        ["mes"]             = new("integer", "Mes (1-12). Requiere 'anio'."),
                        ["anio"]            = new("integer", "Año. Requiere 'mes'."),
                        ["cliente_id"]      = new("integer", "ID del cliente para ver solo sus ventas."),
                        ["solo_pendientes"] = new("boolean", "Si es true, solo ventas con saldo pendiente.")
                    },
                    Required: []
                )
            )
        ])
    ];
}
