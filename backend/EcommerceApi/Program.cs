using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using EcommerceApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Agregar variables de entorno como fuente de configuración
builder.Configuration.AddEnvironmentVariables();

// Helper para obtener configuración (primero env vars, luego appsettings)
string GetConfig(string key) =>
    Environment.GetEnvironmentVariable(key.Replace(":", "__").Replace(".", "_"))
    ?? builder.Configuration[key]
    ?? "";

// Configurar DbContext con PostgreSQL
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string? connectionString;

if (!string.IsNullOrEmpty(databaseUrl) && databaseUrl.StartsWith("postgres"))
{
    // Convertir formato URI de Render/Railway a connection string de .NET
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    var port = uri.Port > 0 ? uri.Port : 5432; // Puerto por defecto de PostgreSQL
    connectionString = $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}
else
{
    connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? builder.Configuration.GetConnectionString("DefaultConnection");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configurar JWT Authentication
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JWT Secret no configurado. Configure JWT_SECRET o JwtSettings:Secret");

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? builder.Configuration["JwtSettings:Issuer"]
    ?? "EcommerceApi";

var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? builder.Configuration["JwtSettings:Audience"]
    ?? "EcommerceClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization();

// Configurar CORS
var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
    ?? builder.Configuration["FrontendUrl"]
    ?? "http://localhost:4200";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(frontendUrl.Split(','))
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });

    // Política permisiva para desarrollo y producción inicial
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// Registrar HttpClient para AndreaniService
builder.Services.AddHttpClient<EcommerceApi.Services.AndreaniService>();

// Registrar HttpClientFactory para OAuth y otros servicios
builder.Services.AddHttpClient();

// Registrar servicio de envíos simulado
builder.Services.AddSingleton<EcommerceApi.Services.MockShippingService>();

// Registrar servicio de MercadoPago
builder.Services.AddScoped<EcommerceApi.Services.MercadoPagoService>();

// Registrar servicio de Encriptación
builder.Services.AddSingleton<EcommerceApi.Services.EncryptionService>();

// Registrar servicio de Cloudinary
builder.Services.AddSingleton<EcommerceApi.Services.CloudinaryService>();

// Registrar servicio de Tiendas (multi-tenant)
builder.Services.AddScoped<EcommerceApi.Services.TiendaService>();

// Registrar servicio de Email (SMTP legacy)
builder.Services.AddScoped<EcommerceApi.Services.EmailService>();

// Registrar servicio de Email con Brevo (recomendado)
builder.Services.AddScoped<EcommerceApi.Services.BrevoEmailService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aplicar migraciones automáticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Verificando base de datos...");

        // Primero intentar EnsureCreated si las tablas no existen
        var canConnect = await db.Database.CanConnectAsync();
        logger.LogInformation("Conexión a BD: {CanConnect}", canConnect);

        // Aplicar migraciones pendientes
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        logger.LogInformation("Migraciones pendientes: {Count}", pendingMigrations.Count());

        if (pendingMigrations.Any())
        {
            logger.LogInformation("Aplicando migraciones...");
            await db.Database.MigrateAsync();
            logger.LogInformation("Migraciones aplicadas correctamente");
        }
        else
        {
            // Si no hay migraciones pendientes pero las tablas no existen, forzar creación
            logger.LogInformation("No hay migraciones pendientes, verificando tablas...");
            await db.Database.EnsureCreatedAsync();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al aplicar migraciones: {Message}", ex.Message);
    }
}

// Swagger habilitado siempre
app.UseSwagger();
app.UseSwaggerUI();

// CORS - usar AllowAll temporalmente para debugging
app.UseCors("AllowAll");

if (!app.Environment.IsDevelopment())
{
    // HTTPS redirect en producción
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapControllers();

app.Run();
