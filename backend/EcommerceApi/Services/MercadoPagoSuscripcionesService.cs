using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Services;

public class MercadoPagoSuscripcionesService
{
    private readonly AppDbContext _context;
    private readonly EncryptionService _encryptionService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MercadoPagoSuscripcionesService> _logger;
    private readonly IConfiguration _configuration;

    private const string MP_API_URL = "https://api.mercadopago.com";

    public MercadoPagoSuscripcionesService(
        AppDbContext context,
        EncryptionService encryptionService,
        HttpClient httpClient,
        ILogger<MercadoPagoSuscripcionesService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _encryptionService = encryptionService;
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Obtiene el Access Token del SuperAdmin (desencriptado)
    /// </summary>
    private async Task<string?> GetAccessTokenAsync()
    {
        var credenciales = await _context.MercadoPagoCredenciales.FirstOrDefaultAsync();

        if (credenciales == null || !credenciales.Conectado || string.IsNullOrEmpty(credenciales.AccessToken))
        {
            return null;
        }

        return _encryptionService.Decrypt(credenciales.AccessToken);
    }

    /// <summary>
    /// Verifica si MercadoPago está conectado
    /// </summary>
    public async Task<bool> EstaConectadoAsync()
    {
        var token = await GetAccessTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    /// <summary>
    /// Crea un plan de suscripción en MercadoPago (preapproval_plan)
    /// </summary>
    public async Task<MpPlanResult> CrearPlanAsync(PlanSuscripcion plan)
    {
        var accessToken = await GetAccessTokenAsync();

        if (string.IsNullOrEmpty(accessToken))
        {
            return new MpPlanResult
            {
                Success = false,
                Error = "MercadoPago no está conectado"
            };
        }

        try
        {
            var config = await _context.ConfiguracionSuscripciones.FirstOrDefaultAsync(c => c.Activo);
            var diasTrial = config?.DiasPrueba ?? 7;

            var backUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:4200";

            var planData = new
            {
                reason = plan.Nombre,
                auto_recurring = new
                {
                    frequency = 1,
                    frequency_type = "months",
                    transaction_amount = plan.PrecioMensual,
                    currency_id = "ARS",
                    free_trial = diasTrial > 0 ? new
                    {
                        frequency = diasTrial,
                        frequency_type = "days"
                    } : null
                },
                back_url = $"{backUrl}/emprendedor/suscripcion/callback",
                status = plan.Activo ? "active" : "inactive"
            };

            var json = JsonSerializer.Serialize(planData, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var request = new HttpRequestMessage(HttpMethod.Post, $"{MP_API_URL}/preapproval_plan")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error creando plan en MP: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);
                return new MpPlanResult
                {
                    Success = false,
                    Error = $"Error de MercadoPago: {response.StatusCode}"
                };
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var mpPlanId = result.GetProperty("id").GetString();

            _logger.LogInformation("Plan creado en MercadoPago: {PlanId}", mpPlanId);

            return new MpPlanResult
            {
                Success = true,
                MercadoPagoPlanId = mpPlanId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear plan en MercadoPago");
            return new MpPlanResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Actualiza un plan de suscripción en MercadoPago
    /// </summary>
    public async Task<MpPlanResult> ActualizarPlanAsync(PlanSuscripcion plan)
    {
        if (string.IsNullOrEmpty(plan.MercadoPagoPlanId))
        {
            // Si no tiene ID de MP, crear uno nuevo
            return await CrearPlanAsync(plan);
        }

        var accessToken = await GetAccessTokenAsync();

        if (string.IsNullOrEmpty(accessToken))
        {
            return new MpPlanResult
            {
                Success = false,
                Error = "MercadoPago no está conectado"
            };
        }

        try
        {
            var planData = new
            {
                reason = plan.Nombre,
                status = plan.Activo ? "active" : "inactive"
            };

            var json = JsonSerializer.Serialize(planData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var request = new HttpRequestMessage(HttpMethod.Put,
                $"{MP_API_URL}/preapproval_plan/{plan.MercadoPagoPlanId}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error actualizando plan en MP: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                // Si el plan no existe en MP, crear uno nuevo
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return await CrearPlanAsync(plan);
                }

                return new MpPlanResult
                {
                    Success = false,
                    Error = $"Error de MercadoPago: {response.StatusCode}"
                };
            }

            _logger.LogInformation("Plan actualizado en MercadoPago: {PlanId}", plan.MercadoPagoPlanId);

            return new MpPlanResult
            {
                Success = true,
                MercadoPagoPlanId = plan.MercadoPagoPlanId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar plan en MercadoPago");
            return new MpPlanResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Desactiva un plan en MercadoPago (no se puede eliminar)
    /// </summary>
    public async Task<MpPlanResult> DesactivarPlanAsync(string mercadoPagoPlanId)
    {
        var accessToken = await GetAccessTokenAsync();

        if (string.IsNullOrEmpty(accessToken))
        {
            return new MpPlanResult
            {
                Success = false,
                Error = "MercadoPago no está conectado"
            };
        }

        try
        {
            var planData = new { status = "inactive" };

            var json = JsonSerializer.Serialize(planData);

            var request = new HttpRequestMessage(HttpMethod.Put,
                $"{MP_API_URL}/preapproval_plan/{mercadoPagoPlanId}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error desactivando plan en MP: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);
                return new MpPlanResult
                {
                    Success = false,
                    Error = $"Error de MercadoPago: {response.StatusCode}"
                };
            }

            _logger.LogInformation("Plan desactivado en MercadoPago: {PlanId}", mercadoPagoPlanId);

            return new MpPlanResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar plan en MercadoPago");
            return new MpPlanResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Sincroniza todos los planes activos con MercadoPago
    /// </summary>
    public async Task<int> SincronizarTodosPlanesAsync()
    {
        var planes = await _context.PlanesSuscripcion.ToListAsync();
        var sincronizados = 0;

        foreach (var plan in planes)
        {
            MpPlanResult result;

            if (string.IsNullOrEmpty(plan.MercadoPagoPlanId))
            {
                result = await CrearPlanAsync(plan);
            }
            else
            {
                result = await ActualizarPlanAsync(plan);
            }

            if (result.Success)
            {
                plan.MercadoPagoPlanId = result.MercadoPagoPlanId;
                plan.MercadoPagoSyncDate = DateTime.UtcNow;
                sincronizados++;
            }
        }

        await _context.SaveChangesAsync();
        return sincronizados;
    }
}

public class MpPlanResult
{
    public bool Success { get; set; }
    public string? MercadoPagoPlanId { get; set; }
    public string? Error { get; set; }
}
