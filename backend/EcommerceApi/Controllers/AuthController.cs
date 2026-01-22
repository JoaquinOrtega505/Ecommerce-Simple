using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EcommerceApi.Data;
using EcommerceApi.Models;
using EcommerceApi.DTOs;
using EcommerceApi.Services;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly EmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext context,
        IConfiguration configuration,
        EmailService emailService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        // Verificar si el email ya existe
        if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest(new { message = "El email ya está registrado" });
        }

        // Generar código de verificación
        var codigoVerificacion = _emailService.GenerarCodigoVerificacion();

        // Crear nuevo usuario con rol Admin (emprendedor) automáticamente
        var usuario = new Usuario
        {
            Nombre = dto.Nombre,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Rol = "Admin", // Auto-asignar rol Admin para emprendedores
            TiendaId = null, // Sin tienda asignada hasta que la cree
            EmailVerificado = false,
            CodigoVerificacion = codigoVerificacion,
            FechaExpiracionCodigo = DateTime.UtcNow.AddMinutes(15),
            FechaCreacion = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        // Enviar email de verificación
        try
        {
            await _emailService.EnviarCodigoVerificacionAsync(
                usuario.Email,
                codigoVerificacion,
                usuario.Nombre);

            _logger.LogInformation("Código de verificación enviado a {Email} durante el registro", usuario.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email de verificación durante el registro");
            // Continuar con el registro aunque falle el email
        }

        // Generar token
        var token = GenerateJwtToken(usuario);

        return Ok(new AuthResponseDto
        {
            Token = token,
            UsuarioId = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Rol = usuario.Rol,
            EmailVerificado = usuario.EmailVerificado
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        // Buscar usuario por email
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
        {
            return Unauthorized(new { message = "Email o contraseña incorrectos" });
        }

        // Generar token
        var token = GenerateJwtToken(usuario);

        return Ok(new AuthResponseDto
        {
            Token = token,
            UsuarioId = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Rol = usuario.Rol,
            EmailVerificado = usuario.EmailVerificado,
            TiendaId = usuario.TiendaId
        });
    }

    private string GenerateJwtToken(Usuario usuario)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret no configurado");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(Convert.ToDouble(jwtSettings["ExpirationHours"])),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
