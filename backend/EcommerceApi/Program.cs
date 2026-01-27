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
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

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

    // Política permisiva solo para desarrollo
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("AllowAll",
            policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
    }
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

// Aplicar migraciones automáticamente en producción
if (!app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Swagger habilitado en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("AllowFrontend");
    // HTTPS redirect en producción
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapControllers();

app.Run();
