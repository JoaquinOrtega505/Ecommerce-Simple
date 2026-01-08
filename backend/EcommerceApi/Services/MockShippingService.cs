namespace EcommerceApi.Services;

/// <summary>
/// Servicio simulado de envíos que genera números de tracking automáticamente
/// Útil para desarrollo y demos sin necesidad de integración real
/// </summary>
public class MockShippingService
{
    private readonly ILogger<MockShippingService> _logger;
    private static int _trackingCounter = 1000;

    public MockShippingService(ILogger<MockShippingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Genera un número de seguimiento simulado
    /// </summary>
    public string GenerarNumeroSeguimiento(string servicio = "Andreani")
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var numero = Interlocked.Increment(ref _trackingCounter);
        var trackingNumber = $"{servicio.ToUpper()}-{timestamp}-{numero:D6}";

        _logger.LogInformation("Número de seguimiento generado: {TrackingNumber}", trackingNumber);

        return trackingNumber;
    }

    /// <summary>
    /// Simula la creación de un envío
    /// </summary>
    public Task<ShippingResponse> CrearEnvioSimuladoAsync(int pedidoId, string direccion)
    {
        var trackingNumber = GenerarNumeroSeguimiento("ANDREANI");

        _logger.LogInformation("Envío simulado creado para pedido {PedidoId}. Tracking: {TrackingNumber}",
            pedidoId, trackingNumber);

        var response = new ShippingResponse
        {
            NumeroSeguimiento = trackingNumber,
            Servicio = "Andreani (Simulado)",
            FechaCreacion = DateTime.UtcNow,
            Estado = "En preparación",
            Mensaje = "Envío creado exitosamente en modo simulación",
            UrlTracking = $"https://www.andreani.com/#!/personas/tracking/{trackingNumber}",
            EtiquetaUrl = $"/api/shipping/etiqueta/{trackingNumber}"
        };

        return Task.FromResult(response);
    }

    /// <summary>
    /// Simula el tracking de un envío
    /// </summary>
    public Task<TrackingInfo> ObtenerTrackingAsync(string numeroSeguimiento)
    {
        _logger.LogInformation("Consultando tracking simulado: {NumeroSeguimiento}", numeroSeguimiento);

        var info = new TrackingInfo
        {
            NumeroSeguimiento = numeroSeguimiento,
            EstadoActual = "En tránsito",
            Eventos = new List<TrackingEvento>
            {
                new() { Fecha = DateTime.UtcNow.AddHours(-24), Estado = "Ingresado", Descripcion = "El envío fue ingresado al sistema" },
                new() { Fecha = DateTime.UtcNow.AddHours(-20), Estado = "En preparación", Descripcion = "El envío está siendo preparado" },
                new() { Fecha = DateTime.UtcNow.AddHours(-12), Estado = "Despachado", Descripcion = "El envío fue despachado desde origen" },
                new() { Fecha = DateTime.UtcNow.AddHours(-6), Estado = "En tránsito", Descripcion = "El envío está en camino" }
            }
        };

        return Task.FromResult(info);
    }

    /// <summary>
    /// Simula la generación de una etiqueta de envío
    /// </summary>
    public byte[] GenerarEtiquetaSimulada(string numeroSeguimiento, string destinatario, string direccion)
    {
        // Genera un PDF simple o devuelve bytes de ejemplo
        // En un caso real, esto generaría un PDF con código de barras
        var contenido = $"ETIQUETA DE ENVÍO\n\nNúmero: {numeroSeguimiento}\nDestinatario: {destinatario}\nDirección: {direccion}\n\nEsta es una etiqueta simulada para testing.";
        return System.Text.Encoding.UTF8.GetBytes(contenido);
    }
}

// DTOs para el servicio simulado
public class ShippingResponse
{
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string Servicio { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string UrlTracking { get; set; } = string.Empty;
    public string EtiquetaUrl { get; set; } = string.Empty;
}

public class TrackingInfo
{
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string EstadoActual { get; set; } = string.Empty;
    public List<TrackingEvento> Eventos { get; set; } = new();
}

public class TrackingEvento
{
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}
