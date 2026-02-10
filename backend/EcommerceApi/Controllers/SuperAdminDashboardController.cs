using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/superadmin/dashboard")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminDashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<SuperAdminDashboardController> _logger;

    public SuperAdminDashboardController(AppDbContext context, ILogger<SuperAdminDashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene métricas generales de suscripciones
    /// </summary>
    [HttpGet("metricas")]
    public async Task<ActionResult<object>> GetMetricas()
    {
        var ahora = DateTime.UtcNow;
        var inicioMes = new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Métricas de tiendas por estado
        var tiendasPorEstado = await _context.Tiendas
            .GroupBy(t => t.EstadoSuscripcion ?? "sin_suscripcion")
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        var tiendasPorEstadoTienda = await _context.Tiendas
            .GroupBy(t => t.EstadoTienda)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        // Totales
        var totalTiendas = await _context.Tiendas.CountAsync();
        var tiendasActivas = await _context.Tiendas.CountAsync(t => t.EstadoTienda == "Activa");
        var tiendasEnTrial = await _context.Tiendas.CountAsync(t => t.EstadoSuscripcion == "trial");
        var tiendasSuspendidas = await _context.Tiendas.CountAsync(t => t.EstadoTienda == "Suspendida");
        var tiendasPendienteEliminacion = await _context.Tiendas.CountAsync(t => t.EstadoTienda == "PendienteEliminacion");

        // Ingresos estimados (tiendas activas * precio de su plan)
        var ingresosMensualesEstimados = await _context.Tiendas
            .Where(t => t.EstadoSuscripcion == "active" && t.PlanSuscripcionId != null)
            .Include(t => t.PlanSuscripcion)
            .SumAsync(t => t.PlanSuscripcion!.PrecioMensual);

        // Trials por expirar en los próximos 3 días
        var fechaLimiteTrial = ahora.AddDays(3);
        var trialsPorExpirar = await _context.Tiendas
            .CountAsync(t => t.EstadoSuscripcion == "trial" &&
                             t.FechaFinTrial.HasValue &&
                             t.FechaFinTrial.Value <= fechaLimiteTrial &&
                             t.FechaFinTrial.Value > ahora);

        // Nuevas suscripciones del mes
        var nuevasSuscripcionesMes = await _context.Tiendas
            .CountAsync(t => t.FechaSuscripcion.HasValue &&
                             t.FechaSuscripcion.Value >= inicioMes);

        // Estado de MercadoPago
        var mpCredenciales = await _context.MercadoPagoCredenciales.FirstOrDefaultAsync();
        var mpConectado = mpCredenciales?.Conectado ?? false;

        return Ok(new
        {
            resumen = new
            {
                totalTiendas,
                tiendasActivas,
                tiendasEnTrial,
                tiendasSuspendidas,
                tiendasPendienteEliminacion,
                trialsPorExpirar,
                nuevasSuscripcionesMes
            },
            ingresos = new
            {
                mensualesEstimados = ingresosMensualesEstimados
            },
            tiendasPorEstadoSuscripcion = tiendasPorEstado,
            tiendasPorEstadoTienda = tiendasPorEstadoTienda,
            mercadoPago = new
            {
                conectado = mpConectado,
                email = mpCredenciales?.MercadoPagoEmail
            }
        });
    }

    /// <summary>
    /// Obtiene lista de tiendas suspendidas
    /// </summary>
    [HttpGet("tiendas-suspendidas")]
    public async Task<ActionResult<object>> GetTiendasSuspendidas()
    {
        var config = await _context.ConfiguracionSuscripciones.FirstOrDefaultAsync(c => c.Activo);
        var diasGracia = config?.DiasGraciaSuspension ?? 3;
        var ahora = DateTime.UtcNow;

        var tiendasSuspendidas = await _context.Tiendas
            .Include(t => t.PlanSuscripcion)
            .Include(t => t.Usuarios)
            .Where(t => t.EstadoTienda == "Suspendida" || t.EstadoTienda == "PendienteEliminacion")
            .OrderBy(t => t.FechaModificacion)
            .Select(t => new
            {
                t.Id,
                t.Nombre,
                t.Subdominio,
                t.EstadoTienda,
                t.EstadoSuscripcion,
                Plan = t.PlanSuscripcion != null ? t.PlanSuscripcion.Nombre : "Sin plan",
                FechaSuspension = t.FechaModificacion,
                DiasEnSuspension = t.FechaModificacion.HasValue
                    ? (int)(ahora - t.FechaModificacion.Value).TotalDays
                    : 0,
                DiasRestantesGracia = t.FechaModificacion.HasValue
                    ? Math.Max(0, diasGracia - (int)(ahora - t.FechaModificacion.Value).TotalDays)
                    : diasGracia,
                Propietario = t.Usuarios
                    .Where(u => u.Rol == "Emprendedor")
                    .Select(u => new { u.Nombre, u.Email })
                    .FirstOrDefault(),
                t.ReintentosPago
            })
            .ToListAsync();

        return Ok(new
        {
            total = tiendasSuspendidas.Count,
            diasGraciaConfiguracion = diasGracia,
            tiendas = tiendasSuspendidas
        });
    }

    /// <summary>
    /// Obtiene lista de tiendas en período de prueba
    /// </summary>
    [HttpGet("tiendas-trial")]
    public async Task<ActionResult<object>> GetTiendasTrial()
    {
        var ahora = DateTime.UtcNow;

        var tiendasTrial = await _context.Tiendas
            .Include(t => t.PlanSuscripcion)
            .Include(t => t.Usuarios)
            .Where(t => t.EstadoSuscripcion == "trial")
            .OrderBy(t => t.FechaFinTrial)
            .Select(t => new
            {
                t.Id,
                t.Nombre,
                t.Subdominio,
                Plan = t.PlanSuscripcion != null ? t.PlanSuscripcion.Nombre : "Sin plan",
                t.FechaInicioTrial,
                t.FechaFinTrial,
                DiasRestantes = t.FechaFinTrial.HasValue
                    ? Math.Max(0, (int)(t.FechaFinTrial.Value - ahora).TotalDays)
                    : 0,
                TieneMercadoPago = !string.IsNullOrEmpty(t.MercadoPagoSuscripcionId),
                Propietario = t.Usuarios
                    .Where(u => u.Rol == "Emprendedor")
                    .Select(u => new { u.Nombre, u.Email })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new
        {
            total = tiendasTrial.Count,
            tiendas = tiendasTrial
        });
    }

    /// <summary>
    /// Obtiene métricas de planes
    /// </summary>
    [HttpGet("planes")]
    public async Task<ActionResult<object>> GetMetricasPlanes()
    {
        var planes = await _context.PlanesSuscripcion
            .Select(p => new
            {
                p.Id,
                p.Nombre,
                p.PrecioMensual,
                p.MaxProductos,
                p.Activo,
                SincronizadoMP = !string.IsNullOrEmpty(p.MercadoPagoPlanId),
                TiendasActivas = _context.Tiendas.Count(t =>
                    t.PlanSuscripcionId == p.Id && t.EstadoSuscripcion == "active"),
                TiendasTrial = _context.Tiendas.Count(t =>
                    t.PlanSuscripcionId == p.Id && t.EstadoSuscripcion == "trial"),
                TotalTiendas = _context.Tiendas.Count(t => t.PlanSuscripcionId == p.Id)
            })
            .ToListAsync();

        var ingresosTotalesPorPlan = planes
            .Where(p => p.TiendasActivas > 0)
            .Sum(p => p.TiendasActivas * p.PrecioMensual);

        return Ok(new
        {
            planes,
            ingresosMensualesTotal = ingresosTotalesPorPlan
        });
    }
}
