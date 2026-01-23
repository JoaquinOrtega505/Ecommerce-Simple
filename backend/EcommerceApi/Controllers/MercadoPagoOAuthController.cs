using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using EcommerceApi.Data;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class MercadoPagoOAuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MercadoPagoOAuthController> _logger;
    private readonly HttpClient _httpClient;

    // MercadoPago OAuth URLs
    private const string AUTHORIZATION_URL = "https://auth.mercadopago.com/authorization";
    private const string TOKEN_URL = "https://api.mercadopago.com/oauth/token";

    public MercadoPagoOAuthController(
        AppDbContext context,
        IConfiguration configuration,
        ILogger<MercadoPagoOAuthController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    private async Task<int?> GetUserTiendaIdAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return null;
        }

        var usuario = await _context.Usuarios.FindAsync(userId);
        return usuario?.TiendaId;
    }

    /// <summary>
    /// Inicia el flujo OAuth de MercadoPago
    /// </summary>
    [HttpGet("authorize")]
    public async Task<ActionResult> Authorize()
    {
        try
        {
            var tiendaId = await GetUserTiendaIdAsync();
            if (tiendaId == null)
            {
                return BadRequest(new { message = "Usuario no asociado a ninguna tienda" });
            }

            var appId = _configuration["MercadoPago:AppId"];
            var redirectUri = _configuration["MercadoPago:RedirectUri"];

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(redirectUri))
            {
                _logger.LogError("MercadoPago OAuth configuration missing");
                return StatusCode(500, new { message = "Configuración de OAuth no disponible" });
            }

            // Construir URL de autorización
            var authUrl = $"{AUTHORIZATION_URL}?client_id={appId}&response_type=code&platform_id=mp&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={tiendaId}";

            return Ok(new
            {
                authorizationUrl = authUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al iniciar OAuth de MercadoPago");
            return StatusCode(500, new { message = "Error al iniciar el proceso de autorización" });
        }
    }

    /// <summary>
    /// Callback de OAuth - Intercambia el código por el access token
    /// </summary>
    [HttpPost("callback")]
    [AllowAnonymous] // El callback viene de MercadoPago, no tiene token JWT
    public async Task<ActionResult> Callback([FromBody] OAuthCallbackRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Code))
            {
                return BadRequest(new { message = "Código de autorización no recibido" });
            }

            if (!int.TryParse(request.State, out int tiendaId))
            {
                return BadRequest(new { message = "State inválido" });
            }

            var appId = _configuration["MercadoPago:AppId"];
            var clientSecret = _configuration["MercadoPago:ClientSecret"];
            var redirectUri = _configuration["MercadoPago:RedirectUri"];

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("MercadoPago OAuth configuration missing");
                return StatusCode(500, new { message = "Configuración de OAuth no disponible" });
            }

            // Intercambiar código por access token
            var tokenRequest = new
            {
                client_id = appId,
                client_secret = clientSecret,
                code = request.Code,
                grant_type = "authorization_code",
                redirect_uri = redirectUri
            };

            var content = new StringContent(
                JsonSerializer.Serialize(tokenRequest),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(TOKEN_URL, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error al intercambiar código por token: {Response}", responseContent);
                return StatusCode(500, new { message = "Error al obtener credenciales de MercadoPago" });
            }

            var tokenResponse = JsonSerializer.Deserialize<MercadoPagoTokenResponse>(responseContent);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                return StatusCode(500, new { message = "Respuesta de token inválida" });
            }

            // Guardar credenciales en la tienda
            var tienda = await _context.Tiendas.FindAsync(tiendaId);
            if (tienda == null)
            {
                return NotFound(new { message = "Tienda no encontrada" });
            }

            tienda.MercadoPagoAccessToken = tokenResponse.AccessToken;
            tienda.MercadoPagoPublicKey = tokenResponse.PublicKey;
            tienda.FechaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Credenciales de MercadoPago guardadas para tienda {TiendaId}", tiendaId);

            return Ok(new
            {
                success = true,
                message = "Conexión con MercadoPago establecida correctamente",
                publicKey = tokenResponse.PublicKey
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar callback de OAuth");
            return StatusCode(500, new { message = "Error al procesar la autorización" });
        }
    }

    /// <summary>
    /// Desconecta MercadoPago eliminando las credenciales
    /// </summary>
    [HttpPost("disconnect")]
    public async Task<ActionResult> Disconnect()
    {
        try
        {
            var tiendaId = await GetUserTiendaIdAsync();
            if (tiendaId == null)
            {
                return BadRequest(new { message = "Usuario no asociado a ninguna tienda" });
            }

            var tienda = await _context.Tiendas.FindAsync(tiendaId.Value);
            if (tienda == null)
            {
                return NotFound(new { message = "Tienda no encontrada" });
            }

            tienda.MercadoPagoAccessToken = null;
            tienda.MercadoPagoPublicKey = null;
            tienda.FechaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("MercadoPago desconectado para tienda {TiendaId}", tiendaId);

            return Ok(new
            {
                success = true,
                message = "MercadoPago desconectado correctamente"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desconectar MercadoPago");
            return StatusCode(500, new { message = "Error al desconectar MercadoPago" });
        }
    }

    /// <summary>
    /// Verifica si MercadoPago está conectado
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult> GetStatus()
    {
        try
        {
            var tiendaId = await GetUserTiendaIdAsync();
            if (tiendaId == null)
            {
                return BadRequest(new { message = "Usuario no asociado a ninguna tienda" });
            }

            var tienda = await _context.Tiendas.FindAsync(tiendaId.Value);
            if (tienda == null)
            {
                return NotFound(new { message = "Tienda no encontrada" });
            }

            var isConnected = !string.IsNullOrEmpty(tienda.MercadoPagoAccessToken);

            return Ok(new
            {
                connected = isConnected,
                publicKey = isConnected ? tienda.MercadoPagoPublicKey : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estado de MercadoPago");
            return StatusCode(500, new { message = "Error al verificar estado de conexión" });
        }
    }
}

public class OAuthCallbackRequest
{
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class MercadoPagoTokenResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("public_key")]
    public string PublicKey { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("live_mode")]
    public bool LiveMode { get; set; }
}
