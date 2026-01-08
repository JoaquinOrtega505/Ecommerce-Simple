using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using EcommerceApi.Services;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShippingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly MockShippingService _shippingService;
    private readonly ILogger<ShippingController> _logger;

    public ShippingController(
        AppDbContext context,
        MockShippingService shippingService,
        ILogger<ShippingController> logger)
    {
        _context = context;
        _shippingService = shippingService;
        _logger = logger;
    }

    /// <summary>
    /// Crea un envío para un pedido pagado
    /// </summary>
    [HttpPost("crear/{pedidoId}")]
    [Authorize(Roles = "Admin,Deposito")]
    public async Task<ActionResult> CrearEnvio(int pedidoId)
    {
        try
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null)
            {
                return NotFound(new { message = "Pedido no encontrado" });
            }

            if (pedido.Estado != "Pagado")
            {
                return BadRequest(new { message = "El pedido debe estar en estado 'Pagado' para crear el envío" });
            }

            if (!string.IsNullOrEmpty(pedido.NumeroSeguimiento))
            {
                return BadRequest(new { message = "El pedido ya tiene un número de seguimiento asignado" });
            }

            // Crear envío simulado
            var response = await _shippingService.CrearEnvioSimuladoAsync(pedidoId, pedido.DireccionEnvio);

            // Actualizar pedido con información de envío
            pedido.NumeroSeguimiento = response.NumeroSeguimiento;
            pedido.ServicioEnvio = response.Servicio;
            pedido.FechaDespacho = DateTime.UtcNow;
            pedido.Estado = "Enviado";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Envío creado para pedido {PedidoId}. Tracking: {TrackingNumber}",
                pedidoId, response.NumeroSeguimiento);

            return Ok(new
            {
                message = "Envío creado exitosamente",
                pedidoId = pedido.Id,
                numeroSeguimiento = response.NumeroSeguimiento,
                servicio = response.Servicio,
                urlTracking = response.UrlTracking,
                etiquetaUrl = response.EtiquetaUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear envío para pedido {PedidoId}", pedidoId);
            return StatusCode(500, new { message = "Error al crear el envío" });
        }
    }

    /// <summary>
    /// Obtiene el tracking de un envío
    /// </summary>
    [HttpGet("tracking/{numeroSeguimiento}")]
    public async Task<ActionResult> ObtenerTracking(string numeroSeguimiento)
    {
        try
        {
            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(p => p.NumeroSeguimiento == numeroSeguimiento);

            if (pedido == null)
            {
                return NotFound(new { message = "Número de seguimiento no encontrado" });
            }

            var tracking = await _shippingService.ObtenerTrackingAsync(numeroSeguimiento);

            return Ok(new
            {
                numeroSeguimiento = tracking.NumeroSeguimiento,
                estadoActual = tracking.EstadoActual,
                pedidoId = pedido.Id,
                eventos = tracking.Eventos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tracking: {NumeroSeguimiento}", numeroSeguimiento);
            return StatusCode(500, new { message = "Error al obtener información de tracking" });
        }
    }

    /// <summary>
    /// Descarga la etiqueta de envío
    /// </summary>
    [HttpGet("etiqueta/{numeroSeguimiento}")]
    [Authorize(Roles = "Admin,Deposito")]
    public async Task<ActionResult> DescargarEtiqueta(string numeroSeguimiento)
    {
        try
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.NumeroSeguimiento == numeroSeguimiento);

            if (pedido == null)
            {
                return NotFound(new { message = "Número de seguimiento no encontrado" });
            }

            var etiquetaBytes = _shippingService.GenerarEtiquetaSimulada(
                numeroSeguimiento,
                pedido.Usuario.Nombre,
                pedido.DireccionEnvio
            );

            return File(etiquetaBytes, "application/pdf", $"Etiqueta_{numeroSeguimiento}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar etiqueta: {NumeroSeguimiento}", numeroSeguimiento);
            return StatusCode(500, new { message = "Error al generar la etiqueta" });
        }
    }

    /// <summary>
    /// Simula la entrega de un pedido (para testing)
    /// </summary>
    [HttpPost("simular-entrega/{numeroSeguimiento}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SimularEntrega(string numeroSeguimiento)
    {
        try
        {
            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(p => p.NumeroSeguimiento == numeroSeguimiento);

            if (pedido == null)
            {
                return NotFound(new { message = "Número de seguimiento no encontrado" });
            }

            if (pedido.Estado != "Enviado")
            {
                return BadRequest(new { message = "El pedido debe estar en estado 'Enviado'" });
            }

            // Simular webhook de entrega
            pedido.Estado = "Entregado";
            pedido.FechaEntrega = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Entrega simulada para pedido {PedidoId}, tracking: {TrackingNumber}",
                pedido.Id, numeroSeguimiento);

            return Ok(new
            {
                message = "Entrega simulada exitosamente",
                pedidoId = pedido.Id,
                numeroSeguimiento = numeroSeguimiento,
                fechaEntrega = pedido.FechaEntrega
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al simular entrega: {NumeroSeguimiento}", numeroSeguimiento);
            return StatusCode(500, new { message = "Error al simular la entrega" });
        }
    }
}
