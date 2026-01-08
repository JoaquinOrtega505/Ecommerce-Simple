using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using EcommerceApi.DTOs;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<WebhookController> _logger;
    private readonly IConfiguration _configuration;

    public WebhookController(AppDbContext context, ILogger<WebhookController> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Webhook genérico para recibir notificaciones de entrega de servicios externos
    /// </summary>
    /// <param name="request">Datos de la notificación de entrega</param>
    /// <returns>Confirmación del procesamiento</returns>
    [HttpPost("entrega")]
    public async Task<ActionResult> NotificarEntrega([FromBody] WebhookEntregaDto request)
    {
        try
        {
            // Log de la petición recibida
            _logger.LogInformation("Webhook recibido: Pedido {PedidoId}, Estado: {Estado}, Servicio: {Servicio}",
                request.PedidoId, request.Estado, request.ServicioEnvio);

            // Validar que el webhook incluya un secret key (opcional pero recomendado)
            var webhookSecret = _configuration["Webhook:SecretKey"];
            if (!string.IsNullOrEmpty(webhookSecret) && request.SecretKey != webhookSecret)
            {
                _logger.LogWarning("Intento de acceso con secret key inválido para pedido {PedidoId}", request.PedidoId);
                return Unauthorized(new { message = "Secret key inválido" });
            }

            // Buscar el pedido
            var pedido = await _context.Pedidos.FindAsync(request.PedidoId);
            if (pedido == null)
            {
                _logger.LogWarning("Pedido {PedidoId} no encontrado", request.PedidoId);
                return NotFound(new { message = "Pedido no encontrado" });
            }

            // Actualizar estado según la notificación
            switch (request.Estado.ToLower())
            {
                case "entregado":
                case "delivered":
                    if (pedido.Estado != "Enviado")
                    {
                        _logger.LogWarning("Intento de marcar como entregado un pedido que no está en estado Enviado. Pedido {PedidoId}, Estado actual: {Estado}",
                            request.PedidoId, pedido.Estado);
                        return BadRequest(new { message = "El pedido debe estar en estado 'Enviado' para marcarse como entregado" });
                    }

                    pedido.Estado = "Entregado";
                    pedido.FechaEntrega = request.FechaEntrega ?? DateTime.UtcNow;

                    _logger.LogInformation("Pedido {PedidoId} marcado como Entregado", request.PedidoId);
                    break;

                case "en_transito":
                case "in_transit":
                    // Información adicional, no cambia el estado principal
                    _logger.LogInformation("Pedido {PedidoId} en tránsito según {Servicio}",
                        request.PedidoId, request.ServicioEnvio);
                    break;

                case "fallido":
                case "failed":
                    _logger.LogWarning("Intento de entrega fallido para pedido {PedidoId}. Motivo: {Motivo}",
                        request.PedidoId, request.Observaciones);
                    // Podrías agregar un campo en el modelo para registrar intentos fallidos
                    break;

                default:
                    _logger.LogWarning("Estado desconocido recibido: {Estado} para pedido {PedidoId}",
                        request.Estado, request.PedidoId);
                    return BadRequest(new { message = $"Estado '{request.Estado}' no reconocido" });
            }

            // Guardar información del tracking si se proporciona
            if (!string.IsNullOrEmpty(request.NumeroSeguimiento))
            {
                // Aquí podrías guardar el número de seguimiento en un campo del pedido
                // Por ahora solo lo logueamos
                _logger.LogInformation("Número de seguimiento para pedido {PedidoId}: {NumeroSeguimiento}",
                    request.PedidoId, request.NumeroSeguimiento);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Notificación procesada correctamente",
                pedidoId = request.PedidoId,
                nuevoEstado = pedido.Estado,
                fechaProcesamiento = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando webhook de entrega para pedido {PedidoId}", request.PedidoId);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Endpoint de prueba para verificar que el webhook está funcionando
    /// </summary>
    [HttpGet("test")]
    public ActionResult TestWebhook()
    {
        return Ok(new
        {
            status = "Webhook funcionando correctamente",
            timestamp = DateTime.UtcNow,
            endpoints = new
            {
                entrega = "/api/webhook/entrega"
            }
        });
    }
}
