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
    private readonly BrevoEmailService _emailService;

    private const string MP_API_URL = "https://api.mercadopago.com";

    public MercadoPagoSuscripcionesService(
        AppDbContext context,
        EncryptionService encryptionService,
        HttpClient httpClient,
        ILogger<MercadoPagoSuscripcionesService> logger,
        IConfiguration configuration,
        BrevoEmailService emailService)
    {
        _context = context;
        _encryptionService = encryptionService;
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _emailService = emailService;
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

    /// <summary>
    /// Crea una suscripción (preapproval) para un emprendedor
    /// </summary>
    public async Task<MpSuscripcionResult> CrearSuscripcionAsync(CrearSuscripcionRequest request)
    {
        var accessToken = await GetAccessTokenAsync();

        if (string.IsNullOrEmpty(accessToken))
        {
            return new MpSuscripcionResult
            {
                Success = false,
                Error = "MercadoPago no está conectado"
            };
        }

        try
        {
            var plan = await _context.PlanesSuscripcion.FindAsync(request.PlanId);
            if (plan == null)
            {
                return new MpSuscripcionResult
                {
                    Success = false,
                    Error = "Plan no encontrado"
                };
            }

            // Si el plan no tiene ID de MercadoPago, sincronizarlo primero
            if (string.IsNullOrEmpty(plan.MercadoPagoPlanId))
            {
                var syncResult = await CrearPlanAsync(plan);
                if (!syncResult.Success)
                {
                    return new MpSuscripcionResult
                    {
                        Success = false,
                        Error = "No se pudo sincronizar el plan con MercadoPago"
                    };
                }
                plan.MercadoPagoPlanId = syncResult.MercadoPagoPlanId;
                plan.MercadoPagoSyncDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            var backUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:4200";

            var suscripcionData = new
            {
                preapproval_plan_id = plan.MercadoPagoPlanId,
                payer_email = request.PayerEmail,
                card_token_id = request.CardTokenId,
                back_url = $"{backUrl}/emprendedor/suscripcion/callback",
                external_reference = $"tienda_{request.TiendaId}"
            };

            var json = JsonSerializer.Serialize(suscripcionData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{MP_API_URL}/preapproval")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error creando suscripción en MP: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);
                return new MpSuscripcionResult
                {
                    Success = false,
                    Error = $"Error de MercadoPago: {responseContent}"
                };
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var preapprovalId = result.GetProperty("id").GetString();
            var status = result.GetProperty("status").GetString();
            var initPoint = result.TryGetProperty("init_point", out var initPointProp)
                ? initPointProp.GetString()
                : null;

            _logger.LogInformation("Suscripción creada en MercadoPago: {PreapprovalId} - Status: {Status}",
                preapprovalId, status);

            return new MpSuscripcionResult
            {
                Success = true,
                PreapprovalId = preapprovalId,
                Status = status,
                InitPoint = initPoint
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear suscripción en MercadoPago");
            return new MpSuscripcionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Obtiene el estado de una suscripción en MercadoPago
    /// </summary>
    public async Task<MpSuscripcionResult> ObtenerEstadoSuscripcionAsync(string preapprovalId)
    {
        var accessToken = await GetAccessTokenAsync();

        if (string.IsNullOrEmpty(accessToken))
        {
            return new MpSuscripcionResult
            {
                Success = false,
                Error = "MercadoPago no está conectado"
            };
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{MP_API_URL}/preapproval/{preapprovalId}");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new MpSuscripcionResult
                {
                    Success = false,
                    Error = $"Error de MercadoPago: {response.StatusCode}"
                };
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var status = result.GetProperty("status").GetString();

            return new MpSuscripcionResult
            {
                Success = true,
                PreapprovalId = preapprovalId,
                Status = status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estado de suscripción");
            return new MpSuscripcionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Obtiene la Public Key para el frontend
    /// </summary>
    public async Task<string?> GetPublicKeyAsync()
    {
        var credenciales = await _context.MercadoPagoCredenciales.FirstOrDefaultAsync();

        if (credenciales == null || !credenciales.Conectado || string.IsNullOrEmpty(credenciales.PublicKey))
        {
            return null;
        }

        return _encryptionService.Decrypt(credenciales.PublicKey);
    }

    /// <summary>
    /// Procesa una notificación de webhook de MercadoPago para suscripciones
    /// </summary>
    public async Task<WebhookResult> ProcesarWebhookSuscripcionAsync(string topic, string resourceId)
    {
        try
        {
            _logger.LogInformation("Procesando webhook: Topic={Topic}, ResourceId={ResourceId}", topic, resourceId);

            // Solo procesamos notificaciones de preapproval (suscripciones)
            if (topic != "preapproval" && topic != "subscription_preapproval")
            {
                _logger.LogInformation("Topic no relacionado con suscripciones: {Topic}", topic);
                return new WebhookResult { Success = true, Message = "Topic ignorado" };
            }

            // Obtener estado actual de la suscripción en MercadoPago
            var estadoResult = await ObtenerEstadoSuscripcionAsync(resourceId);
            if (!estadoResult.Success)
            {
                _logger.LogError("No se pudo obtener estado de suscripción {Id}: {Error}",
                    resourceId, estadoResult.Error);
                return new WebhookResult { Success = false, Error = estadoResult.Error };
            }

            // Buscar la tienda asociada a esta suscripción
            var tienda = await _context.Tiendas
                .FirstOrDefaultAsync(t => t.MercadoPagoSuscripcionId == resourceId);

            if (tienda == null)
            {
                _logger.LogWarning("No se encontró tienda para suscripción {Id}", resourceId);
                return new WebhookResult { Success = true, Message = "Tienda no encontrada" };
            }

            var config = await _context.ConfiguracionSuscripciones.FirstOrDefaultAsync(c => c.Activo);
            var maxReintentos = config?.MaxReintentosPago ?? 3;

            // Actualizar estado según respuesta de MP
            var (cambioRealizado, tipoNotificacion) = ActualizarEstadoTienda(
                tienda, estadoResult.Status!, maxReintentos);

            if (cambioRealizado)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Tienda {TiendaId} actualizada a estado {Estado}",
                    tienda.Id, tienda.EstadoSuscripcion);

                // Enviar notificación por email
                if (!string.IsNullOrEmpty(tipoNotificacion))
                {
                    var intentosRestantes = maxReintentos - tienda.ReintentosPago;
                    await EnviarNotificacionSuscripcionAsync(tienda.Id, tipoNotificacion, intentosRestantes);
                }
            }

            return new WebhookResult
            {
                Success = true,
                Message = $"Estado actualizado: {tienda.EstadoSuscripcion}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando webhook de suscripción");
            return new WebhookResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Actualiza el estado de una tienda basado en el estado de MercadoPago
    /// </summary>
    private (bool cambio, string tipoNotificacion) ActualizarEstadoTienda(
        Tienda tienda, string estadoMP, int maxReintentos)
    {
        var estadoAnterior = tienda.EstadoSuscripcion;
        var cambio = false;
        var tipoNotificacion = "";

        switch (estadoMP.ToLower())
        {
            case "authorized":
            case "active":
                // Pago autorizado exitosamente
                var eraInactiva = tienda.EstadoTienda == "Suspendida";
                tienda.EstadoSuscripcion = "active";
                tienda.EstadoTienda = "Activa";
                tienda.ReintentosPago = 0;
                tienda.FechaVencimientoSuscripcion = DateTime.UtcNow.AddMonths(1);
                cambio = true;
                tipoNotificacion = eraInactiva ? "reactivada" : "pago_exitoso";
                break;

            case "pending":
                // Pendiente de autorización
                if (tienda.EstadoSuscripcion != "pending")
                {
                    tienda.EstadoSuscripcion = "pending";
                    cambio = true;
                }
                break;

            case "paused":
                // Suscripción pausada por el usuario o por fallo de pago
                tienda.EstadoSuscripcion = "paused";
                tienda.ReintentosPago++;

                if (tienda.ReintentosPago >= maxReintentos)
                {
                    tienda.EstadoTienda = "Suspendida";
                    tipoNotificacion = "suspendida";
                    _logger.LogWarning("Tienda {TiendaId} suspendida por máximo de reintentos alcanzado",
                        tienda.Id);
                }
                else
                {
                    tipoNotificacion = "pago_fallido";
                }
                cambio = true;
                break;

            case "cancelled":
                // Suscripción cancelada
                tienda.EstadoSuscripcion = "cancelled";
                tienda.EstadoTienda = "Suspendida";
                tipoNotificacion = "suspendida";
                cambio = true;
                break;

            default:
                _logger.LogWarning("Estado de MP no reconocido: {Estado}", estadoMP);
                break;
        }

        if (cambio)
        {
            _logger.LogInformation("Tienda {TiendaId}: Estado cambió de {Anterior} a {Nuevo}",
                tienda.Id, estadoAnterior, tienda.EstadoSuscripcion);
        }

        return (cambio, tipoNotificacion);
    }

    /// <summary>
    /// Envía notificación por email según el tipo de evento
    /// </summary>
    public async Task EnviarNotificacionSuscripcionAsync(
        int tiendaId, string tipoNotificacion, int? intentosRestantes = null)
    {
        try
        {
            var tienda = await _context.Tiendas
                .Include(t => t.PlanSuscripcion)
                .Include(t => t.Usuarios)
                .FirstOrDefaultAsync(t => t.Id == tiendaId);

            if (tienda == null) return;

            var emprendedor = tienda.Usuarios.FirstOrDefault(u => u.Rol == "Emprendedor");
            if (emprendedor == null) return;

            var config = await _context.ConfiguracionSuscripciones.FirstOrDefaultAsync(c => c.Activo);
            var diasGracia = config?.DiasGraciaSuspension ?? 3;

            switch (tipoNotificacion)
            {
                case "pago_exitoso":
                    await _emailService.EnviarPagoSuscripcionExitosoAsync(
                        emprendedor.Email,
                        emprendedor.Nombre,
                        tienda.Nombre,
                        tienda.PlanSuscripcion?.Nombre ?? "Plan",
                        tienda.PlanSuscripcion?.PrecioMensual ?? 0,
                        tienda.FechaVencimientoSuscripcion ?? DateTime.UtcNow.AddMonths(1));
                    break;

                case "pago_fallido":
                    await _emailService.EnviarPagoSuscripcionFallidoAsync(
                        emprendedor.Email,
                        emprendedor.Nombre,
                        tienda.Nombre,
                        tienda.PlanSuscripcion?.Nombre ?? "Plan",
                        intentosRestantes ?? 0);
                    break;

                case "suspendida":
                    await _emailService.EnviarTiendaSuspendidaAsync(
                        emprendedor.Email,
                        emprendedor.Nombre,
                        tienda.Nombre,
                        "Máximo de reintentos de pago alcanzado o suscripción cancelada",
                        diasGracia);
                    break;

                case "reactivada":
                    await _emailService.EnviarTiendaReactivadaAsync(
                        emprendedor.Email,
                        emprendedor.Nombre,
                        tienda.Nombre);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando notificación de suscripción para tienda {TiendaId}", tiendaId);
        }
    }

    /// <summary>
    /// Procesa notificación de pago de suscripción
    /// </summary>
    public async Task<WebhookResult> ProcesarWebhookPagoAsync(string paymentId)
    {
        var accessToken = await GetAccessTokenAsync();

        if (string.IsNullOrEmpty(accessToken))
        {
            return new WebhookResult { Success = false, Error = "MercadoPago no está conectado" };
        }

        try
        {
            // Obtener información del pago
            var request = new HttpRequestMessage(HttpMethod.Get, $"{MP_API_URL}/v1/payments/{paymentId}");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error obteniendo pago {PaymentId}: {Response}", paymentId, responseContent);
                return new WebhookResult { Success = false, Error = "No se pudo obtener información del pago" };
            }

            var paymentData = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var status = paymentData.GetProperty("status").GetString();
            var externalReference = paymentData.TryGetProperty("external_reference", out var extRef)
                ? extRef.GetString()
                : null;

            _logger.LogInformation("Pago {PaymentId}: Status={Status}, ExternalRef={ExtRef}",
                paymentId, status, externalReference);

            // Si tiene external_reference con formato tienda_X, buscar la tienda
            if (!string.IsNullOrEmpty(externalReference) && externalReference.StartsWith("tienda_"))
            {
                var tiendaIdStr = externalReference.Replace("tienda_", "");
                if (int.TryParse(tiendaIdStr, out var tiendaId))
                {
                    var tienda = await _context.Tiendas.FindAsync(tiendaId);
                    if (tienda != null)
                    {
                        var config = await _context.ConfiguracionSuscripciones.FirstOrDefaultAsync(c => c.Activo);
                        var maxReintentos = config?.MaxReintentosPago ?? 3;

                        var tipoNotificacion = ProcesarEstadoPagoTienda(tienda, status!, maxReintentos);
                        await _context.SaveChangesAsync();

                        // Enviar notificación por email
                        if (!string.IsNullOrEmpty(tipoNotificacion))
                        {
                            var intentosRestantes = maxReintentos - tienda.ReintentosPago;
                            await EnviarNotificacionSuscripcionAsync(tienda.Id, tipoNotificacion, intentosRestantes);
                        }
                    }
                }
            }

            return new WebhookResult { Success = true, Message = $"Pago procesado: {status}" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando webhook de pago {PaymentId}", paymentId);
            return new WebhookResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Procesa el estado de pago para una tienda
    /// </summary>
    private string ProcesarEstadoPagoTienda(Tienda tienda, string statusPago, int maxReintentos)
    {
        var tipoNotificacion = "";

        switch (statusPago.ToLower())
        {
            case "approved":
                var eraInactiva = tienda.EstadoTienda == "Suspendida";
                tienda.EstadoSuscripcion = "active";
                tienda.EstadoTienda = "Activa";
                tienda.ReintentosPago = 0;
                tienda.FechaVencimientoSuscripcion = DateTime.UtcNow.AddMonths(1);
                tipoNotificacion = eraInactiva ? "reactivada" : "pago_exitoso";
                _logger.LogInformation("Tienda {TiendaId}: Pago aprobado, suscripción activa", tienda.Id);
                break;

            case "rejected":
            case "cancelled":
                tienda.ReintentosPago++;
                _logger.LogWarning("Tienda {TiendaId}: Pago rechazado, reintento {Actual}/{Max}",
                    tienda.Id, tienda.ReintentosPago, maxReintentos);

                if (tienda.ReintentosPago >= maxReintentos)
                {
                    tienda.EstadoSuscripcion = "paused";
                    tienda.EstadoTienda = "Suspendida";
                    tipoNotificacion = "suspendida";
                    _logger.LogWarning("Tienda {TiendaId}: Suspendida por máximo de reintentos", tienda.Id);
                }
                else
                {
                    tipoNotificacion = "pago_fallido";
                }
                break;

            case "pending":
            case "in_process":
                _logger.LogInformation("Tienda {TiendaId}: Pago pendiente de procesamiento", tienda.Id);
                break;
        }

        return tipoNotificacion;
    }

    /// <summary>
    /// Cancela una suscripción en MercadoPago
    /// </summary>
    public async Task<MpSuscripcionResult> CancelarSuscripcionAsync(string preapprovalId)
    {
        var accessToken = await GetAccessTokenAsync();

        if (string.IsNullOrEmpty(accessToken))
        {
            return new MpSuscripcionResult
            {
                Success = false,
                Error = "MercadoPago no está conectado"
            };
        }

        try
        {
            var cancelData = new { status = "cancelled" };
            var json = JsonSerializer.Serialize(cancelData);

            var request = new HttpRequestMessage(HttpMethod.Put, $"{MP_API_URL}/preapproval/{preapprovalId}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error cancelando suscripción {Id}: {Response}", preapprovalId, responseContent);
                return new MpSuscripcionResult
                {
                    Success = false,
                    Error = $"Error de MercadoPago: {response.StatusCode}"
                };
            }

            _logger.LogInformation("Suscripción {Id} cancelada exitosamente", preapprovalId);
            return new MpSuscripcionResult { Success = true, Status = "cancelled" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cancelar suscripción {Id}", preapprovalId);
            return new MpSuscripcionResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Verifica y actualiza tiendas con trial expirado
    /// </summary>
    public async Task<int> VerificarTrialsExpiradosAsync()
    {
        var ahora = DateTime.UtcNow;
        var tiendasTrialExpirado = await _context.Tiendas
            .Where(t => t.EstadoSuscripcion == "trial" &&
                        t.FechaFinTrial.HasValue &&
                        t.FechaFinTrial.Value <= ahora)
            .ToListAsync();

        foreach (var tienda in tiendasTrialExpirado)
        {
            // Si tiene suscripción de MP, verificar estado
            if (!string.IsNullOrEmpty(tienda.MercadoPagoSuscripcionId))
            {
                var estado = await ObtenerEstadoSuscripcionAsync(tienda.MercadoPagoSuscripcionId);
                if (estado.Success && estado.Status == "authorized")
                {
                    tienda.EstadoSuscripcion = "active";
                    tienda.EstadoTienda = "Activa";
                    continue;
                }
            }

            // Trial expirado sin pago activo
            tienda.EstadoSuscripcion = "expired";
            tienda.EstadoTienda = "Suspendida";
            _logger.LogInformation("Tienda {TiendaId}: Trial expirado, tienda suspendida", tienda.Id);
        }

        await _context.SaveChangesAsync();
        return tiendasTrialExpirado.Count;
    }
}

public class MpPlanResult
{
    public bool Success { get; set; }
    public string? MercadoPagoPlanId { get; set; }
    public string? Error { get; set; }
}

public class MpSuscripcionResult
{
    public bool Success { get; set; }
    public string? PreapprovalId { get; set; }
    public string? Status { get; set; }
    public string? InitPoint { get; set; }
    public string? Error { get; set; }
}

public class CrearSuscripcionRequest
{
    public int TiendaId { get; set; }
    public int PlanId { get; set; }
    public string PayerEmail { get; set; } = string.Empty;
    public string CardTokenId { get; set; } = string.Empty;
}

public class WebhookResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
