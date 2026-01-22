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

        var productos = await query
            .Select(p => new ProductoDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Precio = p.Precio,
                Stock = p.Stock,
                ImagenUrl = p.ImagenUrl,
                Activo = p.Activo,
                CategoriaId = p.CategoriaId,
                CategoriaNombre = p.Categoria!.Nombre
            })
            .ToListAsync();

        return Ok(productos);
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

        var productos = await query
            .OrderByDescending(p => p.FechaCreacion)
            .Select(p => new ProductoDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Precio = p.Precio,
                Stock = p.Stock,
                ImagenUrl = p.ImagenUrl,
                Activo = p.Activo,
                CategoriaId = p.CategoriaId,
                CategoriaNombre = p.Categoria!.Nombre
            })
            .ToListAsync();

        return Ok(productos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductoDto>> GetProducto(int id)
    {
        var producto = await _context.Productos
            .Include(p => p.Categoria)
            .Where(p => p.Id == id)
            .Select(p => new ProductoDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Precio = p.Precio,
                Stock = p.Stock,
                ImagenUrl = p.ImagenUrl,
                Activo = p.Activo,
                CategoriaId = p.CategoriaId,
                CategoriaNombre = p.Categoria!.Nombre
            })
            .FirstOrDefaultAsync();

        if (producto == null)
        {
            return NotFound(new { message = "Producto no encontrado" });
        }

        return Ok(producto);
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
            CategoriaId = dto.CategoriaId,
            TiendaId = tiendaId.Value,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();

        var categoria = await _context.Categorias.FindAsync(dto.CategoriaId);

        var productoDto = new ProductoDto
        {
            Id = producto.Id,
            Nombre = producto.Nombre,
            Descripcion = producto.Descripcion,
            Precio = producto.Precio,
            Stock = producto.Stock,
            ImagenUrl = producto.ImagenUrl,
            Activo = producto.Activo,
            CategoriaId = producto.CategoriaId,
            CategoriaNombre = categoria?.Nombre ?? ""
        };

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
        producto.Activo = dto.Activo;
        producto.CategoriaId = dto.CategoriaId;

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
