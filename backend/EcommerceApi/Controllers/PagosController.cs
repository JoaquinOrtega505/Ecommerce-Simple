using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EcommerceApi.Data;
using EcommerceApi.Services;
using MercadoPago.Client.Payment;
using MercadoPago.Resource.Payment;

namespace EcommerceApi.Controllers;

public class ProcessPaymentRequest
{
    public int PedidoId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
    public string? IssuerId { get; set; }
    public int Installments { get; set; }
    public PayerInfo Payer { get; set; } = new();
}

public class PayerInfo
{
    public string Email { get; set; } = string.Empty;
    public IdentificationInfo Identification { get; set; } = new();
}

public class IdentificationInfo
{
    public string Type { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
}

[ApiController]
[Route("api/[controller]")]
public class PagosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly MercadoPagoService _mercadoPagoService;
    private readonly ILogger<PagosController> _logger;
    private readonly IConfiguration _configuration;

    public PagosController(
        AppDbContext context,
        MercadoPagoService mercadoPagoService,
        ILogger<PagosController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _mercadoPagoService = mercadoPagoService;
        _logger = logger;
        _configuration = configuration;
    }

    private int GetUsuarioId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    /// <summary>
    /// Crea una preferencia de pago para un pedido
    /// </summary>
    [HttpPost("crear-preferencia/{pedidoId}")]
    [Authorize]
    public async Task<ActionResult> CrearPreferencia(int pedidoId)
    {
        try
        {
            var usuarioId = GetUsuarioId();

            // Obtener el pedido con items y productos
            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.PedidoItems)
                    .ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(p => p.Id == pedidoId && p.UsuarioId == usuarioId);

            if (pedido == null)
            {
                return NotFound(new { message = "Pedido no encontrado" });
            }

            if (pedido.Estado != "Pendiente")
            {
                return BadRequest(new { message = "El pedido no está en estado pendiente" });
            }

            // URLs de retorno - todas apuntan a /pago/return que redirige según el estado
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
            var urlReturn = $"{frontendUrl}/pago/return?pedidoId={pedidoId}";
            var urlSuccess = urlReturn;
            var urlFailure = urlReturn;
            var urlPending = urlReturn;

            // Crear preferencia en MercadoPago
            var preference = await _mercadoPagoService.CrearPreferenciaPagoAsync(
                pedido,
                pedido.Usuario.Email,
                urlSuccess,
                urlFailure,
                urlPending
            );

            _logger.LogInformation("Preferencia creada: {PreferenceId} para pedido {PedidoId}",
                preference.Id, pedidoId);

            return Ok(new
            {
                preferenceId = preference.Id,
                initPoint = preference.InitPoint,
                sandboxInitPoint = preference.SandboxInitPoint
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear preferencia de pago para pedido {PedidoId}", pedidoId);
            return StatusCode(500, new { message = "Error al crear la preferencia de pago" });
        }
    }

    /// <summary>
    /// Procesa un pago directo usando el token de tarjeta de MercadoPago
    /// </summary>
    [HttpPost("procesar-pago")]
    [Authorize]
    public async Task<ActionResult> ProcesarPago([FromBody] ProcessPaymentRequest request)
    {
        try
        {
            var usuarioId = GetUsuarioId();

            // Obtener el pedido con items
            var pedido = await _context.Pedidos
                .Include(p => p.PedidoItems)
                .FirstOrDefaultAsync(p => p.Id == request.PedidoId && p.UsuarioId == usuarioId);

            if (pedido == null)
            {
                return NotFound(new { message = "Pedido no encontrado" });
            }

            if (pedido.Estado != "Pendiente")
            {
                return BadRequest(new { message = "El pedido no está en estado pendiente" });
            }

            // Calcular el monto total
            var amount = pedido.PedidoItems.Sum(i => i.PrecioUnitario * i.Cantidad);

            // Crear la solicitud de pago simplificada
            var paymentRequest = new PaymentCreateRequest
            {
                TransactionAmount = amount,
                Token = request.Token,
                Description = $"Pedido #{request.PedidoId}",
                Installments = request.Installments,
                PaymentMethodId = request.PaymentMethodId,
                Payer = new PaymentPayerRequest
                {
                    Email = request.Payer.Email
                },
                ExternalReference = request.PedidoId.ToString()
            };

            // Procesar el pago
            var client = new PaymentClient();
            Payment payment = await client.CreateAsync(paymentRequest);

            _logger.LogInformation("Pago procesado: {PaymentId}, Estado: {Status}, Pedido: {PedidoId}",
                payment.Id, payment.Status, request.PedidoId);

            // Actualizar el pedido según el resultado
            switch (payment.Status)
            {
                case "approved":
                    pedido.Estado = "Pagado";
                    pedido.FechaPago = DateTime.UtcNow;
                    pedido.TransaccionId = payment.Id.ToString();
                    pedido.MetodoPago = payment.PaymentMethodId;
                    break;

                case "rejected":
                    return BadRequest(new
                    {
                        message = "El pago fue rechazado",
                        status = payment.Status,
                        statusDetail = payment.StatusDetail
                    });

                case "pending":
                case "in_process":
                    pedido.Estado = "Pendiente";
                    break;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                paymentId = payment.Id,
                status = payment.Status,
                statusDetail = payment.StatusDetail,
                pedidoId = pedido.Id,
                mensaje = "Pago procesado correctamente"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar pago para pedido {PedidoId}", request.PedidoId);
            return StatusCode(500, new { message = "Error al procesar el pago: " + ex.Message });
        }
    }

    /// <summary>
    /// Webhook para recibir notificaciones de MercadoPago
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<ActionResult> WebhookMercadoPago([FromQuery] string type, [FromQuery] long id)
    {
        try
        {
            _logger.LogInformation("Webhook recibido: Type={Type}, Id={Id}", type, id);

            // Solo procesamos notificaciones de pago
            if (type != "payment")
            {
                return Ok();
            }

            // MercadoPago a veces envía notificaciones con id=0, las ignoramos
            if (id <= 0)
            {
                _logger.LogInformation("Webhook con ID inválido ignorado: {Id}", id);
                return Ok();
            }

            // Obtener información del pago
            var payment = await _mercadoPagoService.ObtenerPagoAsync(id);

            if (payment == null)
            {
                _logger.LogWarning("No se pudo obtener información del pago {PaymentId}", id);
                return Ok();
            }

            // Obtener el pedido por ExternalReference
            if (payment.ExternalReference == null)
            {
                _logger.LogWarning("Pago {PaymentId} sin ExternalReference", id);
                return Ok();
            }

            var pedidoId = int.Parse(payment.ExternalReference);
            var pedido = await _context.Pedidos.FindAsync(pedidoId);

            if (pedido == null)
            {
                _logger.LogWarning("Pedido {PedidoId} no encontrado para pago {PaymentId}",
                    pedidoId, id);
                return Ok();
            }

            // Actualizar estado del pedido según el estado del pago
            switch (payment.Status)
            {
                case "approved":
                    pedido.Estado = "Pagado";
                    pedido.FechaPago = DateTime.UtcNow;
                    pedido.TransaccionId = payment.Id.ToString();
                    pedido.MetodoPago = payment.PaymentMethodId;
                    _logger.LogInformation("Pago aprobado para pedido {PedidoId}", pedidoId);
                    break;

                case "rejected":
                case "cancelled":
                    pedido.Estado = "Cancelado";
                    _logger.LogInformation("Pago rechazado/cancelado para pedido {PedidoId}", pedidoId);
                    break;

                case "pending":
                case "in_process":
                    // No cambiamos el estado, queda en Pendiente
                    _logger.LogInformation("Pago pendiente para pedido {PedidoId}", pedidoId);
                    break;
            }

            await _context.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar webhook de MercadoPago");
            return Ok(); // Siempre retornamos 200 para que MP no reintente
        }
    }

    /// <summary>
    /// Consulta el estado de un pago
    /// </summary>
    [HttpGet("estado/{pedidoId}")]
    [Authorize]
    public async Task<ActionResult> ConsultarEstadoPago(int pedidoId)
    {
        try
        {
            var usuarioId = GetUsuarioId();

            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(p => p.Id == pedidoId && p.UsuarioId == usuarioId);

            if (pedido == null)
            {
                return NotFound(new { message = "Pedido no encontrado" });
            }

            return Ok(new
            {
                pedidoId = pedido.Id,
                estado = pedido.Estado,
                fechaPago = pedido.FechaPago,
                metodoPago = pedido.MetodoPago,
                transaccionId = pedido.TransaccionId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar estado de pago para pedido {PedidoId}", pedidoId);
            return StatusCode(500, new { message = "Error al consultar el estado del pago" });
        }
    }

    /// <summary>
    /// Marca un pedido como pagado (para usar sin webhook en desarrollo)
    /// </summary>
    [HttpPost("confirmar-pago/{pedidoId}")]
    [Authorize]
    public async Task<ActionResult> ConfirmarPago(int pedidoId, [FromQuery] string? paymentId = null)
    {
        try
        {
            var usuarioId = GetUsuarioId();

            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(p => p.Id == pedidoId && p.UsuarioId == usuarioId);

            if (pedido == null)
            {
                return NotFound(new { message = "Pedido no encontrado" });
            }

            // Solo actualizar si está en estado Pendiente
            if (pedido.Estado == "Pendiente")
            {
                pedido.Estado = "Pagado";
                pedido.FechaPago = DateTime.UtcNow;
                pedido.MetodoPago = "MercadoPago";

                if (!string.IsNullOrEmpty(paymentId))
                {
                    pedido.TransaccionId = paymentId;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Pedido {PedidoId} marcado como pagado manualmente", pedidoId);
            }

            return Ok(new
            {
                pedidoId = pedido.Id,
                estado = pedido.Estado,
                mensaje = "Pago confirmado correctamente"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al confirmar pago para pedido {PedidoId}", pedidoId);
            return StatusCode(500, new { message = "Error al confirmar el pago" });
        }
    }
}
