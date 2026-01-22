using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using EcommerceApi.Services;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailVerificationController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<EmailVerificationController> _logger;

    public EmailVerificationController(
        AppDbContext context,
        EmailService emailService,
        ILogger<EmailVerificationController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Envía un código de verificación al email del usuario
    /// </summary>
    [HttpPost("enviar-codigo")]
    public async Task<ActionResult> EnviarCodigoVerificacion([FromBody] EnviarCodigoRequest request)
    {
        try
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            if (usuario.EmailVerificado)
            {
                return BadRequest(new { message = "El email ya está verificado" });
            }

            // Generar código de 6 dígitos
            var codigo = _emailService.GenerarCodigoVerificacion();

            // Guardar código en la base de datos con expiración de 15 minutos
            usuario.CodigoVerificacion = codigo;
            usuario.FechaExpiracionCodigo = DateTime.UtcNow.AddMinutes(15);

            await _context.SaveChangesAsync();

            // Enviar email
            var emailEnviado = await _emailService.EnviarCodigoVerificacionAsync(
                usuario.Email,
                codigo,
                usuario.Nombre);

            if (!emailEnviado)
            {
                return StatusCode(500, new { message = "Error al enviar el email. Por favor intenta nuevamente." });
            }

            _logger.LogInformation("Código de verificación enviado a {Email}", usuario.Email);

            return Ok(new
            {
                message = "Código de verificación enviado a tu email",
                email = usuario.Email
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar código de verificación");
            return StatusCode(500, new { message = "Error al procesar la solicitud" });
        }
    }

    /// <summary>
    /// Verifica el código ingresado por el usuario
    /// </summary>
    [HttpPost("verificar-codigo")]
    public async Task<ActionResult> VerificarCodigo([FromBody] VerificarCodigoRequest request)
    {
        try
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            if (usuario.EmailVerificado)
            {
                return Ok(new { message = "El email ya está verificado", verificado = true });
            }

            // Verificar que el código no haya expirado
            if (usuario.FechaExpiracionCodigo == null || usuario.FechaExpiracionCodigo < DateTime.UtcNow)
            {
                return BadRequest(new { message = "El código ha expirado. Solicita uno nuevo." });
            }

            // Verificar que el código sea correcto
            if (usuario.CodigoVerificacion != request.Codigo)
            {
                return BadRequest(new { message = "Código incorrecto" });
            }

            // Marcar email como verificado
            usuario.EmailVerificado = true;
            usuario.CodigoVerificacion = null;
            usuario.FechaExpiracionCodigo = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Email verificado exitosamente para {Email}", usuario.Email);

            return Ok(new
            {
                message = "Email verificado exitosamente",
                verificado = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar código");
            return StatusCode(500, new { message = "Error al procesar la solicitud" });
        }
    }

    /// <summary>
    /// Reenvía el código de verificación
    /// </summary>
    [HttpPost("reenviar-codigo")]
    public async Task<ActionResult> ReenviarCodigo([FromBody] EnviarCodigoRequest request)
    {
        // Utiliza el mismo endpoint de enviar código
        return await EnviarCodigoVerificacion(request);
    }
}

public class EnviarCodigoRequest
{
    public string Email { get; set; } = string.Empty;
}

public class VerificarCodigoRequest
{
    public string Email { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
}
