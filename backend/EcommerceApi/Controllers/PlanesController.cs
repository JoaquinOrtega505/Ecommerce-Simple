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
public class PlanesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<PlanesController> _logger;
    private readonly MercadoPagoService _mercadoPagoService;
    private readonly IConfiguration _configuration;

    public PlanesController(
        AppDbContext context,
        ILogger<PlanesController> logger,
        MercadoPagoService mercadoPagoService,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _mercadoPagoService = mercadoPagoService;
        _configuration = configuration;
    }

    // GET: api/planes
    [HttpGet]
    public async Task<ActionResult<List<PlanSuscripcion>>> GetPlanes()
    {
        var planes = await _context.PlanesSuscripcion
            .Where(p => p.Activo)
            .OrderBy(p => p.MaxProductos)
            .ToListAsync();

        return Ok(planes);
    }

    // GET: api/planes/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<PlanSuscripcion>> GetPlan(int id)
    {
        var plan = await _context.PlanesSuscripcion.FindAsync(id);

        if (plan == null)
        {
            return NotFound(new { message = "Plan no encontrado" });
        }

        return Ok(plan);
    }

    // POST: api/planes/suscribirse
    [HttpPost("suscribirse")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SuscribirseAPlan([FromBody] SuscripcionDto dto)
    {
        var tienda = await _context.Tiendas.FindAsync(dto.TiendaId);
        if (tienda == null)
        {
            return NotFound(new { message = "Tienda no encontrada" });
        }

        var plan = await _context.PlanesSuscripcion.FindAsync(dto.PlanId);
        if (plan == null || !plan.Activo)
        {
            return NotFound(new { message = "Plan no encontrado o inactivo" });
        }

        // Verificar que la tienda no supere el límite de productos del nuevo plan
        var cantidadProductos = await _context.Productos
            .Where(p => p.TiendaId == dto.TiendaId)
            .CountAsync();

        if (cantidadProductos > plan.MaxProductos)
        {
            return BadRequest(new {
                message = $"La tienda tiene {cantidadProductos} productos. El plan seleccionado solo permite {plan.MaxProductos}. Por favor, elimine productos antes de cambiar de plan."
            });
        }

        // Si la tienda tiene una suscripción previa, marcarla como "Cambiada"
        if (tienda.PlanSuscripcionId.HasValue)
        {
            var historialActual = await _context.HistorialSuscripciones
                .Where(h => h.TiendaId == dto.TiendaId && h.Estado == "Activa")
                .FirstOrDefaultAsync();

            if (historialActual != null)
            {
                historialActual.FechaFin = DateTime.UtcNow;
                historialActual.Estado = "Cambiada";
                historialActual.Notas = $"Cambiado a {plan.Nombre}";
            }
        }

        // Crear nuevo registro en el historial
        var nuevoHistorial = new HistorialSuscripcion
        {
            TiendaId = dto.TiendaId,
            PlanSuscripcionId = dto.PlanId,
            FechaInicio = DateTime.UtcNow,
            Estado = "Activa",
            MetodoPago = dto.MetodoPago ?? "Pendiente",
            TransaccionId = dto.TransaccionId,
            MontoTotal = plan.PrecioMensual,
            Notas = dto.Notas
        };

        _context.HistorialSuscripciones.Add(nuevoHistorial);

        // Actualizar la suscripción
        tienda.PlanSuscripcionId = dto.PlanId;
        tienda.MaxProductos = plan.MaxProductos;
        tienda.FechaSuscripcion = DateTime.UtcNow;
        tienda.FechaVencimientoSuscripcion = DateTime.UtcNow.AddMonths(1);
        tienda.FechaModificacion = DateTime.UtcNow;
        tienda.EstadoTienda = "Activa";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tienda {TiendaId} cambió al plan {PlanNombre}", dto.TiendaId, plan.Nombre);

        return Ok(new
        {
            message = "Suscripción actualizada exitosamente",
            plan = plan.Nombre,
            maxProductos = plan.MaxProductos,
            precioMensual = plan.PrecioMensual,
            fechaVencimiento = tienda.FechaVencimientoSuscripcion
        });
    }

    // GET: api/planes/historial/{tiendaId}
    [HttpGet("historial/{tiendaId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetHistorial(int tiendaId)
    {
        var historial = await _context.HistorialSuscripciones
            .Where(h => h.TiendaId == tiendaId)
            .Include(h => h.PlanSuscripcion)
            .OrderByDescending(h => h.FechaCreacion)
            .Select(h => new
            {
                h.Id,
                h.PlanSuscripcionId,
                PlanNombre = h.PlanSuscripcion.Nombre,
                h.FechaInicio,
                h.FechaFin,
                h.Estado,
                h.MetodoPago,
                h.TransaccionId,
                h.MontoTotal,
                h.Notas,
                h.FechaCreacion
            })
            .ToListAsync();

        return Ok(historial);
    }

    // POST: api/planes/cancelar/{tiendaId}
    [HttpPost("cancelar/{tiendaId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CancelarSuscripcion(int tiendaId)
    {
        var tienda = await _context.Tiendas.FindAsync(tiendaId);
        if (tienda == null)
        {
            return NotFound(new { message = "Tienda no encontrada" });
        }

        if (!tienda.PlanSuscripcionId.HasValue)
        {
            return BadRequest(new { message = "La tienda no tiene una suscripción activa" });
        }

        // Marcar el historial actual como cancelado
        var historialActual = await _context.HistorialSuscripciones
            .Where(h => h.TiendaId == tiendaId && h.Estado == "Activa")
            .FirstOrDefaultAsync();

        if (historialActual != null)
        {
            historialActual.FechaFin = DateTime.UtcNow;
            historialActual.Estado = "Cancelada";
            historialActual.Notas = "Cancelada por el usuario";
        }

        // Actualizar la tienda
        tienda.PlanSuscripcionId = null;
        tienda.FechaVencimientoSuscripcion = null;
        tienda.EstadoTienda = "Suspendida";
        tienda.FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tienda {TiendaId} canceló su suscripción", tiendaId);

        return Ok(new { message = "Suscripción cancelada exitosamente" });
    }

    // GET: api/planes/miplan
    [HttpGet("miplan")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetMiPlan()
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

        return Ok(new
        {
            tiendaId = tienda.Id,
            planActual = tienda.PlanSuscripcion != null ? new
            {
                tienda.PlanSuscripcion.Id,
                tienda.PlanSuscripcion.Nombre,
                tienda.PlanSuscripcion.Descripcion,
                tienda.PlanSuscripcion.MaxProductos,
                tienda.PlanSuscripcion.PrecioMensual
            } : null,
            tienda.FechaSuscripcion,
            tienda.FechaVencimientoSuscripcion,
            tienda.MaxProductos,
            tienda.EstadoTienda
        });
    }

    // POST: api/planes/iniciar-pago
    [HttpPost("iniciar-pago")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> IniciarPago([FromBody] IniciarPagoDto dto)
    {
        var tienda = await _context.Tiendas.FindAsync(dto.TiendaId);
        if (tienda == null)
        {
            return NotFound(new { message = "Tienda no encontrada" });
        }

        var plan = await _context.PlanesSuscripcion.FindAsync(dto.PlanId);
        if (plan == null || !plan.Activo)
        {
            return NotFound(new { message = "Plan no encontrado o inactivo" });
        }

        try
        {
            var frontendUrl = _configuration["FrontendUrl"];
            var urlSuccess = $"{frontendUrl}/emprendedor/configuracion?pago=success&tienda={dto.TiendaId}&plan={dto.PlanId}";
            var urlFailure = $"{frontendUrl}/emprendedor/configuracion?pago=failure";
            var urlPending = $"{frontendUrl}/emprendedor/configuracion?pago=pending";

            var preference = await _mercadoPagoService.CrearPreferenciaSuscripcionAsync(
                plan,
                tienda,
                dto.Email,
                urlSuccess,
                urlFailure,
                urlPending
            );

            return Ok(new
            {
                preferenceId = preference.Id,
                initPoint = preference.InitPoint,
                sandboxInitPoint = preference.SandboxInitPoint
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear preferencia de pago para suscripción");
            return StatusCode(500, new { message = "Error al iniciar el pago" });
        }
    }

    // POST: api/planes/confirmar-pago
    [HttpPost("confirmar-pago")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ConfirmarPago([FromBody] ConfirmarPagoDto dto)
    {
        var tienda = await _context.Tiendas.FindAsync(dto.TiendaId);
        if (tienda == null)
        {
            return NotFound(new { message = "Tienda no encontrada" });
        }

        var plan = await _context.PlanesSuscripcion.FindAsync(dto.PlanId);
        if (plan == null)
        {
            return NotFound(new { message = "Plan no encontrado" });
        }

        // Verificar el pago en MercadoPago si se proporciona paymentId
        if (dto.PaymentId.HasValue)
        {
            var pago = await _mercadoPagoService.ObtenerPagoAsync(dto.PaymentId.Value);
            if (pago == null || pago.Status != "approved")
            {
                return BadRequest(new { message = "El pago no fue aprobado" });
            }
        }

        // Si la tienda tiene una suscripción previa, marcarla como "Cambiada"
        if (tienda.PlanSuscripcionId.HasValue)
        {
            var historialActual = await _context.HistorialSuscripciones
                .Where(h => h.TiendaId == dto.TiendaId && h.Estado == "Activa")
                .FirstOrDefaultAsync();

            if (historialActual != null)
            {
                historialActual.FechaFin = DateTime.UtcNow;
                historialActual.Estado = "Cambiada";
                historialActual.Notas = $"Cambiado a {plan.Nombre}";
            }
        }

        // Crear nuevo registro en el historial
        var nuevoHistorial = new HistorialSuscripcion
        {
            TiendaId = dto.TiendaId,
            PlanSuscripcionId = dto.PlanId,
            FechaInicio = DateTime.UtcNow,
            Estado = "Activa",
            MetodoPago = "MercadoPago",
            TransaccionId = dto.PaymentId?.ToString() ?? dto.PreferenceId,
            MontoTotal = plan.PrecioMensual,
            Notas = "Pago procesado con MercadoPago"
        };

        _context.HistorialSuscripciones.Add(nuevoHistorial);

        // Actualizar la suscripción
        tienda.PlanSuscripcionId = dto.PlanId;
        tienda.MaxProductos = plan.MaxProductos;
        tienda.FechaSuscripcion = DateTime.UtcNow;
        tienda.FechaVencimientoSuscripcion = DateTime.UtcNow.AddMonths(1);
        tienda.FechaModificacion = DateTime.UtcNow;
        tienda.EstadoTienda = "Activa";

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Pago confirmado para tienda {TiendaId}, plan {PlanNombre}, payment {PaymentId}",
            dto.TiendaId, plan.Nombre, dto.PaymentId);

        return Ok(new
        {
            message = "Suscripción actualizada exitosamente",
            plan = plan.Nombre,
            fechaVencimiento = tienda.FechaVencimientoSuscripcion
        });
    }
}

public class SuscripcionDto
{
    public int TiendaId { get; set; }
    public int PlanId { get; set; }
    public string? MetodoPago { get; set; }
    public string? TransaccionId { get; set; }
    public string? Notas { get; set; }
}

public class IniciarPagoDto
{
    public int TiendaId { get; set; }
    public int PlanId { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class ConfirmarPagoDto
{
    public int TiendaId { get; set; }
    public int PlanId { get; set; }
    public long? PaymentId { get; set; }
    public string? PreferenceId { get; set; }
}
