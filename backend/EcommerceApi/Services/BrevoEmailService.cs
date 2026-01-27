using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;

namespace EcommerceApi.Services;

public class BrevoEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BrevoEmailService> _logger;
    private readonly TransactionalEmailsApi _apiInstance;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public BrevoEmailService(IConfiguration configuration, ILogger<BrevoEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Leer API Key de variable de entorno o appsettings
        var apiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY")
            ?? _configuration["Brevo:ApiKey"]
            ?? throw new InvalidOperationException("Brevo API Key no configurada. Configure BREVO_API_KEY");

        _fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM_ADDRESS")
            ?? _configuration["Email:FromEmail"]
            ?? "noreply@ecommerce.com";

        _fromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME")
            ?? _configuration["Email:FromName"]
            ?? "E-Commerce Platform";

        // Configurar el cliente de Brevo
        Configuration.Default.ApiKey.Add("api-key", apiKey);
        _apiInstance = new TransactionalEmailsApi();
    }

    public async Task<bool> EnviarCodigoVerificacionAsync(string emailDestino, string codigo, string nombreUsuario)
    {
        try
        {
            var sendSmtpEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender(_fromName, _fromEmail),
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(emailDestino, nombreUsuario) },
                Subject = "Código de Verificación - E-Commerce",
                HtmlContent = GenerarHtmlCodigoVerificacion(codigo, nombreUsuario)
            };

            var result = await _apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            _logger.LogInformation("Email de verificación enviado a {Email} via Brevo. MessageId: {MessageId}",
                emailDestino, result.MessageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email de verificación a {Email} via Brevo", emailDestino);
            return false;
        }
    }

    public async Task<bool> EnviarConfirmacionPedidoAsync(string emailDestino, string nombreUsuario, int pedidoId, decimal total, List<ItemPedidoEmail> items)
    {
        try
        {
            var sendSmtpEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender(_fromName, _fromEmail),
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(emailDestino, nombreUsuario) },
                Subject = $"Confirmación de Pedido #{pedidoId} - E-Commerce",
                HtmlContent = GenerarHtmlConfirmacionPedido(nombreUsuario, pedidoId, total, items)
            };

            var result = await _apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            _logger.LogInformation("Email de confirmación de pedido #{PedidoId} enviado a {Email}", pedidoId, emailDestino);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar confirmación de pedido #{PedidoId} a {Email}", pedidoId, emailDestino);
            return false;
        }
    }

    public async Task<bool> EnviarNotificacionEnvioAsync(string emailDestino, string nombreUsuario, int pedidoId, string codigoSeguimiento)
    {
        try
        {
            var sendSmtpEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender(_fromName, _fromEmail),
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(emailDestino, nombreUsuario) },
                Subject = $"Tu pedido #{pedidoId} ha sido enviado - E-Commerce",
                HtmlContent = GenerarHtmlNotificacionEnvio(nombreUsuario, pedidoId, codigoSeguimiento)
            };

            var result = await _apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            _logger.LogInformation("Email de notificación de envío #{PedidoId} enviado a {Email}", pedidoId, emailDestino);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificación de envío #{PedidoId} a {Email}", pedidoId, emailDestino);
            return false;
        }
    }

    public async Task<bool> EnviarRecuperacionContrasenaAsync(string emailDestino, string nombreUsuario, string token)
    {
        try
        {
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                ?? _configuration["FrontendUrl"]
                ?? "http://localhost:4200";

            var resetUrl = $"{frontendUrl}/reset-password?token={token}";

            var sendSmtpEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender(_fromName, _fromEmail),
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(emailDestino, nombreUsuario) },
                Subject = "Recuperar Contraseña - E-Commerce",
                HtmlContent = GenerarHtmlRecuperacionContrasena(nombreUsuario, resetUrl)
            };

            var result = await _apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            _logger.LogInformation("Email de recuperación de contraseña enviado a {Email}", emailDestino);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email de recuperación a {Email}", emailDestino);
            return false;
        }
    }

    public async Task<bool> EnviarBienvenidaAsync(string emailDestino, string nombreUsuario)
    {
        try
        {
            var sendSmtpEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender(_fromName, _fromEmail),
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(emailDestino, nombreUsuario) },
                Subject = "¡Bienvenido a E-Commerce!",
                HtmlContent = GenerarHtmlBienvenida(nombreUsuario)
            };

            var result = await _apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            _logger.LogInformation("Email de bienvenida enviado a {Email}", emailDestino);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email de bienvenida a {Email}", emailDestino);
            return false;
        }
    }

    public string GenerarCodigoVerificacion()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    #region Templates HTML

    private string GenerarHtmlCodigoVerificacion(string codigo, string nombreUsuario)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .container {{ background-color: #f4f4f4; padding: 30px; border-radius: 10px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background-color: white; padding: 30px; border-radius: 0 0 10px 10px; }}
        .codigo {{ font-size: 32px; font-weight: bold; color: #007bff; text-align: center; padding: 20px; background-color: #f8f9fa; border-radius: 5px; letter-spacing: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>Verificación de Email</h1></div>
        <div class='content'>
            <p>Hola {nombreUsuario},</p>
            <p>Gracias por registrarte. Para completar tu registro, usa el siguiente código:</p>
            <div class='codigo'>{codigo}</div>
            <p>Este código expirará en <strong>15 minutos</strong>.</p>
            <p>Si no solicitaste este código, puedes ignorar este correo.</p>
            <p>Saludos,<br>El equipo de E-Commerce</p>
        </div>
        <div class='footer'><p>Este es un correo automático, por favor no respondas.</p></div>
    </div>
</body>
</html>";
    }

    private string GenerarHtmlConfirmacionPedido(string nombreUsuario, int pedidoId, decimal total, List<ItemPedidoEmail> items)
    {
        var itemsHtml = string.Join("", items.Select(i =>
            $"<tr><td style='padding:10px;border-bottom:1px solid #eee;'>{i.Nombre}</td><td style='padding:10px;border-bottom:1px solid #eee;text-align:center;'>{i.Cantidad}</td><td style='padding:10px;border-bottom:1px solid #eee;text-align:right;'>${i.Precio:N2}</td></tr>"));

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .container {{ background-color: #f4f4f4; padding: 30px; border-radius: 10px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background-color: white; padding: 30px; border-radius: 0 0 10px 10px; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th {{ background-color: #f8f9fa; padding: 10px; text-align: left; }}
        .total {{ font-size: 24px; font-weight: bold; color: #28a745; text-align: right; margin-top: 20px; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>¡Pedido Confirmado!</h1></div>
        <div class='content'>
            <p>Hola {nombreUsuario},</p>
            <p>Tu pedido <strong>#{pedidoId}</strong> ha sido confirmado.</p>
            <table>
                <tr><th>Producto</th><th style='text-align:center;'>Cantidad</th><th style='text-align:right;'>Precio</th></tr>
                {itemsHtml}
            </table>
            <div class='total'>Total: ${total:N2}</div>
            <p>Te notificaremos cuando tu pedido sea enviado.</p>
            <p>Saludos,<br>El equipo de E-Commerce</p>
        </div>
        <div class='footer'><p>Este es un correo automático.</p></div>
    </div>
</body>
</html>";
    }

    private string GenerarHtmlNotificacionEnvio(string nombreUsuario, int pedidoId, string codigoSeguimiento)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .container {{ background-color: #f4f4f4; padding: 30px; border-radius: 10px; }}
        .header {{ background-color: #17a2b8; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background-color: white; padding: 30px; border-radius: 0 0 10px 10px; }}
        .tracking {{ font-size: 20px; font-weight: bold; color: #17a2b8; text-align: center; padding: 15px; background-color: #f8f9fa; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>¡Tu pedido está en camino!</h1></div>
        <div class='content'>
            <p>Hola {nombreUsuario},</p>
            <p>Tu pedido <strong>#{pedidoId}</strong> ha sido enviado.</p>
            <p>Código de seguimiento:</p>
            <div class='tracking'>{codigoSeguimiento}</div>
            <p>Puedes rastrear tu envío con este código.</p>
            <p>Saludos,<br>El equipo de E-Commerce</p>
        </div>
        <div class='footer'><p>Este es un correo automático.</p></div>
    </div>
</body>
</html>";
    }

    private string GenerarHtmlRecuperacionContrasena(string nombreUsuario, string resetUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .container {{ background-color: #f4f4f4; padding: 30px; border-radius: 10px; }}
        .header {{ background-color: #ffc107; color: #333; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background-color: white; padding: 30px; border-radius: 0 0 10px 10px; }}
        .btn {{ display: inline-block; padding: 15px 30px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>Recuperar Contraseña</h1></div>
        <div class='content'>
            <p>Hola {nombreUsuario},</p>
            <p>Recibimos una solicitud para restablecer tu contraseña.</p>
            <p style='text-align:center;'><a href='{resetUrl}' class='btn'>Restablecer Contraseña</a></p>
            <p>Este enlace expirará en <strong>1 hora</strong>.</p>
            <p>Si no solicitaste esto, ignora este correo.</p>
            <p>Saludos,<br>El equipo de E-Commerce</p>
        </div>
        <div class='footer'><p>Este es un correo automático.</p></div>
    </div>
</body>
</html>";
    }

    private string GenerarHtmlBienvenida(string nombreUsuario)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .container {{ background-color: #f4f4f4; padding: 30px; border-radius: 10px; }}
        .header {{ background-color: #6f42c1; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background-color: white; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>¡Bienvenido!</h1></div>
        <div class='content'>
            <p>Hola {nombreUsuario},</p>
            <p>¡Gracias por unirte a E-Commerce!</p>
            <p>Ya puedes explorar nuestro catálogo y realizar compras.</p>
            <p>Si tienes preguntas, no dudes en contactarnos.</p>
            <p>Saludos,<br>El equipo de E-Commerce</p>
        </div>
        <div class='footer'><p>Este es un correo automático.</p></div>
    </div>
</body>
</html>";
    }

    #endregion
}

public class ItemPedidoEmail
{
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Precio { get; set; }
}
