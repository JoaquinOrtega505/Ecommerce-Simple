using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EcommerceApi.Data;
using EcommerceApi.Models;
using EcommerceApi.DTOs;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PedidosController : ControllerBase
{
    private readonly AppDbContext _context;

    public PedidosController(AppDbContext context)
    {
        _context = context;
    }

    private int GetUsuarioId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    [HttpGet]
    public async Task<ActionResult<List<PedidoDto>>> GetPedidos()
    {
        var usuarioId = GetUsuarioId();
        var esAdmin = User.IsInRole("Admin");

        var query = _context.Pedidos
            .Include(p => p.Usuario)
            .Include(p => p.Items)
            .ThenInclude(i => i.Producto)
            .AsQueryable();

        // Si no es admin, solo ver sus propios pedidos
        if (!esAdmin)
        {
            query = query.Where(p => p.UsuarioId == usuarioId);
        }

        var pedidos = await query
            .OrderByDescending(p => p.FechaCreacion)
            .Select(p => new PedidoDto
            {
                Id = p.Id,
                UsuarioId = p.UsuarioId,
                UsuarioNombre = p.Usuario!.Nombre,
                Total = p.Total,
                Estado = p.Estado,
                DireccionEnvio = p.DireccionEnvio,
                FechaCreacion = p.FechaCreacion,
                Items = p.Items.Select(i => new PedidoItemDto
                {
                    ProductoId = i.ProductoId,
                    ProductoNombre = i.Producto!.Nombre,
                    Cantidad = i.Cantidad,
                    PrecioUnitario = i.PrecioUnitario,
                    Subtotal = i.Subtotal
                }).ToList()
            })
            .ToListAsync();

        return Ok(pedidos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PedidoDto>> GetPedido(int id)
    {
        var usuarioId = GetUsuarioId();
        var esAdmin = User.IsInRole("Admin");

        var pedido = await _context.Pedidos
            .Include(p => p.Usuario)
            .Include(p => p.Items)
            .ThenInclude(i => i.Producto)
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync();

        if (pedido == null)
        {
            return NotFound(new { message = "Pedido no encontrado" });
        }

        // Verificar permisos
        if (!esAdmin && pedido.UsuarioId != usuarioId)
        {
            return Forbid();
        }

        var pedidoDto = new PedidoDto
        {
            Id = pedido.Id,
            UsuarioId = pedido.UsuarioId,
            UsuarioNombre = pedido.Usuario!.Nombre,
            Total = pedido.Total,
            Estado = pedido.Estado,
            DireccionEnvio = pedido.DireccionEnvio,
            FechaCreacion = pedido.FechaCreacion,
            Items = pedido.Items.Select(i => new PedidoItemDto
            {
                ProductoId = i.ProductoId,
                ProductoNombre = i.Producto!.Nombre,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario,
                Subtotal = i.Subtotal
            }).ToList()
        };

        return Ok(pedidoDto);
    }

    [HttpPost]
    public async Task<ActionResult<PedidoDto>> CrearPedido(CreatePedidoDto dto)
    {
        var usuarioId = GetUsuarioId();

        // Obtener items del carrito
        var carritoItems = await _context.CarritoItems
            .Include(ci => ci.Producto)
            .Where(ci => ci.UsuarioId == usuarioId)
            .ToListAsync();

        if (!carritoItems.Any())
        {
            return BadRequest(new { message = "El carrito está vacío" });
        }

        // Verificar stock de todos los productos
        foreach (var item in carritoItems)
        {
            if (item.Producto!.Stock < item.Cantidad)
            {
                return BadRequest(new { message = $"Stock insuficiente para {item.Producto.Nombre}" });
            }
        }

        // Crear pedido
        var pedido = new Pedido
        {
            UsuarioId = usuarioId,
            DireccionEnvio = dto.DireccionEnvio,
            Estado = "Pendiente"
        };

        // Crear items del pedido y actualizar stock
        foreach (var carritoItem in carritoItems)
        {
            var pedidoItem = new PedidoItem
            {
                ProductoId = carritoItem.ProductoId,
                Cantidad = carritoItem.Cantidad,
                PrecioUnitario = carritoItem.Producto!.Precio,
                Subtotal = carritoItem.Producto.Precio * carritoItem.Cantidad
            };

            pedido.Items.Add(pedidoItem);

            // Actualizar stock
            carritoItem.Producto.Stock -= carritoItem.Cantidad;
        }

        // Calcular total
        pedido.Total = pedido.Items.Sum(i => i.Subtotal);

        _context.Pedidos.Add(pedido);

        // Vaciar carrito
        _context.CarritoItems.RemoveRange(carritoItems);

        await _context.SaveChangesAsync();

        var usuario = await _context.Usuarios.FindAsync(usuarioId);

        var pedidoDto = new PedidoDto
        {
            Id = pedido.Id,
            UsuarioId = pedido.UsuarioId,
            UsuarioNombre = usuario!.Nombre,
            Total = pedido.Total,
            Estado = pedido.Estado,
            DireccionEnvio = pedido.DireccionEnvio,
            FechaCreacion = pedido.FechaCreacion,
            Items = pedido.Items.Select(i => new PedidoItemDto
            {
                ProductoId = i.ProductoId,
                ProductoNombre = i.Producto!.Nombre,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario,
                Subtotal = i.Subtotal
            }).ToList()
        };

        return CreatedAtAction(nameof(GetPedido), new { id = pedido.Id }, pedidoDto);
    }

    [HttpPut("{id}/estado")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ActualizarEstado(int id, UpdateEstadoDto dto)
    {
        var pedido = await _context.Pedidos.FindAsync(id);
        if (pedido == null)
        {
            return NotFound(new { message = "Pedido no encontrado" });
        }

        var estadosValidos = new[] { "Pendiente", "Pagado", "Enviado", "Entregado", "Cancelado" };
        if (!estadosValidos.Contains(dto.Estado))
        {
            return BadRequest(new { message = "Estado no válido" });
        }

        pedido.Estado = dto.Estado;

        if (dto.Estado == "Entregado")
        {
            pedido.FechaEntrega = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Estado actualizado" });
    }
}
