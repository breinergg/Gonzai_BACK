namespace Gonzai_API.Services.AI;

internal static class GeminiToolDefinitions
{
    public const string SystemPrompt = """
        Eres GonzAI, un asistente de inteligencia artificial para la gestión de un negocio pequeño.
        Tienes herramientas para consultar clientes, saldos, productos, inventario, ventas diarias y movimientos de clientes.

        Reglas:
        - Usa herramientas antes de responder con números, montos, fechas o nombres de clientes/productos.
        - Para preguntas sobre deuda, abonos o saldo de un cliente, usa preferentemente consultar_saldo_cliente.
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
                Description: "Consulta deuda, abonos y saldo pendiente de un cliente. Úsala cuando pregunten cuánto debe, si ha pagado o su estado de cuenta. Acepta nombre o cliente_id.",
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
                Description: "Consulta productos con precios y stock disponible.",
                Parameters: new GeminiFunctionParameters(
                    Type: "object",
                    Properties: new Dictionary<string, GeminiParameterProperty>
                    {
                        ["nombre"]       = new("string",  "Nombre o parte del nombre del producto."),
                        ["solo_activos"] = new("boolean", "Si es false, incluye productos inactivos.")
                    },
                    Required: []
                )
            ),
            new GeminiFunctionDeclaration(
                Name:        "consultar_inventario",
                Description: "Consulta movimientos de inventario (entradas y salidas).",
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
                Name:        "consultar_ventas_diarias",
                Description: "Consulta ventas por fecha, por mes/año o resumen comparativo del mes actual.",
                Parameters: new GeminiFunctionParameters(
                    Type: "object",
                    Properties: new Dictionary<string, GeminiParameterProperty>
                    {
                        ["fecha"] = new("string",  "Fecha puntual en formato YYYY-MM-DD."),
                        ["mes"]   = new("integer", "Mes (1-12). Requiere 'anio'."),
                        ["anio"]  = new("integer", "Año. Requiere 'mes'.")
                    },
                    Required: []
                )
            ),
            new GeminiFunctionDeclaration(
                Name:        "consultar_movimientos_cliente",
                Description: "Lista el historial de movimientos (deudas y abonos) de un cliente por ID.",
                Parameters: new GeminiFunctionParameters(
                    Type: "object",
                    Properties: new Dictionary<string, GeminiParameterProperty>
                    {
                        ["cliente_id"] = new("integer", "ID del cliente.")
                    },
                    Required: ["cliente_id"]
                )
            )
        ])
    ];
}
