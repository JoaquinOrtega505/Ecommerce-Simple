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
public class CarritoController : ControllerBase
{
    private readonly AppDbContext _context;

    public CarritoController(AppDbContext context)
    {
        _context = context;
    }

    private int GetUsuarioId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    [HttpGet]
    public async Task<ActionResult<CarritoDto>> GetCarrito()
    {
        var usuarioId = GetUsuarioId();

        var items = await _context.CarritoItems
            .Include(ci => ci.Producto)
            .Where(ci => ci.UsuarioId == usuarioId)
            .Select(ci => new CarritoItemDto
            {
                Id = ci.Id,
                ProductoId = ci.ProductoId,
                ProductoNombre = ci.Producto!.Nombre,
                ProductoImagen = ci.Producto.ImagenUrl,
                PrecioUnitario = ci.Producto.Precio,
                Cantidad = ci.Cantidad,
                Subtotal = ci.Producto.Precio * ci.Cantidad
            })
            .ToListAsync();

        var carrito = new CarritoDto
        {
            Items = items,
            Total = items.Sum(i => i.Subtotal),
            TotalItems = items.Sum(i => i.Cantidad)
        };

        return Ok(carrito);
    }

    [HttpPost]
    public async Task<ActionResult> AgregarAlCarrito(AgregarCarritoDto dto)
    {
        var usuarioId = GetUsuarioId();

        // Verificar que el producto existe y tiene stock
        var producto = await _context.Productos.FindAsync(dto.ProductoId);
        if (producto == null || !producto.Activo)
        {
            return NotFound(new { message = "Producto no encontrado" });
        }

        if (producto.Stock < dto.Cantidad)
        {
            return BadRequest(new { message = "Stock insuficiente" });
        }

        // Verificar si ya existe en el carrito
        var itemExistente = await _context.CarritoItems
            .FirstOrDefaultAsync(ci => ci.UsuarioId == usuarioId && ci.ProductoId == dto.ProductoId);

        if (itemExistente != null)
        {
            // Actualizar cantidad
            itemExistente.Cantidad += dto.Cantidad;

            if (producto.Stock < itemExistente.Cantidad)
            {
                return BadRequest(new { message = "Stock insuficiente" });
            }
        }
        else
        {
            // Agregar nuevo item
            var nuevoItem = new CarritoItem
            {
                UsuarioId = usuarioId,
                ProductoId = dto.ProductoId,
                Cantidad = dto.Cantidad
            };

            _context.CarritoItems.Add(nuevoItem);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Producto agregado al carrito" });
    }

    [HttpPut("{itemId}")]
    public async Task<ActionResult> ActualizarCantidad(int itemId, ActualizarCarritoDto dto)
    {
        var usuarioId = GetUsuarioId();

        var item = await _context.CarritoItems
            .Include(ci => ci.Producto)
            .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.UsuarioId == usuarioId);

        if (item == null)
        {
            return NotFound(new { message = "Item no encontrado en el carrito" });
        }

        if (dto.Cantidad <= 0)
        {
            return BadRequest(new { message = "La cantidad debe ser mayor a 0" });
        }

        if (item.Producto!.Stock < dto.Cantidad)
        {
            return BadRequest(new { message = "Stock insuficiente" });
        }

        item.Cantidad = dto.Cantidad;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Cantidad actualizada" });
    }

    [HttpDelete("{itemId}")]
    public async Task<ActionResult> EliminarDelCarrito(int itemId)
    {
        var usuarioId = GetUsuarioId();

        var item = await _context.CarritoItems
            .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.UsuarioId == usuarioId);

        if (item == null)
        {
            return NotFound(new { message = "Item no encontrado en el carrito" });
        }

        _context.CarritoItems.Remove(item);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Producto eliminado del carrito" });
    }

    [HttpDelete]
    public async Task<ActionResult> VaciarCarrito()
    {
        var usuarioId = GetUsuarioId();

        var items = await _context.CarritoItems
            .Where(ci => ci.UsuarioId == usuarioId)
            .ToListAsync();

        _context.CarritoItems.RemoveRange(items);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Carrito vaciado" });
    }
}
