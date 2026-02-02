using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EcommerceApi.Data;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PagosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<PagosController> _logger;

    public PagosController(
        AppDbContext context,
        ILogger<PagosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int GetUsuarioId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    /// <summary>
    /// Consulta el estado de un pedido/pago
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
    /// Marca un pedido como pagado (para desarrollo/testing)
    /// </summary>
    [HttpPost("confirmar-pago/{pedidoId}")]
    [Authorize]
    public async Task<ActionResult> ConfirmarPago(int pedidoId)
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

            if (pedido.Estado == "Pendiente")
            {
                pedido.Estado = "Pagado";
                pedido.FechaPago = DateTime.UtcNow;
                pedido.MetodoPago = "Manual";

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

    /// <summary>
    /// Confirma pago para pedido anónimo (para desarrollo/testing)
    /// </summary>
    [HttpPost("confirmar-pago-anonimo/{pedidoId}")]
    [AllowAnonymous]
    public async Task<ActionResult> ConfirmarPagoAnonimo(int pedidoId)
    {
        try
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);

            if (pedido == null)
            {
                return NotFound(new { message = "Pedido no encontrado" });
            }

            if (pedido.Estado == "Pendiente")
            {
                pedido.Estado = "Pagado";
                pedido.FechaPago = DateTime.UtcNow;
                pedido.MetodoPago = "Manual";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Pedido anónimo {PedidoId} marcado como pagado", pedidoId);
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
            _logger.LogError(ex, "Error al confirmar pago anónimo para pedido {PedidoId}", pedidoId);
            return StatusCode(500, new { message = "Error al confirmar el pago" });
        }
    }
}
