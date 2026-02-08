using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using EcommerceApi.DTOs;
using EcommerceApi.Models;
using EcommerceApi.Services;
using System.Text.Json;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin")]
    public class SuscripcionConfigController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EncryptionService _encryptionService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<SuscripcionConfigController> _logger;
        private readonly IConfiguration _configuration;

        public SuscripcionConfigController(
            AppDbContext context,
            EncryptionService encryptionService,
            HttpClient httpClient,
            ILogger<SuscripcionConfigController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _encryptionService = encryptionService;
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        #region Configuración de Suscripciones

        /// <summary>
        /// Obtiene la configuración actual de suscripciones
        /// </summary>
        [HttpGet("configuracion")]
        public async Task<ActionResult<ConfiguracionSuscripcionesDto>> GetConfiguracion()
        {
            var config = await _context.ConfiguracionSuscripciones
                .FirstOrDefaultAsync(c => c.Activo);

            if (config == null)
            {
                // Crear configuración por defecto si no existe
                config = new ConfiguracionSuscripciones
                {
                    DiasPrueba = 7,
                    MaxReintentosPago = 3,
                    DiasGraciaSuspension = 3,
                    DiasAvisoFinTrial = 2,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };
                _context.ConfiguracionSuscripciones.Add(config);
                await _context.SaveChangesAsync();
            }

            return Ok(new ConfiguracionSuscripcionesDto
            {
                Id = config.Id,
                DiasPrueba = config.DiasPrueba,
                MaxReintentosPago = config.MaxReintentosPago,
                DiasGraciaSuspension = config.DiasGraciaSuspension,
                DiasAvisoFinTrial = config.DiasAvisoFinTrial,
                Activo = config.Activo,
                FechaCreacion = config.FechaCreacion,
                FechaModificacion = config.FechaModificacion
            });
        }

        /// <summary>
        /// Actualiza la configuración de suscripciones
        /// </summary>
        [HttpPut("configuracion")]
        public async Task<ActionResult<ConfiguracionSuscripcionesDto>> UpdateConfiguracion(
            [FromBody] UpdateConfiguracionSuscripcionesDto dto)
        {
            // Validaciones
            if (dto.DiasPrueba < 0 || dto.DiasPrueba > 30)
                return BadRequest("Los días de prueba deben estar entre 0 y 30");

            if (dto.MaxReintentosPago < 1 || dto.MaxReintentosPago > 10)
                return BadRequest("Los reintentos de pago deben estar entre 1 y 10");

            if (dto.DiasGraciaSuspension < 1 || dto.DiasGraciaSuspension > 30)
                return BadRequest("Los días de gracia deben estar entre 1 y 30");

            if (dto.DiasAvisoFinTrial < 1 || dto.DiasAvisoFinTrial > dto.DiasPrueba)
                return BadRequest("Los días de aviso deben estar entre 1 y los días de prueba");

            var config = await _context.ConfiguracionSuscripciones
                .FirstOrDefaultAsync(c => c.Activo);

            if (config == null)
            {
                return NotFound("No se encontró la configuración");
            }

            config.DiasPrueba = dto.DiasPrueba;
            config.MaxReintentosPago = dto.MaxReintentosPago;
            config.DiasGraciaSuspension = dto.DiasGraciaSuspension;
            config.DiasAvisoFinTrial = dto.DiasAvisoFinTrial;
            config.FechaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Configuración de suscripciones actualizada");

            return Ok(new ConfiguracionSuscripcionesDto
            {
                Id = config.Id,
                DiasPrueba = config.DiasPrueba,
                MaxReintentosPago = config.MaxReintentosPago,
                DiasGraciaSuspension = config.DiasGraciaSuspension,
                DiasAvisoFinTrial = config.DiasAvisoFinTrial,
                Activo = config.Activo,
                FechaCreacion = config.FechaCreacion,
                FechaModificacion = config.FechaModificacion
            });
        }

        #endregion

        #region MercadoPago Credenciales

        /// <summary>
        /// Obtiene el estado de conexión de MercadoPago
        /// </summary>
        [HttpGet("mercadopago/estado")]
        public async Task<ActionResult<MercadoPagoCredencialesDto>> GetEstadoMercadoPago()
        {
            var credenciales = await _context.MercadoPagoCredenciales.FirstOrDefaultAsync();

            if (credenciales == null)
            {
                return Ok(new MercadoPagoCredencialesDto
                {
                    Id = 0,
                    Conectado = false,
                    MercadoPagoEmail = null,
                    FechaConexion = null,
                    EsProduccion = false,
                    TokenValido = false
                });
            }

            // Verificar si el token está expirado
            bool tokenValido = credenciales.Conectado &&
                (credenciales.TokenExpiracion == null || credenciales.TokenExpiracion > DateTime.UtcNow);

            return Ok(new MercadoPagoCredencialesDto
            {
                Id = credenciales.Id,
                Conectado = credenciales.Conectado,
                MercadoPagoEmail = credenciales.MercadoPagoEmail,
                FechaConexion = credenciales.FechaConexion,
                EsProduccion = credenciales.EsProduccion,
                TokenValido = tokenValido
            });
        }

        /// <summary>
        /// Conecta MercadoPago usando credenciales manuales (Access Token y Public Key)
        /// </summary>
        [HttpPost("mercadopago/conectar")]
        public async Task<ActionResult<MercadoPagoCredencialesDto>> ConectarMercadoPago(
            [FromBody] MercadoPagoConectarManualDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.AccessToken))
                return BadRequest("El Access Token es requerido");

            if (string.IsNullOrWhiteSpace(dto.PublicKey))
                return BadRequest("La Public Key es requerida");

            try
            {
                // Validar el token haciendo una llamada a la API de MercadoPago
                var userInfo = await ValidarTokenMercadoPago(dto.AccessToken);

                if (userInfo == null)
                {
                    return BadRequest("El Access Token no es válido o ha expirado");
                }

                var credenciales = await _context.MercadoPagoCredenciales.FirstOrDefaultAsync();

                if (credenciales == null)
                {
                    credenciales = new MercadoPagoCredencialesSuperAdmin
                    {
                        FechaCreacion = DateTime.UtcNow
                    };
                    _context.MercadoPagoCredenciales.Add(credenciales);
                }

                // Encriptar y guardar credenciales
                credenciales.AccessToken = _encryptionService.Encrypt(dto.AccessToken);
                credenciales.PublicKey = _encryptionService.Encrypt(dto.PublicKey);
                credenciales.MercadoPagoUserId = userInfo.UserId;
                credenciales.MercadoPagoEmail = userInfo.Email;
                credenciales.Conectado = true;
                credenciales.FechaConexion = DateTime.UtcNow;
                credenciales.EsProduccion = dto.EsProduccion;
                credenciales.TokenExpiracion = null; // Los tokens manuales no expiran (a menos que se revoquen)

                await _context.SaveChangesAsync();

                _logger.LogInformation("MercadoPago conectado exitosamente para {Email}", userInfo.Email);

                return Ok(new MercadoPagoCredencialesDto
                {
                    Id = credenciales.Id,
                    Conectado = true,
                    MercadoPagoEmail = credenciales.MercadoPagoEmail,
                    FechaConexion = credenciales.FechaConexion,
                    EsProduccion = credenciales.EsProduccion,
                    TokenValido = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al conectar MercadoPago");
                return StatusCode(500, "Error al conectar con MercadoPago: " + ex.Message);
            }
        }

        /// <summary>
        /// Desconecta MercadoPago (elimina las credenciales)
        /// </summary>
        [HttpPost("mercadopago/desconectar")]
        public async Task<ActionResult> DesconectarMercadoPago()
        {
            var credenciales = await _context.MercadoPagoCredenciales.FirstOrDefaultAsync();

            if (credenciales == null || !credenciales.Conectado)
            {
                return BadRequest("MercadoPago no está conectado");
            }

            // Limpiar credenciales
            credenciales.AccessToken = null;
            credenciales.RefreshToken = null;
            credenciales.PublicKey = null;
            credenciales.MercadoPagoUserId = null;
            credenciales.MercadoPagoEmail = null;
            credenciales.Conectado = false;
            credenciales.FechaDesconexion = DateTime.UtcNow;
            credenciales.TokenExpiracion = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("MercadoPago desconectado");

            return Ok(new { message = "MercadoPago desconectado exitosamente" });
        }

        /// <summary>
        /// Obtiene la URL para autorización OAuth de MercadoPago (para uso futuro)
        /// </summary>
        [HttpGet("mercadopago/oauth-url")]
        public ActionResult<object> GetOAuthUrl([FromQuery] string redirectUri)
        {
            var clientId = _configuration["MercadoPago:ClientId"];

            if (string.IsNullOrEmpty(clientId))
            {
                return BadRequest("MercadoPago ClientId no está configurado");
            }

            var authUrl = $"https://auth.mercadopago.com.ar/authorization?" +
                $"client_id={clientId}&" +
                $"response_type=code&" +
                $"platform_id=mp&" +
                $"redirect_uri={Uri.EscapeDataString(redirectUri)}";

            return Ok(new { url = authUrl });
        }

        /// <summary>
        /// Procesa el callback de OAuth de MercadoPago (para uso futuro)
        /// </summary>
        [HttpPost("mercadopago/oauth-callback")]
        public async Task<ActionResult<MercadoPagoCredencialesDto>> ProcessOAuthCallback(
            [FromBody] MercadoPagoOAuthRequestDto dto)
        {
            var clientId = _configuration["MercadoPago:ClientId"];
            var clientSecret = _configuration["MercadoPago:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                return BadRequest("Credenciales de aplicación MercadoPago no configuradas");
            }

            try
            {
                // Intercambiar código por tokens
                var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "code", dto.Code },
                    { "redirect_uri", dto.RedirectUri }
                });

                var response = await _httpClient.PostAsync(
                    "https://api.mercadopago.com/oauth/token",
                    tokenRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error en OAuth: {Error}", error);
                    return BadRequest("Error al obtener tokens de MercadoPago");
                }

                var tokenResponse = await response.Content.ReadFromJsonAsync<MercadoPagoTokenResponse>();

                if (tokenResponse == null)
                {
                    return BadRequest("Respuesta inválida de MercadoPago");
                }

                // Obtener información del usuario
                var userInfo = await ValidarTokenMercadoPago(tokenResponse.AccessToken);

                var credenciales = await _context.MercadoPagoCredenciales.FirstOrDefaultAsync();

                if (credenciales == null)
                {
                    credenciales = new MercadoPagoCredencialesSuperAdmin
                    {
                        FechaCreacion = DateTime.UtcNow
                    };
                    _context.MercadoPagoCredenciales.Add(credenciales);
                }

                // Guardar credenciales encriptadas
                credenciales.AccessToken = _encryptionService.Encrypt(tokenResponse.AccessToken);
                credenciales.RefreshToken = _encryptionService.Encrypt(tokenResponse.RefreshToken ?? "");
                credenciales.PublicKey = _encryptionService.Encrypt(tokenResponse.PublicKey ?? "");
                credenciales.MercadoPagoUserId = tokenResponse.UserId?.ToString() ?? userInfo?.UserId;
                credenciales.MercadoPagoEmail = userInfo?.Email;
                credenciales.Conectado = true;
                credenciales.FechaConexion = DateTime.UtcNow;
                credenciales.TokenExpiracion = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn ?? 15552000);
                credenciales.EsProduccion = true;

                await _context.SaveChangesAsync();

                _logger.LogInformation("MercadoPago conectado via OAuth para {Email}", userInfo?.Email);

                return Ok(new MercadoPagoCredencialesDto
                {
                    Id = credenciales.Id,
                    Conectado = true,
                    MercadoPagoEmail = credenciales.MercadoPagoEmail,
                    FechaConexion = credenciales.FechaConexion,
                    EsProduccion = credenciales.EsProduccion,
                    TokenValido = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando OAuth callback");
                return StatusCode(500, "Error al procesar autorización: " + ex.Message);
            }
        }

        #endregion

        #region Métodos Privados

        private async Task<MercadoPagoUserInfo?> ValidarTokenMercadoPago(string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.mercadopago.com/users/me");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Token de MercadoPago inválido: {StatusCode}", response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var userData = JsonSerializer.Deserialize<JsonElement>(content);

                return new MercadoPagoUserInfo
                {
                    UserId = userData.GetProperty("id").ToString(),
                    Email = userData.GetProperty("email").GetString() ?? ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando token de MercadoPago");
                return null;
            }
        }

        #endregion

        #region Clases Auxiliares

        private class MercadoPagoUserInfo
        {
            public string UserId { get; set; } = "";
            public string Email { get; set; } = "";
        }

        private class MercadoPagoTokenResponse
        {
            public string AccessToken { get; set; } = "";
            public string? RefreshToken { get; set; }
            public string? PublicKey { get; set; }
            public int? UserId { get; set; }
            public int? ExpiresIn { get; set; }
        }

        #endregion
    }
}
