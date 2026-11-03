using System.Text;
using System.Threading.RateLimiting;
using QuestPDF.Infrastructure;
using Gonzai_API.Data;
using Gonzai_API.Mapping;
using Gonzai_API.Middleware;
using Gonzai_API.Services;
using Gonzai_API.Services.AI;
using Gonzai_API.Services.AI.Functions;
using Gonzai_API.Services.Comprobantes;
using Gonzai_API.Services.Email;
using Gonzai_API.Services.Interfaces;
using Gonzai_API.Services.Tenancy;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// La clave JWT es obligatoria y debe ser larga: con una débil se podrían falsificar tokens de Admin.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException(
        "Jwt:Key no está configurada o es demasiado corta (mínimo 32 caracteres). " +
        "Defínela con user-secrets (desarrollo) o la variable de entorno Jwt__Key (producción).");

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Pega el token JWT de POST /api/usuario/login (sin escribir 'Bearer')."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Catálogo de la plataforma: la lista de empresas y a qué empresa pertenece cada correo.
builder.Services.AddDbContext<PlataformaDbContext>(options =>
    options.UseNpgsql(TenantStore.CadenaCatalogo(builder.Configuration)));

// Cifra los secretos por empresa (como su clave propia de Gemini). El llavero se guarda en el
// catálogo (no en memoria ni en disco del contenedor) para que sobreviva a los reinicios:
// si se perdiera, esos secretos quedarían ilegibles para siempre.
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<PlataformaDbContext>()
    .SetApplicationName("GonzAI");

builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddScoped<ITenantStore, TenantStore>();
builder.Services.AddScoped<ITenantDbFactory, TenantDbFactory>();
builder.Services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
builder.Services.AddScoped<DashboardCacheInterceptor>();

// Una base de datos por empresa. La conexión sale de la empresa que viene firmada en el token,
// así que una petición no puede alcanzar los datos de otra empresa ni por error.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseNpgsql(EF.IsDesignTime
               // Las herramientas (dotnet ef) construyen el contexto sin ninguna petición de por medio.
               ? AppDbContextDesignFactory.Cadena()
               : sp.GetRequiredService<ITenantContext>().CadenaConexionRequerida)
           .AddInterceptors(sp.GetRequiredService<DashboardCacheInterceptor>()),
    ServiceLifetime.Scoped, ServiceLifetime.Scoped);

// El health check va contra el catálogo: es la única base que existe siempre, sin sesión.
builder.Services.AddHealthChecks().AddDbContextCheck<PlataformaDbContext>("postgres");

// Detrás de un proxy inverso (nginx, balanceador) la IP y el esquema reales llegan en X-Forwarded-*.
// Actívalo solo si la API NO es accesible directamente desde internet; si no, X-Forwarded-For se podría falsificar.
if (builder.Configuration.GetValue("ForwardedHeaders:Enabled", false))
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = builder.Configuration.GetValue("ForwardedHeaders:ForwardLimit", 1);
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// Mapster
var mapsterConfig = TypeAdapterConfig.GlobalSettings;
mapsterConfig.Scan(typeof(MappingConfig).Assembly);
builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();

