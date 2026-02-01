using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using EcommerceApi.Models;
using EcommerceApi.DTOs;
using System.Security.Claims;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductosController(AppDbContext context)
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

    private static ProductoDto MapToDto(Producto p, string categoriaNombre)
    {
        var dto = new ProductoDto
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            Precio = p.Precio,
            Stock = p.Stock,
            ImagenUrl = p.ImagenUrl,
            ImagenUrl2 = p.ImagenUrl2,
            ImagenUrl3 = p.ImagenUrl3,
            Activo = p.Activo,
            CategoriaId = p.CategoriaId,
            CategoriaNombre = categoriaNombre
        };

        // Build Imagenes list
        dto.Imagenes = new List<string> { p.ImagenUrl };
        if (!string.IsNullOrEmpty(p.ImagenUrl2)) dto.Imagenes.Add(p.ImagenUrl2);
        if (!string.IsNullOrEmpty(p.ImagenUrl3)) dto.Imagenes.Add(p.ImagenUrl3);

        return dto;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductoDto>>> GetProductos([FromQuery] int? categoriaId, [FromQuery] string? buscar)
    {
        var query = _context.Productos
            .Include(p => p.Categoria)
            .Where(p => p.Activo)
            .AsQueryable();

        if (categoriaId.HasValue)
        {
            query = query.Where(p => p.CategoriaId == categoriaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            query = query.Where(p => p.Nombre.Contains(buscar) || p.Descripcion.Contains(buscar));
        }

        var productos = await query.ToListAsync();
        var productoDtos = productos.Select(p => MapToDto(p, p.Categoria?.Nombre ?? "")).ToList();

        return Ok(productoDtos);
    }

    [HttpGet("mis-productos")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ProductoDto>>> GetMisProductos([FromQuery] int? categoriaId, [FromQuery] string? buscar, [FromQuery] bool? incluirInactivos = false)
    {
        var tiendaId = await GetUserTiendaIdAsync();
        if (tiendaId == null)
        {
            return BadRequest(new { message = "Usuario no asociado a ninguna tienda" });
        }

        var query = _context.Productos
            .Include(p => p.Categoria)
            .Where(p => p.TiendaId == tiendaId.Value)
            .AsQueryable();

        if (!incluirInactivos.GetValueOrDefault())
        {
            query = query.Where(p => p.Activo);
        }

        if (categoriaId.HasValue)
        {
            query = query.Where(p => p.CategoriaId == categoriaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            query = query.Where(p => p.Nombre.Contains(buscar) || p.Descripcion.Contains(buscar));
        }

        var productos = await query.OrderByDescending(p => p.FechaCreacion).ToListAsync();
        var productoDtos = productos.Select(p => MapToDto(p, p.Categoria?.Nombre ?? "")).ToList();

        return Ok(productoDtos);
    }

    // GET: api/productos/tienda/{tiendaId} - Público (para vista de tienda pública)
    [HttpGet("tienda/{tiendaId}")]
    public async Task<ActionResult<List<ProductoDto>>> GetProductosPorTienda(int tiendaId)
    {
        var productos = await _context.Productos
            .Include(p => p.Categoria)
            .Where(p => p.TiendaId == tiendaId && p.Activo)
            .OrderByDescending(p => p.FechaCreacion)
            .ToListAsync();

        var productoDtos = productos.Select(p => MapToDto(p, p.Categoria?.Nombre ?? "")).ToList();

        return Ok(productoDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductoDto>> GetProducto(int id)
    {
        var producto = await _context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (producto == null)
        {
            return NotFound(new { message = "Producto no encontrado" });
        }

        var productoDto = MapToDto(producto, producto.Categoria?.Nombre ?? "");
        return Ok(productoDto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductoDto>> CreateProducto(CreateProductoDto dto)
    {
        var tiendaId = await GetUserTiendaIdAsync();
        if (tiendaId == null)
        {
            return BadRequest(new { message = "Usuario no asociado a ninguna tienda" });
        }

        // Verificar que la tienda no haya alcanzado el límite de productos
        var tienda = await _context.Tiendas.FindAsync(tiendaId.Value);
        if (tienda == null)
        {
            return BadRequest(new { message = "Tienda no encontrada" });
        }

        var totalProductos = await _context.Productos.CountAsync(p => p.TiendaId == tiendaId.Value && p.Activo);
        if (totalProductos >= tienda.MaxProductos)
        {
            return BadRequest(new { message = $"Has alcanzado el límite de {tienda.MaxProductos} productos para tu tienda" });
        }

        var producto = new Producto
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Precio = dto.Precio,
            Stock = dto.Stock,
            ImagenUrl = dto.ImagenUrl,
            ImagenUrl2 = dto.ImagenUrl2,
            ImagenUrl3 = dto.ImagenUrl3,
            CategoriaId = dto.CategoriaId,
            TiendaId = tiendaId.Value,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();

        var categoria = await _context.Categorias.FindAsync(dto.CategoriaId);
        var productoDto = MapToDto(producto, categoria?.Nombre ?? "");

        return CreatedAtAction(nameof(GetProducto), new { id = producto.Id }, productoDto);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateProducto(int id, UpdateProductoDto dto)
    {
        var tiendaId = await GetUserTiendaIdAsync();
        if (tiendaId == null)
        {
            return BadRequest(new { message = "Usuario no asociado a ninguna tienda" });
        }

        var producto = await _context.Productos.FindAsync(id);
        if (producto == null)
        {
            return NotFound(new { message = "Producto no encontrado" });
        }

        // Verificar que el producto pertenece a la tienda del usuario
        if (producto.TiendaId != tiendaId.Value)
        {
            return Forbid();
        }

        producto.Nombre = dto.Nombre;
        producto.Descripcion = dto.Descripcion;
        producto.Precio = dto.Precio;
        producto.Stock = dto.Stock;
        producto.ImagenUrl = dto.ImagenUrl;
        producto.ImagenUrl2 = dto.ImagenUrl2;
        producto.ImagenUrl3 = dto.ImagenUrl3;
        producto.Activo = dto.Activo;
        producto.CategoriaId = dto.CategoriaId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}/toggle-activo")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ToggleActivoProducto(int id, [FromBody] ToggleActivoDto dto)
    {
        var tiendaId = await GetUserTiendaIdAsync();
        if (tiendaId == null)
        {
            return BadRequest(new { message = "Usuario no asociado a ninguna tienda" });
        }

        var producto = await _context.Productos.FindAsync(id);
        if (producto == null)
        {
            return NotFound(new { message = "Producto no encontrado" });
        }

        // Verificar que el producto pertenece a la tienda del usuario
        if (producto.TiendaId != tiendaId.Value)
        {
            return Forbid();
        }

        producto.Activo = dto.Activo;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteProducto(int id)
    {
        var tiendaId = await GetUserTiendaIdAsync();
        if (tiendaId == null)
        {
            return BadRequest(new { message = "Usuario no asociado a ninguna tienda" });
        }

        var producto = await _context.Productos.FindAsync(id);
        if (producto == null)
        {
            return NotFound(new { message = "Producto no encontrado" });
        }

        // Verificar que el producto pertenece a la tienda del usuario
        if (producto.TiendaId != tiendaId.Value)
        {
            return Forbid();
        }

        // Soft delete - solo marcamos como inactivo
        producto.Activo = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
