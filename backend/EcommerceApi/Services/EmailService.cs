using System.Net;
using System.Net.Mail;

namespace EcommerceApi.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> EnviarCodigoVerificacionAsync(string emailDestino, string codigo, string nombreUsuario)
    {
        try
        {
            // Leer de variables de entorno primero, luego appsettings
            var smtpHost = Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST")
                ?? _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT")
                ?? _configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = Environment.GetEnvironmentVariable("EMAIL_SMTP_USER")
                ?? _configuration["Email:SmtpUser"];
            var smtpPassword = Environment.GetEnvironmentVariable("EMAIL_SMTP_PASSWORD")
                ?? _configuration["Email:SmtpPassword"];
            var fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM_ADDRESS")
                ?? _configuration["Email:FromEmail"];
            var fromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME")
                ?? _configuration["Email:FromName"];

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail ?? smtpUser!, fromName ?? "E-Commerce"),
                Subject = "Código de Verificación - E-Commerce",
                Body = GenerarHtmlCodigoVerificacion(codigo, nombreUsuario),
                IsBodyHtml = true
            };

            mailMessage.To.Add(emailDestino);

            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = true
            };

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email de verificación enviado a {Email}", emailDestino);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email de verificación a {Email}", emailDestino);
            return false;
        }
    }

    private string GenerarHtmlCodigoVerificacion(string codigo, string nombreUsuario)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            background-color: #f4f4f4;
            padding: 30px;
            border-radius: 10px;
        }}
        .header {{
            background-color: #007bff;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 10px 10px 0 0;
        }}
        .content {{
            background-color: white;
            padding: 30px;
            border-radius: 0 0 10px 10px;
        }}
        .codigo {{
            font-size: 32px;
            font-weight: bold;
            color: #007bff;
            text-align: center;
            padding: 20px;
            background-color: #f8f9fa;
            border-radius: 5px;
            letter-spacing: 5px;
            margin: 20px 0;
        }}
        .footer {{
            text-align: center;
            margin-top: 20px;
            font-size: 12px;
            color: #666;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Verificación de Email</h1>
        </div>
        <div class='content'>
            <p>Hola {nombreUsuario},</p>
            <p>Gracias por registrarte en nuestro E-Commerce. Para completar tu registro, por favor verifica tu dirección de correo electrónico usando el siguiente código:</p>

            <div class='codigo'>{codigo}</div>

            <p>Este código expirará en <strong>15 minutos</strong>.</p>

            <p>Si no solicitaste este código, puedes ignorar este correo de forma segura.</p>

            <p>Saludos,<br>El equipo de E-Commerce</p>
        </div>
        <div class='footer'>
            <p>Este es un correo automático, por favor no respondas a este mensaje.</p>
        </div>
    </div>
</body>
</html>";
    }

    public string GenerarCodigoVerificacion()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}
