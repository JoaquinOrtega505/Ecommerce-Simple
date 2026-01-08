namespace EcommerceApi.DTOs;

/// <summary>
/// DTO para recibir notificaciones de entrega desde servicios externos
/// </summary>
public class WebhookEntregaDto
{
    /// <summary>
    /// ID del pedido en nuestro sistema
    /// </summary>
    public int PedidoId { get; set; }

    /// <summary>
    /// Estado de la entrega: "entregado", "en_transito", "fallido", etc.
    /// </summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del servicio de envío (Andreani, OCA, Correo Argentino, etc.)
    /// </summary>
    public string ServicioEnvio { get; set; } = string.Empty;

    /// <summary>
    /// Número de seguimiento del envío
    /// </summary>
    public string? NumeroSeguimiento { get; set; }

    /// <summary>
    /// Fecha y hora de la entrega (si aplica)
    /// </summary>
    public DateTime? FechaEntrega { get; set; }

    /// <summary>
    /// Observaciones o notas adicionales
    /// </summary>
    public string? Observaciones { get; set; }

    /// <summary>
    /// Nombre de quien recibió el paquete (opcional)
    /// </summary>
    public string? ReceptorNombre { get; set; }

    /// <summary>
    /// DNI o documento de quien recibió (opcional)
    /// </summary>
    public string? ReceptorDocumento { get; set; }

    /// <summary>
    /// Secret key para validar que la petición viene del servicio autorizado
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Datos adicionales en formato JSON (para campos específicos del servicio)
    /// </summary>
    public string? MetadataJson { get; set; }
}
