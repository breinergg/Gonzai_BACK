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
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

builder.Services.AddSingleton<DashboardCacheInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(sp.GetRequiredService<DashboardCacheInterceptor>()));

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("postgres");

// Detrás de un proxy inverso (nginx, balanceador) la IP y el esquema reales llegan en X-Forwarded-*.
// Actívalo solo si la API NO es accesible directamente desde internet; si no, X-Forwarded-For se podría falsificar.
if (builder.Configuration.GetValue("ForwardedHeaders:Enabled", false))
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
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
        sp.GetRequiredService<IGeminiFunctionDispatcher>()
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

    // AI Chat: máx. 20 peticiones por usuario cada 60 segundos
    options.AddPolicy("ai-chat", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
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

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").DisableRateLimiting().AllowAnonymous();

await app.RunAsync();

public partial class Program { }
