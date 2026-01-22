using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using EcommerceApi.Models;
using EcommerceApi.DTOs;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsuariosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<UsuarioDto>>> GetUsuarios()
    {
        var usuarios = await _context.Usuarios
            .Select(u => new UsuarioDto
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Email = u.Email,
                Rol = u.Rol,
                FechaCreacion = u.FechaCreacion,
                TiendaId = u.TiendaId
            })
            .ToListAsync();

        return Ok(usuarios);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UsuarioDto>> GetUsuario(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }

        var usuarioDto = new UsuarioDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Rol = usuario.Rol,
            FechaCreacion = usuario.FechaCreacion,
            TiendaId = usuario.TiendaId
        };

        return Ok(usuarioDto);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> CrearUsuario(CreateUsuarioDto dto)
    {
        // Verificar si el email ya existe
        if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest(new { message = "El email ya está registrado" });
        }

        // Validar rol
        var rolesValidos = new[] { "Admin", "Deposito", "Cliente" };
        if (!rolesValidos.Contains(dto.Rol))
        {
            return BadRequest(new { message = "Rol no válido. Roles permitidos: Admin, Deposito, Cliente" });
        }

        var usuario = new Usuario
        {
            Nombre = dto.Nombre,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Rol = dto.Rol,
            TiendaId = dto.TiendaId,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        var usuarioDto = new UsuarioDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Rol = usuario.Rol,
            FechaCreacion = usuario.FechaCreacion,
            TiendaId = usuario.TiendaId
        };

        return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, usuarioDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> ActualizarUsuario(int id, UpdateUsuarioDto dto)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        
        if (usuario == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }

        // Verificar si el nuevo email ya existe (excepto para el mismo usuario)
        if (dto.Email != usuario.Email && await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest(new { message = "El email ya está registrado" });
        }

        usuario.Nombre = dto.Nombre;
        usuario.Email = dto.Email;
        usuario.TiendaId = dto.TiendaId;

        if (!string.IsNullOrEmpty(dto.Password))
        {
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        if (!string.IsNullOrEmpty(dto.Rol))
        {
            var rolesValidos = new[] { "Admin", "Deposito", "Cliente" };
            if (!rolesValidos.Contains(dto.Rol))
            {
                return BadRequest(new { message = "Rol no válido" });
            }
            usuario.Rol = dto.Rol;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Usuario actualizado correctamente" });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> EliminarUsuario(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        
        if (usuario == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }

        // No permitir eliminar el usuario administrador principal
        if (usuario.Id == 1)
        {
            return BadRequest(new { message = "No se puede eliminar el usuario administrador principal" });
        }

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Usuario eliminado correctamente" });
    }
}