// AI
builder.Services.AddHttpClient("Gemini", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddScoped<IGeminiFunctionHandler, ConsultarSaldoClienteHandler>();
builder.Services.AddScoped<IGeminiFunctionHandler, ConsultarClientesHandler>();
builder.Services.AddScoped<IGeminiFunctionHandler, ConsultarProductosHandler>();
builder.Services.AddScoped<IGeminiFunctionHandler, ConsultarLotesHandler>();
builder.Services.AddScoped<IGeminiFunctionHandler, ConsultarInventarioHandler>();
builder.Services.AddScoped<IGeminiFunctionHandler, ConsultarVentasHandler>();
builder.Services.AddScoped<IGeminiFunctionDispatcher, GeminiFunctionDispatcher>();
builder.Services.AddScoped<IAIService>(sp =>
    new GeminiService(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("Gemini"),
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<ILogger<GeminiService>>(),
        sp.GetRequiredService<IGeminiFunctionDispatcher>(),
        sp.GetRequiredService<INegocioService>()
    ));

// Services
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IChatLogService, ChatLogService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<IMovimientoInventarioService, MovimientoInventarioService>();
builder.Services.AddScoped<ILoteService, LoteService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();
builder.Services.AddScoped<ICompraProveedorService, CompraProveedorService>();
builder.Services.AddScoped<IVentaClienteService, VentaClienteService>();
builder.Services.AddScoped<IPagoClienteService, PagoClienteService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<INegocioService, NegocioService>();
builder.Services.AddScoped<ComprobantePdfService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IComprobanteService, ComprobanteService>();
builder.Services.AddScoped<IComprobantePublicoService, ComprobantePublicoService>();
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();
builder.Services.AddScoped<IPreguntaNoReconocidaService, PreguntaNoReconocidaService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

// Memory cache (para dashboard y resúmenes)
builder.Services.AddMemoryCache();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Login: máx. 10 intentos por IP cada 60 segundos
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(60),
                QueueLimit = 0
            }));

    // AI Chat: máx. N peticiones por usuario cada 60 segundos. El límite es configurable para
    // poder bajarlo en pruebas (RateLimiting:AiChatPermitLimit). La partición combina empresa y
    // usuario porque el Id de usuario solo es único dentro de su empresa: sin la empresa, el
    // usuario 1 de una tienda compartiría cupo con el usuario 1 de otra.
    var aiChatPermitLimit = builder.Configuration.GetValue("RateLimiting:AiChatPermitLimit", 20);
    options.AddPolicy("ai-chat", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ParticionChatIa(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = aiChatPermitLimit,
                Window = TimeSpan.FromSeconds(60),
                QueueLimit = 0
            }));

    // General: máx. 100 peticiones por IP cada 60 segundos
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromSeconds(60),
                QueueLimit = 0
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
            ? (int)retry.TotalSeconds
            : 60;
        context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString();
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            message = $"Has alcanzado el límite de solicitudes. Intenta de nuevo en {retryAfter} segundos.",
            retryAfterSeconds = retryAfter
        }, cancellationToken);
    };

    // Clave de partición del chat de IA: empresa + usuario (ambos del token ya validado), o la IP
    // si la petición aún no está autenticada.
    static string ParticionChatIa(HttpContext context)
    {
        var empresa = context.User.FindFirst(TenantMiddleware.ClaimEmpresa)?.Value;
        var usuario = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return empresa is not null && usuario is not null
            ? $"{empresa}:{usuario}"
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
});

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddAuthorization();

var corsOrigins = builder.Environment.IsDevelopment()
    ? new[] { "http://localhost:4200", "https://1f15mqrk-4200.use2.devtunnels.ms" }
    : builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];

builder.Services.AddCors(options => {
    options.AddPolicy("FrontendPolicy", policy => {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .WithHeaders("Content-Type", "Authorization")
              .WithExposedHeaders("Content-Disposition")
              .AllowCredentials();
    });
});

var app = builder.Build();

await DbInitializer.InicializarAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Configuration.GetValue("ForwardedHeaders:Enabled", false))
    app.UseForwardedHeaders();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// En producción el TLS suele terminar en el proxy: ahí se pone HttpsRedirection:Enabled=false.
if (app.Configuration.GetValue("HttpsRedirection:Enabled", true))
    app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

// Va DESPUÉS de la autenticación: así el límite del chat de IA puede particionar por el usuario
// (y su empresa) del token. Antes de autenticar, context.User está vacío y todo caería a la IP.
// Se puede apagar con RateLimiting:Enabled=false (solo para pruebas automatizadas).
if (app.Configuration.GetValue("RateLimiting:Enabled", true))
    app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health").DisableRateLimiting().AllowAnonymous();

await app.RunAsync();

public partial class Program { }
