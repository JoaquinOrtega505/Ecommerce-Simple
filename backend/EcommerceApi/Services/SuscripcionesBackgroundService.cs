using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using EcommerceApi.Models;

namespace EcommerceApi.Services;

/// <summary>
/// Servicio en segundo plano que verifica periódicamente el estado de las suscripciones
/// </summary>
public class SuscripcionesBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SuscripcionesBackgroundService> _logger;
    private readonly TimeSpan _intervalo = TimeSpan.FromHours(1); // Ejecutar cada hora

    public SuscripcionesBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SuscripcionesBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SuscripcionesBackgroundService iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await VerificarSuscripcionesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en la verificación de suscripciones");
            }

            await Task.Delay(_intervalo, stoppingToken);
        }
    }

    private async Task VerificarSuscripcionesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mpService = scope.ServiceProvider.GetRequiredService<MercadoPagoSuscripcionesService>();
        var emailService = scope.ServiceProvider.GetRequiredService<BrevoEmailService>();

        _logger.LogInformation("Iniciando verificación de suscripciones...");

        var ahora = DateTime.UtcNow;
        var config = await context.ConfiguracionSuscripciones.FirstOrDefaultAsync(c => c.Activo);

        // 1. Enviar recordatorios de trial por expirar
        var diasAviso = config?.DiasAvisoFinTrial ?? 2;
        var recordatoriosEnviados = await EnviarRecordatoriosTrialAsync(context, emailService, ahora, diasAviso);
        _logger.LogInformation("Recordatorios de trial enviados: {Count}", recordatoriosEnviados);

        // 2. Verificar trials expirados
        var trialsExpirados = await VerificarTrialsExpiradosAsync(context, mpService, emailService, ahora, config);
        _logger.LogInformation("Trials expirados procesados: {Count}", trialsExpirados);

        // 3. Verificar suscripciones vencidas
        var suscripcionesVencidas = await VerificarSuscripcionesVencidasAsync(context, emailService, ahora, config);
        _logger.LogInformation("Suscripciones vencidas procesadas: {Count}", suscripcionesVencidas);

        // 4. Verificar tiendas en gracia que deben marcarse para eliminación
        var diasGracia = config?.DiasGraciaSuspension ?? 3;
        var enGraciaParaEliminar = await VerificarTiendasEnGraciaAsync(context, ahora, diasGracia);
        _logger.LogInformation("Tiendas en período de gracia expirado: {Count}", enGraciaParaEliminar);
    }

    private async Task<int> EnviarRecordatoriosTrialAsync(
        AppDbContext context,
        BrevoEmailService emailService,
        DateTime ahora,
        int diasAviso)
    {
        var fechaLimite = ahora.AddDays(diasAviso);

        var tiendasTrialPorExpirar = await context.Tiendas
            .Include(t => t.PlanSuscripcion)
            .Include(t => t.Usuarios)
            .Where(t => t.EstadoSuscripcion == "trial" &&
                        t.FechaFinTrial.HasValue &&
                        t.FechaFinTrial.Value <= fechaLimite &&
                        t.FechaFinTrial.Value > ahora)
            .ToListAsync();

        var enviados = 0;
        foreach (var tienda in tiendasTrialPorExpirar)
        {
            var emprendedor = tienda.Usuarios.FirstOrDefault(u => u.Rol == "Emprendedor");
            if (emprendedor == null) continue;

            var diasRestantes = (int)(tienda.FechaFinTrial!.Value - ahora).TotalDays;
            if (diasRestantes < 0) diasRestantes = 0;

            try
            {
                var enviado = await emailService.EnviarAvisoTrialPorExpirarAsync(
                    emprendedor.Email,
                    emprendedor.Nombre,
                    tienda.Nombre,
                    tienda.PlanSuscripcion?.Nombre ?? "Plan",
                    diasRestantes,
                    tienda.PlanSuscripcion?.PrecioMensual ?? 0);

                if (enviado)
                {
                    enviados++;
                    _logger.LogInformation("Recordatorio de trial enviado a {Email} para tienda {Tienda}",
                        emprendedor.Email, tienda.Nombre);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando recordatorio de trial a {Email}", emprendedor.Email);
            }
        }

        return enviados;
    }

    private async Task<int> VerificarTrialsExpiradosAsync(
        AppDbContext context,
        MercadoPagoSuscripcionesService mpService,
        BrevoEmailService emailService,
        DateTime ahora,
        ConfiguracionSuscripciones? config)
    {
        var tiendasTrialExpirado = await context.Tiendas
            .Include(t => t.Usuarios)
            .Include(t => t.PlanSuscripcion)
            .Where(t => t.EstadoSuscripcion == "trial" &&
                        t.FechaFinTrial.HasValue &&
                        t.FechaFinTrial.Value <= ahora)
            .ToListAsync();

        var diasGracia = config?.DiasGraciaSuspension ?? 3;

        foreach (var tienda in tiendasTrialExpirado)
        {
            // Si tiene suscripción de MP, verificar estado
            if (!string.IsNullOrEmpty(tienda.MercadoPagoSuscripcionId))
            {
                var estado = await mpService.ObtenerEstadoSuscripcionAsync(tienda.MercadoPagoSuscripcionId);
                if (estado.Success && (estado.Status == "authorized" || estado.Status == "active"))
                {
                    tienda.EstadoSuscripcion = "active";
                    tienda.EstadoTienda = "Activa";
                    tienda.FechaVencimientoSuscripcion = ahora.AddMonths(1);
                    _logger.LogInformation("Tienda {TiendaId}: Trial expirado pero suscripción activa", tienda.Id);
                    continue;
                }
            }

            // Trial expirado sin pago activo
            tienda.EstadoSuscripcion = "expired";
            tienda.EstadoTienda = "Suspendida";
            tienda.FechaModificacion = ahora;
            _logger.LogInformation("Tienda {TiendaId}: Trial expirado, tienda suspendida", tienda.Id);

            // Enviar notificación de suspensión
            var emprendedor = tienda.Usuarios.FirstOrDefault(u => u.Rol == "Emprendedor");
            if (emprendedor != null)
            {
                try
                {
                    await emailService.EnviarTiendaSuspendidaAsync(
                        emprendedor.Email,
                        emprendedor.Nombre,
                        tienda.Nombre,
                        "Tu período de prueba ha terminado sin un método de pago activo",
                        diasGracia);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando email de suspensión a {Email}", emprendedor.Email);
                }
            }
        }

        await context.SaveChangesAsync();
        return tiendasTrialExpirado.Count;
    }

    private async Task<int> VerificarSuscripcionesVencidasAsync(
        AppDbContext context,
        BrevoEmailService emailService,
        DateTime ahora,
        ConfiguracionSuscripciones? config)
    {
        var suscripcionesVencidas = await context.Tiendas
            .Include(t => t.Usuarios)
            .Where(t => t.EstadoSuscripcion == "active" &&
                        t.FechaVencimientoSuscripcion.HasValue &&
                        t.FechaVencimientoSuscripcion.Value <= ahora)
            .ToListAsync();

        var diasGracia = config?.DiasGraciaSuspension ?? 3;

        foreach (var tienda in suscripcionesVencidas)
        {
            tienda.EstadoSuscripcion = "expired";
            tienda.EstadoTienda = "Suspendida";
            tienda.FechaModificacion = ahora;
            _logger.LogWarning("Tienda {TiendaId}: Suscripción vencida, tienda suspendida", tienda.Id);

            // Enviar notificación de suspensión
            var emprendedor = tienda.Usuarios.FirstOrDefault(u => u.Rol == "Emprendedor");
            if (emprendedor != null)
            {
                try
                {
                    await emailService.EnviarTiendaSuspendidaAsync(
                        emprendedor.Email,
                        emprendedor.Nombre,
                        tienda.Nombre,
                        "Tu suscripción ha vencido y no se pudo renovar",
                        diasGracia);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando email de suspensión a {Email}", emprendedor.Email);
                }
            }
        }

        await context.SaveChangesAsync();
        return suscripcionesVencidas.Count;
    }

    private async Task<int> VerificarTiendasEnGraciaAsync(
        AppDbContext context,
        DateTime ahora,
        int diasGracia)
    {
        // Buscar tiendas suspendidas por más de X días
        var fechaLimite = ahora.AddDays(-diasGracia);

        var tiendasEnGraciaExpirada = await context.Tiendas
            .Where(t => t.EstadoTienda == "Suspendida" &&
                        t.FechaModificacion.HasValue &&
                        t.FechaModificacion.Value <= fechaLimite)
            .ToListAsync();

        foreach (var tienda in tiendasEnGraciaExpirada)
        {
            // Marcar como "PendienteEliminacion" pero no eliminar automáticamente
            // La eliminación debe ser manual por el SuperAdmin
            tienda.EstadoTienda = "PendienteEliminacion";
            tienda.FechaModificacion = ahora;
            _logger.LogWarning("Tienda {TiendaId}: Período de gracia expirado, marcada para eliminación",
                tienda.Id);
        }

        await context.SaveChangesAsync();
        return tiendasEnGraciaExpirada.Count;
    }
}
