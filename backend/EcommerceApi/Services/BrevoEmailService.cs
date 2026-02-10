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

        // Configurar el cliente de Brevo (usar indexer para evitar error de duplicado)
        Configuration.Default.ApiKey["api-key"] = apiKey;
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

    #region Notificaciones de Suscripción

    /// <summary>
    /// Envía notificación de que el período de prueba está por expirar
    /// </summary>
    public async Task<bool> EnviarAvisoTrialPorExpirarAsync(
        string emailDestino,
        string nombreUsuario,
        string nombreTienda,
        string nombrePlan,
        int diasRestantes,
        decimal precioMensual)
    {
        try
        {
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                ?? _configuration["FrontendUrl"]
                ?? "http://localhost:4200";

            var sendSmtpEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender(_fromName, _fromEmail),
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(emailDestino, nombreUsuario) },
                Subject = $"Tu período de prueba termina en {diasRestantes} día(s) - {nombreTienda}",
                HtmlContent = GenerarHtmlAvisoTrial(nombreUsuario, nombreTienda, nombrePlan, diasRestantes, precioMensual, frontendUrl)
            };

            var result = await _apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            _logger.LogInformation("Email de aviso de trial enviado a {Email} para tienda {Tienda}",
                emailDestino, nombreTienda);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar aviso de trial a {Email}", emailDestino);
            return false;
        }
    }

    /// <summary>
    /// Envía notificación de pago exitoso de suscripción
    /// </summary>
    public async Task<bool> EnviarPagoSuscripcionExitosoAsync(
        string emailDestino,
        string nombreUsuario,
        string nombreTienda,
        string nombrePlan,
        decimal monto,
        DateTime proximoPago)
    {
        try
        {
            var sendSmtpEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender(_fromName, _fromEmail),
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(emailDestino, nombreUsuario) },
                Subject = $"Pago confirmado - {nombreTienda}",
                HtmlContent = GenerarHtmlPagoExitoso(nombreUsuario, nombreTienda, nombrePlan, monto, proximoPago)
            };

            var result = await _apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            _logger.LogInformation("Email de pago exitoso enviado a {Email} para tienda {Tienda}",
                emailDestino, nombreTienda);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar confirmación de pago a {Email}", emailDestino);
            return false;
        }
    }

    /// <summary>
    /// Envía notificación de pago fallido de suscripción
    /// </summary>
    public async Task<bool> EnviarPagoSuscripcionFallidoAsync(
        string emailDestino,
        string nombreUsuario,
        string nombreTienda,
        string nombrePlan,
        int intentosRestantes)
    {
        try
        {
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                ?? _configuration["FrontendUrl"]
                ?? "http://localhost:4200";

            var sendSmtpEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender(_fromName, _fromEmail),
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(emailDestino, nombreUsuario) },
                Subject = $"⚠️ Problema con tu pago - {nombreTienda}",
                HtmlContent = GenerarHtmlPagoFallido(nombreUsuario, nombreTienda, nombrePlan, intentosRestantes, frontendUrl)
            };

            var result = await _apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            _logger.LogInformation("Email de pago fallido enviado a {Email} para tienda {Tienda}",
                emailDestino, nombreTienda);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificación de pago fallido a {Email}", emailDestino);
            return false;
        }
    }

    /// <summary>
    /// Envía notificación de tienda suspendida
    /// </summary>
    public async Task<bool> EnviarTiendaSuspendidaAsync(
        string emailDestino,
        string nombreUsuario,
        string nombreTienda,
        string motivo,
        int diasGracia)
    {
        try
        {
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                ?? _configuration["FrontendUrl"]
                ?? "http://localhost:4200";

            var sendSmtpEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender(_fromName, _fromEmail),
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(emailDestino, nombreUsuario) },
                Subject = $"🚨 Tu tienda ha sido suspendida - {nombreTienda}",
                HtmlContent = GenerarHtmlTiendaSuspendida(nombreUsuario, nombreTienda, motivo, diasGracia, frontendUrl)
            };

            var result = await _apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            _logger.LogInformation("Email de suspensión enviado a {Email} para tienda {Tienda}",
                emailDestino, nombreTienda);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificación de suspensión a {Email}", emailDestino);
            return false;
        }
    }

    /// <summary>
    /// Envía notificación de tienda reactivada
    /// </summary>
    public async Task<bool> EnviarTiendaReactivadaAsync(
        string emailDestino,
        string nombreUsuario,
        string nombreTienda)
    {
        try
        {
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                ?? _configuration["FrontendUrl"]
                ?? "http://localhost:4200";

            var sendSmtpEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender(_fromName, _fromEmail),
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(emailDestino, nombreUsuario) },
                Subject = $"✅ Tu tienda está activa nuevamente - {nombreTienda}",
                HtmlContent = GenerarHtmlTiendaReactivada(nombreUsuario, nombreTienda, frontendUrl)
            };

            var result = await _apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            _logger.LogInformation("Email de reactivación enviado a {Email} para tienda {Tienda}",
                emailDestino, nombreTienda);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificación de reactivación a {Email}", emailDestino);
            return false;
        }
    }

    #endregion

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

    private string GenerarHtmlAvisoTrial(string nombreUsuario, string nombreTienda, string nombrePlan, int diasRestantes, decimal precioMensual, string frontendUrl)
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
        .alerta {{ font-size: 24px; font-weight: bold; color: #ffc107; text-align: center; padding: 20px; background-color: #fff3cd; border-radius: 5px; margin: 20px 0; }}
        .btn {{ display: inline-block; padding: 15px 30px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>⏰ Tu período de prueba está por terminar</h1></div>
        <div class='content'>
            <p>Hola {nombreUsuario},</p>
            <p>Tu período de prueba gratuito para <strong>{nombreTienda}</strong> termina en:</p>
            <div class='alerta'>{diasRestantes} día(s)</div>
            <p>Para continuar disfrutando de tu tienda online con el <strong>{nombrePlan}</strong>,
               asegúrate de tener un método de pago válido configurado.</p>
            <p>Después del período de prueba, se cobrará <strong>${precioMensual:N0}/mes</strong>.</p>
            <p style='text-align:center;'>
                <a href='{frontendUrl}/emprendedor/suscripcion' class='btn'>Revisar mi suscripción</a>
            </p>
            <p>Si decides no continuar, puedes cancelar en cualquier momento desde tu panel.</p>
            <p>Saludos,<br>El equipo de E-Commerce</p>
        </div>
        <div class='footer'><p>Este es un correo automático.</p></div>
    </div>
</body>
</html>";
    }

    private string GenerarHtmlPagoExitoso(string nombreUsuario, string nombreTienda, string nombrePlan, decimal monto, DateTime proximoPago)
    {
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
        .monto {{ font-size: 28px; font-weight: bold; color: #28a745; text-align: center; padding: 15px; background-color: #d4edda; border-radius: 5px; margin: 20px 0; }}
        .detalle {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>✅ Pago Confirmado</h1></div>
        <div class='content'>
            <p>Hola {nombreUsuario},</p>
            <p>Tu pago de suscripción ha sido procesado exitosamente.</p>
            <div class='monto'>${monto:N0} ARS</div>
            <div class='detalle'>
                <p><strong>Tienda:</strong> {nombreTienda}</p>
                <p><strong>Plan:</strong> {nombrePlan}</p>
                <p><strong>Próximo pago:</strong> {proximoPago:dd/MM/yyyy}</p>
            </div>
            <p>Tu tienda seguirá activa y funcionando normalmente.</p>
            <p>Gracias por confiar en nosotros.</p>
            <p>Saludos,<br>El equipo de E-Commerce</p>
        </div>
        <div class='footer'><p>Este es un correo automático.</p></div>
    </div>
</body>
</html>";
    }

    private string GenerarHtmlPagoFallido(string nombreUsuario, string nombreTienda, string nombrePlan, int intentosRestantes, string frontendUrl)
    {
        var mensajeUrgencia = intentosRestantes <= 1
            ? "<p style='color: #dc3545; font-weight: bold;'>⚠️ Este es tu último intento antes de que tu tienda sea suspendida.</p>"
            : $"<p>Tienes <strong>{intentosRestantes}</strong> intento(s) restantes antes de que tu tienda sea suspendida.</p>";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .container {{ background-color: #f4f4f4; padding: 30px; border-radius: 10px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background-color: white; padding: 30px; border-radius: 0 0 10px 10px; }}
        .alerta {{ font-size: 18px; color: #dc3545; text-align: center; padding: 15px; background-color: #f8d7da; border-radius: 5px; margin: 20px 0; border: 1px solid #f5c6cb; }}
        .btn {{ display: inline-block; padding: 15px 30px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>⚠️ Problema con tu pago</h1></div>
        <div class='content'>
            <p>Hola {nombreUsuario},</p>
            <p>No pudimos procesar el pago de tu suscripción para <strong>{nombreTienda}</strong>.</p>
            <div class='alerta'>
                <p><strong>Plan:</strong> {nombrePlan}</p>
                <p>El pago fue rechazado o no pudo completarse.</p>
            </div>
            {mensajeUrgencia}
            <p>Por favor, verifica que:</p>
            <ul>
                <li>Tu tarjeta tenga fondos suficientes</li>
                <li>Los datos de la tarjeta sean correctos</li>
                <li>La tarjeta no esté vencida</li>
            </ul>
            <p style='text-align:center;'>
                <a href='{frontendUrl}/emprendedor/suscripcion' class='btn'>Actualizar método de pago</a>
            </p>
            <p>Saludos,<br>El equipo de E-Commerce</p>
        </div>
        <div class='footer'><p>Este es un correo automático.</p></div>
    </div>
</body>
</html>";
    }

    private string GenerarHtmlTiendaSuspendida(string nombreUsuario, string nombreTienda, string motivo, int diasGracia, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .container {{ background-color: #f4f4f4; padding: 30px; border-radius: 10px; }}
        .header {{ background-color: #343a40; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background-color: white; padding: 30px; border-radius: 0 0 10px 10px; }}
        .alerta {{ font-size: 18px; color: #721c24; text-align: center; padding: 15px; background-color: #f8d7da; border-radius: 5px; margin: 20px 0; border: 1px solid #f5c6cb; }}
        .btn {{ display: inline-block; padding: 15px 30px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>🚨 Tienda Suspendida</h1></div>
        <div class='content'>
            <p>Hola {nombreUsuario},</p>
            <p>Lamentamos informarte que tu tienda <strong>{nombreTienda}</strong> ha sido suspendida.</p>
            <div class='alerta'>
                <p><strong>Motivo:</strong> {motivo}</p>
            </div>
            <p>Tu tienda ya no es visible para los clientes y no puedes recibir nuevos pedidos.</p>
            <p><strong>Tienes {diasGracia} días</strong> para reactivar tu suscripción antes de que los datos
               de tu tienda puedan ser eliminados permanentemente.</p>
            <p style='text-align:center;'>
                <a href='{frontendUrl}/emprendedor/suscripcion' class='btn'>Reactivar mi tienda</a>
            </p>
            <p>Si tienes alguna pregunta, no dudes en contactarnos.</p>
            <p>Saludos,<br>El equipo de E-Commerce</p>
        </div>
        <div class='footer'><p>Este es un correo automático.</p></div>
    </div>
</body>
</html>";
    }

    private string GenerarHtmlTiendaReactivada(string nombreUsuario, string nombreTienda, string frontendUrl)
    {
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
        .exito {{ font-size: 24px; font-weight: bold; color: #28a745; text-align: center; padding: 20px; background-color: #d4edda; border-radius: 5px; margin: 20px 0; }}
        .btn {{ display: inline-block; padding: 15px 30px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>✅ ¡Tu tienda está activa!</h1></div>
        <div class='content'>
            <p>Hola {nombreUsuario},</p>
            <p>¡Excelentes noticias! Tu tienda <strong>{nombreTienda}</strong> ha sido reactivada exitosamente.</p>
            <div class='exito'>Tu tienda ya está visible para tus clientes</div>
            <p>Ya puedes:</p>
            <ul>
                <li>Recibir nuevos pedidos</li>
                <li>Administrar tus productos</li>
                <li>Gestionar tu tienda normalmente</li>
            </ul>
            <p style='text-align:center;'>
                <a href='{frontendUrl}/emprendedor/dashboard' class='btn'>Ir a mi panel</a>
            </p>
            <p>Gracias por seguir confiando en nosotros.</p>
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
