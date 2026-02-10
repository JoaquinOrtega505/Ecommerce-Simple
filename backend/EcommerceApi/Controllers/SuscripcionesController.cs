using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using EcommerceApi.Models;
using EcommerceApi.Services;
using System.Security.Claims;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuscripcionesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly MercadoPagoSuscripcionesService _mpService;
    private readonly ILogger<SuscripcionesController> _logger;

    public SuscripcionesController(
        AppDbContext context,
        MercadoPagoSuscripcionesService mpService,
        ILogger<SuscripcionesController> logger)
    {
        _context = context;
        _mpService = mpService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la Public Key de MercadoPago para el frontend
    /// </summary>
    [HttpGet("mercadopago/public-key")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetPublicKey()
    {
        var publicKey = await _mpService.GetPublicKeyAsync();

        if (string.IsNullOrEmpty(publicKey))
        {
            return BadRequest(new { message = "MercadoPago no está configurado" });
        }

        return Ok(new { publicKey });
    }

    /// <summary>
    /// Crea una nueva suscripción para la tienda del emprendedor
    /// </summary>
    [HttpPost("crear")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CrearSuscripcion([FromBody] CrearSuscripcionDto dto)
    {
        var tiendaIdClaim = User.FindFirstValue("TiendaId");
        if (string.IsNullOrEmpty(tiendaIdClaim) || !int.TryParse(tiendaIdClaim, out int tiendaId))
        {
            return BadRequest(new { message = "No se pudo obtener la tienda del usuario" });
        }

        var tienda = await _context.Tiendas
            .Include(t => t.PlanSuscripcion)
            .FirstOrDefaultAsync(t => t.Id == tiendaId);

        if (tienda == null)
        {
            return NotFound(new { message = "Tienda no encontrada" });
        }

        var plan = await _context.PlanesSuscripcion.FindAsync(dto.PlanId);
        if (plan == null || !plan.Activo)
        {
            return NotFound(new { message = "Plan no encontrado o inactivo" });
        }

        // Obtener email del usuario
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var usuario = await _context.Usuarios.FindAsync(int.Parse(userId!));
        var payerEmail = dto.PayerEmail ?? usuario?.Email ?? "";

        if (string.IsNullOrEmpty(payerEmail))
        {
            return BadRequest(new { message = "Email del pagador es requerido" });
        }

        if (string.IsNullOrEmpty(dto.CardTokenId))
        {
            return BadRequest(new { message = "Token de tarjeta es requerido" });
        }

        // Crear suscripción en MercadoPago
        var request = new CrearSuscripcionRequest
        {
            TiendaId = tiendaId,
            PlanId = dto.PlanId,
            PayerEmail = payerEmail,
            CardTokenId = dto.CardTokenId
        };

        var result = await _mpService.CrearSuscripcionAsync(request);

        if (!result.Success)
        {
            _logger.LogError("Error al crear suscripción: {Error}", result.Error);
            return BadRequest(new { message = result.Error });
        }

        // Obtener configuración para días de prueba
        var config = await _context.ConfiguracionSuscripciones.FirstOrDefaultAsync(c => c.Activo);
        var diasTrial = config?.DiasPrueba ?? 7;

        // Actualizar la tienda con la nueva suscripción
        tienda.PlanSuscripcionId = dto.PlanId;
        tienda.MaxProductos = plan.MaxProductos;
        tienda.MercadoPagoSuscripcionId = result.PreapprovalId;
        tienda.FechaSuscripcion = DateTime.UtcNow;
        tienda.FechaInicioTrial = DateTime.UtcNow;
        tienda.FechaFinTrial = DateTime.UtcNow.AddDays(diasTrial);
        tienda.FechaVencimientoSuscripcion = DateTime.UtcNow.AddDays(diasTrial).AddMonths(1);
        tienda.EstadoSuscripcion = diasTrial > 0 ? "trial" : "active";
        tienda.EstadoTienda = "Activa";
        tienda.FechaModificacion = DateTime.UtcNow;

        // Crear registro en historial
        var historial = new HistorialSuscripcion
        {
            TiendaId = tiendaId,
            PlanSuscripcionId = dto.PlanId,
            FechaInicio = DateTime.UtcNow,
            Estado = "Activa",
            MetodoPago = "MercadoPago",
            TransaccionId = result.PreapprovalId,
            MontoTotal = plan.PrecioMensual,
            Notas = $"Suscripción creada con {diasTrial} días de prueba"
        };
        _context.HistorialSuscripciones.Add(historial);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Suscripción creada para tienda {TiendaId} - Plan: {PlanNombre} - MP ID: {PreapprovalId}",
            tiendaId, plan.Nombre, result.PreapprovalId);

        return Ok(new
        {
            message = "Suscripción creada exitosamente",
            preapprovalId = result.PreapprovalId,
            status = result.Status,
            initPoint = result.InitPoint,
            plan = plan.Nombre,
            diasTrial,
            fechaFinTrial = tienda.FechaFinTrial
        });
    }

    /// <summary>
    /// Obtiene el estado de la suscripción actual de la tienda
    /// </summary>
    [HttpGet("mi-suscripcion")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetMiSuscripcion()
    {
        var tiendaIdClaim = User.FindFirstValue("TiendaId");
        if (string.IsNullOrEmpty(tiendaIdClaim) || !int.TryParse(tiendaIdClaim, out int tiendaId))
        {
            return BadRequest(new { message = "No se pudo obtener la tienda del usuario" });
        }

        var tienda = await _context.Tiendas
            .Include(t => t.PlanSuscripcion)
            .FirstOrDefaultAsync(t => t.Id == tiendaId);

        if (tienda == null)
        {
            return NotFound(new { message = "Tienda no encontrada" });
        }

        // Verificar estado en MercadoPago si hay suscripción
        string? mpStatus = null;
        if (!string.IsNullOrEmpty(tienda.MercadoPagoSuscripcionId))
        {
            var mpResult = await _mpService.ObtenerEstadoSuscripcionAsync(tienda.MercadoPagoSuscripcionId);
            if (mpResult.Success)
            {
                mpStatus = mpResult.Status;
            }
        }

        return Ok(new
        {
            tiendaId = tienda.Id,
            plan = tienda.PlanSuscripcion != null ? new
            {
                tienda.PlanSuscripcion.Id,
                tienda.PlanSuscripcion.Nombre,
                tienda.PlanSuscripcion.Descripcion,
                tienda.PlanSuscripcion.MaxProductos,
                tienda.PlanSuscripcion.PrecioMensual
            } : null,
            estadoSuscripcion = tienda.EstadoSuscripcion,
            estadoMercadoPago = mpStatus,
            fechaSuscripcion = tienda.FechaSuscripcion,
            fechaInicioTrial = tienda.FechaInicioTrial,
            fechaFinTrial = tienda.FechaFinTrial,
            fechaVencimiento = tienda.FechaVencimientoSuscripcion,
            enTrial = tienda.EstadoSuscripcion == "trial" && tienda.FechaFinTrial > DateTime.UtcNow,
            diasRestantesTrial = tienda.FechaFinTrial.HasValue
                ? Math.Max(0, (tienda.FechaFinTrial.Value - DateTime.UtcNow).Days)
                : 0
        });
    }

    /// <summary>
    /// Callback para MercadoPago después de la autorización
    /// </summary>
    [HttpGet("callback")]
    public async Task<ActionResult> Callback(
        [FromQuery] string? preapproval_id,
        [FromQuery] string? status)
    {
        _logger.LogInformation("Callback de suscripción recibido: PreapprovalId={PreapprovalId}, Status={Status}",
            preapproval_id, status);

        if (string.IsNullOrEmpty(preapproval_id))
        {
            return BadRequest(new { message = "preapproval_id es requerido" });
        }

        // Buscar la tienda con esta suscripción
        var tienda = await _context.Tiendas
            .FirstOrDefaultAsync(t => t.MercadoPagoSuscripcionId == preapproval_id);

        if (tienda != null && !string.IsNullOrEmpty(status))
        {
            // Actualizar estado según respuesta de MP
            switch (status.ToLower())
            {
                case "authorized":
                    tienda.EstadoSuscripcion = "active";
                    tienda.EstadoTienda = "Activa";
                    break;
                case "pending":
                    tienda.EstadoSuscripcion = "pending";
                    break;
                case "cancelled":
                    tienda.EstadoSuscripcion = "cancelled";
                    tienda.EstadoTienda = "Suspendida";
                    break;
            }
            tienda.FechaModificacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // Redirigir al frontend
        var frontendUrl = _context.Database.GetConnectionString() != null
            ? "http://localhost:4200"
            : "http://localhost:4200";

        return Redirect($"{frontendUrl}/emprendedor/suscripcion/resultado?status={status}&preapproval_id={preapproval_id}");
    }
}

public class CrearSuscripcionDto
{
    public int PlanId { get; set; }
    public string CardTokenId { get; set; } = string.Empty;
    public string? PayerEmail { get; set; }
}
