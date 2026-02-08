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
    private readonly IConfiguration _configuration;
    private readonly MercadoPagoSuscripcionesService _mpService;

    public PlanesController(
        AppDbContext context,
        ILogger<PlanesController> logger,
        IConfiguration configuration,
        MercadoPagoSuscripcionesService mpService)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _mpService = mpService;
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

    // GET: api/planes/todos
    [HttpGet("todos")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<List<PlanSuscripcion>>> GetTodosLosPlanes()
    {
        var planes = await _context.PlanesSuscripcion
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

    // POST: api/planes
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<PlanSuscripcion>> CrearPlan([FromBody] CrearPlanDto dto)
    {
        var plan = new PlanSuscripcion
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            MaxProductos = dto.MaxProductos,
            PrecioMensual = dto.PrecioMensual,
            Activo = dto.Activo
        };

        _context.PlanesSuscripcion.Add(plan);
        await _context.SaveChangesAsync();

        // Sincronizar con MercadoPago si está conectado
        if (await _mpService.EstaConectadoAsync())
        {
            var mpResult = await _mpService.CrearPlanAsync(plan);
            if (mpResult.Success)
            {
                plan.MercadoPagoPlanId = mpResult.MercadoPagoPlanId;
                plan.MercadoPagoSyncDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("No se pudo crear el plan en MercadoPago: {Error}", mpResult.Error);
            }
        }

        _logger.LogInformation("Plan de suscripción creado: {PlanNombre} (ID: {PlanId})", plan.Nombre, plan.Id);

        return CreatedAtAction(nameof(GetPlan), new { id = plan.Id }, plan);
    }

    // PUT: api/planes/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> ActualizarPlan(int id, [FromBody] ActualizarPlanDto dto)
    {
        var plan = await _context.PlanesSuscripcion.FindAsync(id);
        if (plan == null)
        {
            return NotFound(new { message = "Plan no encontrado" });
        }

        plan.Nombre = dto.Nombre;
        plan.Descripcion = dto.Descripcion;
        plan.MaxProductos = dto.MaxProductos;
        plan.PrecioMensual = dto.PrecioMensual;
        plan.Activo = dto.Activo;

        // Sincronizar con MercadoPago si está conectado
        if (await _mpService.EstaConectadoAsync())
        {
            var mpResult = await _mpService.ActualizarPlanAsync(plan);
            if (mpResult.Success)
            {
                plan.MercadoPagoPlanId = mpResult.MercadoPagoPlanId;
                plan.MercadoPagoSyncDate = DateTime.UtcNow;
            }
            else
            {
                _logger.LogWarning("No se pudo actualizar el plan en MercadoPago: {Error}", mpResult.Error);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Plan de suscripción actualizado: {PlanNombre} (ID: {PlanId})", plan.Nombre, plan.Id);

        return Ok(new { message = "Plan actualizado exitosamente", plan });
    }

    // DELETE: api/planes/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> EliminarPlan(int id)
    {
        var plan = await _context.PlanesSuscripcion.FindAsync(id);
        if (plan == null)
        {
            return NotFound(new { message = "Plan no encontrado" });
        }

        // Verificar si hay tiendas usando este plan
        var tiendasConPlan = await _context.Tiendas
            .Where(t => t.PlanSuscripcionId == id)
            .CountAsync();

        if (tiendasConPlan > 0)
        {
            return BadRequest(new {
                message = $"No se puede eliminar el plan porque hay {tiendasConPlan} tienda(s) suscritas a él. Desactívelo en su lugar."
            });
        }

        // Desactivar en MercadoPago si está conectado
        if (!string.IsNullOrEmpty(plan.MercadoPagoPlanId) && await _mpService.EstaConectadoAsync())
        {
            await _mpService.DesactivarPlanAsync(plan.MercadoPagoPlanId);
        }

        _context.PlanesSuscripcion.Remove(plan);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Plan de suscripción eliminado: {PlanNombre} (ID: {PlanId})", plan.Nombre, plan.Id);

        return Ok(new { message = "Plan eliminado exitosamente" });
    }

    // POST: api/planes/sincronizar-mp
    [HttpPost("sincronizar-mp")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> SincronizarConMercadoPago()
    {
        if (!await _mpService.EstaConectadoAsync())
        {
            return BadRequest(new { message = "MercadoPago no está conectado. Configúrelo primero." });
        }

        var sincronizados = await _mpService.SincronizarTodosPlanesAsync();

        return Ok(new {
            message = $"Sincronización completada. {sincronizados} planes sincronizados con MercadoPago.",
            planesSincronizados = sincronizados
        });
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

    // POST: api/planes/confirmar-pago
    // Endpoint simplificado para confirmar pago de suscripción (sin MercadoPago por ahora)
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
            MetodoPago = "Manual",
            TransaccionId = dto.PreferenceId,
            MontoTotal = plan.PrecioMensual,
            Notas = "Pago confirmado manualmente"
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
            "Pago confirmado para tienda {TiendaId}, plan {PlanNombre}",
            dto.TiendaId, plan.Nombre);

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

public class ConfirmarPagoDto
{
    public int TiendaId { get; set; }
    public int PlanId { get; set; }
    public string? PreferenceId { get; set; }
}

public class CrearPlanDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int MaxProductos { get; set; }
    public decimal PrecioMensual { get; set; }
    public bool Activo { get; set; } = true;
}

public class ActualizarPlanDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int MaxProductos { get; set; }
    public decimal PrecioMensual { get; set; }
    public bool Activo { get; set; }
}
