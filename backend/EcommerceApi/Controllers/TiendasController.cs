using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceApi.Models;
using EcommerceApi.Services;
using static EcommerceApi.Services.TiendaService;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TiendasController : ControllerBase
{
    private readonly TiendaService _tiendaService;
    private readonly ILogger<TiendasController> _logger;

    public TiendasController(TiendaService tiendaService, ILogger<TiendasController> logger)
    {
        _tiendaService = tiendaService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todas las tiendas activas
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Tienda>>> GetTiendasActivas()
    {
        try
        {
            var tiendas = await _tiendaService.ObtenerTiendasActivasAsync();
            return Ok(tiendas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tiendas activas");
            return StatusCode(500, new { message = "Error al obtener tiendas" });
        }
    }

    /// <summary>
    /// Obtiene todas las tiendas (solo SuperAdmin)
    /// </summary>
    [HttpGet("todas")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<List<Tienda>>> GetTodasLasTiendas()
    {
        try
        {
            var tiendas = await _tiendaService.ObtenerTodasLasTiendasAsync();
            return Ok(tiendas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las tiendas");
            return StatusCode(500, new { message = "Error al obtener tiendas" });
        }
    }

    /// <summary>
    /// Obtiene una tienda por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Tienda>> GetTienda(int id)
    {
        try
        {
            var tienda = await _tiendaService.ObtenerTiendaPorIdAsync(id);
            if (tienda == null)
            {
                return NotFound(new { message = "Tienda no encontrada" });
            }

            return Ok(tienda);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tienda {TiendaId}", id);
            return StatusCode(500, new { message = "Error al obtener tienda" });
        }
    }

    /// <summary>
    /// Obtiene una tienda por subdominio
    /// </summary>
    [HttpGet("subdominio/{subdominio}")]
    public async Task<ActionResult<Tienda>> GetTiendaPorSubdominio(string subdominio)
    {
        try
        {
            var tienda = await _tiendaService.ObtenerTiendaPorSubdominioAsync(subdominio);
            if (tienda == null)
            {
                return NotFound(new { message = $"Tienda con subdominio '{subdominio}' no encontrada" });
            }

            return Ok(tienda);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tienda por subdominio {Subdominio}", subdominio);
            return StatusCode(500, new { message = "Error al obtener tienda" });
        }
    }

    /// <summary>
    /// Crea una nueva tienda (solo SuperAdmin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<Tienda>> CrearTienda([FromBody] Tienda tienda)
    {
        try
        {
            var nuevaTienda = await _tiendaService.CrearTiendaAsync(tienda);
            return CreatedAtAction(nameof(GetTienda), new { id = nuevaTienda.Id }, nuevaTienda);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tienda");
            return StatusCode(500, new { message = "Error al crear tienda" });
        }
    }

    /// <summary>
    /// Permite a un Admin crear su propia tienda (onboarding)
    /// </summary>
    [HttpPost("mi-tienda")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Tienda>> CrearMiTienda([FromBody] CreateTiendaDto dto)
    {
        try
        {
            // Obtener el ID del usuario del token
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int usuarioId))
            {
                return Unauthorized(new { message = "Usuario no autenticado" });
            }

            var nuevaTienda = await _tiendaService.CrearTiendaParaUsuarioAsync(dto, usuarioId);
            return CreatedAtAction(nameof(GetTienda), new { id = nuevaTienda.Id }, nuevaTienda);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tienda para usuario");
            return StatusCode(500, new { message = "Error al crear tienda" });
        }
    }

    /// <summary>
    /// Actualiza una tienda existente (SuperAdmin o Admin de la tienda)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<Tienda>> ActualizarTienda(int id, [FromBody] Tienda tienda)
    {
        try
        {
            // TODO: Validar que el usuario Admin solo pueda editar su propia tienda
            var tiendaActualizada = await _tiendaService.ActualizarTiendaAsync(id, tienda);
            return Ok(tiendaActualizada);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Tienda no encontrada" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar tienda {TiendaId}", id);
            return StatusCode(500, new { message = "Error al actualizar tienda" });
        }
    }

    /// <summary>
    /// Desactiva una tienda (solo SuperAdmin)
    /// </summary>
    [HttpPut("{id}/desactivar")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DesactivarTienda(int id)
    {
        try
        {
            await _tiendaService.DesactivarTiendaAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Tienda no encontrada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar tienda {TiendaId}", id);
            return StatusCode(500, new { message = "Error al desactivar tienda" });
        }
    }

    /// <summary>
    /// Activa una tienda (solo SuperAdmin)
    /// </summary>
    [HttpPut("{id}/activar")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ActivarTienda(int id)
    {
        try
        {
            await _tiendaService.ActivarTiendaAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Tienda no encontrada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al activar tienda {TiendaId}", id);
            return StatusCode(500, new { message = "Error al activar tienda" });
        }
    }

    /// <summary>
    /// Elimina una tienda permanentemente (solo SuperAdmin)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> EliminarTienda(int id)
    {
        try
        {
            await _tiendaService.EliminarTiendaAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Tienda no encontrada" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar tienda {TiendaId}", id);
            return StatusCode(500, new { message = "Error al eliminar tienda" });
        }
    }

    /// <summary>
    /// Obtiene estadísticas de una tienda
    /// </summary>
    [HttpGet("{id}/estadisticas")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> GetEstadisticasTienda(int id)
    {
        try
        {
            // TODO: Validar que el usuario Admin solo pueda ver estadísticas de su propia tienda
            var estadisticas = await _tiendaService.ObtenerEstadisticasTiendaAsync(id);
            return Ok(estadisticas);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Tienda no encontrada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas de tienda {TiendaId}", id);
            return StatusCode(500, new { message = "Error al obtener estadísticas" });
        }
    }

    /// <summary>
    /// Verifica si una tienda puede agregar más productos
    /// </summary>
    [HttpGet("{id}/puede-agregar-producto")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<bool>> PuedeAgregarProducto(int id)
    {
        try
        {
            var puede = await _tiendaService.PuedeAgregarProductoAsync(id);
            return Ok(new { puedeAgregar = puede });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar límite de productos para tienda {TiendaId}", id);
            return StatusCode(500, new { message = "Error al verificar límite de productos" });
        }
    }
}
