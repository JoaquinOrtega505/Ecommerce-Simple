using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using System.Security.Claims;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    private async Task<int?> GetUserTiendaIdAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return null;
        }

        var usuario = await _context.Usuarios.FindAsync(userId);
        return usuario?.TiendaId;
    }

    [HttpGet("estadisticas")]
    public async Task<ActionResult<object>> GetEstadisticas()
    {
        var tiendaId = await GetUserTiendaIdAsync();
        if (tiendaId == null)
        {
            return BadRequest(new { message = "Usuario no asociado a ninguna tienda" });
        }

        var tienda = await _context.Tiendas.FindAsync(tiendaId.Value);
        if (tienda == null)
        {
            return NotFound(new { message = "Tienda no encontrada" });
        }

        // Productos
        var totalProductos = await _context.Productos
            .CountAsync(p => p.TiendaId == tiendaId.Value && p.Activo);

        var productosConStockBajo = await _context.Productos
            .CountAsync(p => p.TiendaId == tiendaId.Value && p.Activo && p.Stock <= 5);

        // Pedidos
        var totalPedidos = await _context.Pedidos
            .CountAsync(p => p.TiendaId == tiendaId.Value);

        var pedidosPendientes = await _context.Pedidos
            .CountAsync(p => p.TiendaId == tiendaId.Value && p.Estado == "Pendiente");

        var pedidosCompletados = await _context.Pedidos
            .CountAsync(p => p.TiendaId == tiendaId.Value && p.Estado == "Completado");

        // Ventas
        var ventasTotales = await _context.Pedidos
            .Where(p => p.TiendaId == tiendaId.Value && p.Estado == "Completado")
            .SumAsync(p => (decimal?)p.Total) ?? 0;

        var ventasDelMes = await _context.Pedidos
            .Where(p => p.TiendaId == tiendaId.Value &&
                       p.Estado == "Completado" &&
                       p.FechaCreacion.Month == DateTime.UtcNow.Month &&
                       p.FechaCreacion.Year == DateTime.UtcNow.Year)
            .SumAsync(p => (decimal?)p.Total) ?? 0;

        // Pedidos recientes
        var pedidosRecientes = await _context.Pedidos
            .Where(p => p.TiendaId == tiendaId.Value)
            .OrderByDescending(p => p.FechaCreacion)
            .Take(5)
            .Select(p => new
            {
                p.Id,
                p.Total,
                p.Estado,
                p.FechaCreacion,
                Cliente = p.Usuario.Nombre
            })
            .ToListAsync();

        // Productos más vendidos
        var productosMasVendidos = await _context.PedidoItems
            .Where(pi => pi.Pedido.TiendaId == tiendaId.Value && pi.Pedido.Estado == "Completado")
            .GroupBy(pi => new { pi.ProductoId, pi.Producto.Nombre })
            .Select(g => new
            {
                ProductoId = g.Key.ProductoId,
                Nombre = g.Key.Nombre,
                CantidadVendida = g.Sum(pi => pi.Cantidad)
            })
            .OrderByDescending(x => x.CantidadVendida)
            .Take(5)
            .ToListAsync();

        return Ok(new
        {
            Tienda = new
            {
                tienda.Id,
                tienda.Nombre,
                tienda.Subdominio,
                tienda.LogoUrl,
                tienda.MaxProductos
            },
            Productos = new
            {
                Total = totalProductos,
                Maximo = tienda.MaxProductos,
                StockBajo = productosConStockBajo,
                Disponibles = tienda.MaxProductos - totalProductos
            },
            Pedidos = new
            {
                Total = totalPedidos,
                Pendientes = pedidosPendientes,
                Completados = pedidosCompletados
            },
            Ventas = new
            {
                Totales = ventasTotales,
                DelMes = ventasDelMes
            },
            PedidosRecientes = pedidosRecientes,
            ProductosMasVendidos = productosMasVendidos
        });
    }

    [HttpGet("mi-tienda")]
    public async Task<ActionResult<object>> GetMiTienda()
    {
        var tiendaId = await GetUserTiendaIdAsync();
        if (tiendaId == null)
        {
            return BadRequest(new { message = "Usuario no asociado a ninguna tienda" });
        }

        var tienda = await _context.Tiendas.FindAsync(tiendaId.Value);
        if (tienda == null)
        {
            return NotFound(new { message = "Tienda no encontrada" });
        }

        return Ok(new
        {
            tienda.Id,
            tienda.Nombre,
            tienda.Subdominio,
            tienda.Descripcion,
            tienda.LogoUrl,
            tienda.BannerUrl,
            tienda.MercadoPagoPublicKey,
            tienda.EnvioHabilitado,
            tienda.ApiEnvioProveedor,
            tienda.MaxProductos,
            tienda.Activo
        });
    }

    [HttpGet("mis-pedidos")]
    public async Task<ActionResult<object>> GetMisPedidos([FromQuery] string? estado, [FromQuery] int? limit)
    {
        var tiendaId = await GetUserTiendaIdAsync();
        if (tiendaId == null)
        {
            return BadRequest(new { message = "Usuario no asociado a ninguna tienda" });
        }

        var query = _context.Pedidos
            .Include(p => p.Usuario)
            .Include(p => p.PedidoItems)
                .ThenInclude(pi => pi.Producto)
            .Where(p => p.TiendaId == tiendaId.Value)
            .AsQueryable();

        if (!string.IsNullOrEmpty(estado))
        {
            query = query.Where(p => p.Estado == estado);
        }

        query = query.OrderByDescending(p => p.FechaCreacion);

        if (limit.HasValue && limit.Value > 0)
        {
            query = query.Take(limit.Value);
        }

        var pedidos = await query
            .Select(p => new
            {
                p.Id,
                p.Total,
                p.Estado,
                p.FechaCreacion,
                p.FechaEntrega,
                p.DireccionEnvio,
                p.MetodoPago,
                Cliente = new
                {
                    p.Usuario.Id,
                    p.Usuario.Nombre,
                    p.Usuario.Email
                },
                Items = p.PedidoItems.Select(pi => new
                {
                    ProductoId = pi.ProductoId,
                    ProductoNombre = pi.Producto.Nombre,
                    Cantidad = pi.Cantidad,
                    PrecioUnitario = pi.PrecioUnitario,
                    Subtotal = pi.Cantidad * pi.PrecioUnitario
                }).ToList(),
                CantidadItems = p.PedidoItems.Sum(pi => pi.Cantidad)
            })
            .ToListAsync();

        return Ok(pedidos);
    }
}
