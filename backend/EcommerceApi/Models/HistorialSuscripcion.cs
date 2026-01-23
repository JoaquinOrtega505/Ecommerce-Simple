namespace EcommerceApi.Models;

public class HistorialSuscripcion
{
    public int Id { get; set; }
    public int TiendaId { get; set; }
    public int PlanSuscripcionId { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty; // "Activa", "Cancelada", "Vencida", "Cambiada"
    public string? MetodoPago { get; set; } // "MercadoPago", "Transferencia", etc.
    public string? TransaccionId { get; set; } // ID de transacción de MercadoPago
    public decimal MontoTotal { get; set; }
    public string? Notas { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Tienda Tienda { get; set; } = null!;
    public virtual PlanSuscripcion PlanSuscripcion { get; set; } = null!;
}
