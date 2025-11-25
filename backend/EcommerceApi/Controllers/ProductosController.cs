using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using EcommerceApi.Models;
using EcommerceApi.DTOs;

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
        var producto = new Producto
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Precio = dto.Precio,
            Stock = dto.Stock,
            ImagenUrl = dto.ImagenUrl,
            CategoriaId = dto.CategoriaId,
            Activo = true
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
        var producto = await _context.Productos.FindAsync(id);
        if (producto == null)
        {
            return NotFound(new { message = "Producto no encontrado" });
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
        var producto = await _context.Productos.FindAsync(id);
        if (producto == null)
        {
            return NotFound(new { message = "Producto no encontrado" });
        }

        // Soft delete - solo marcamos como inactivo
        producto.Activo = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
