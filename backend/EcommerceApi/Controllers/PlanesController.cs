using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using EcommerceApi.Models;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlanesController : ControllerBase
{
    private readonly AppDbContext _context;

    public PlanesController(AppDbContext context)
    {
        _context = context;
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

        // Actualizar la suscripción
        tienda.PlanSuscripcionId = dto.PlanId;
        tienda.MaxProductos = plan.MaxProductos;
        tienda.FechaSuscripcion = DateTime.UtcNow;
        tienda.FechaVencimientoSuscripcion = DateTime.UtcNow.AddMonths(1);
        tienda.FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Suscripción actualizada exitosamente",
            plan = plan.Nombre,
            maxProductos = plan.MaxProductos,
            precioMensual = plan.PrecioMensual,
            fechaVencimiento = tienda.FechaVencimientoSuscripcion
        });
    }
}

public class SuscripcionDto
{
    public int TiendaId { get; set; }
    public int PlanId { get; set; }
}
