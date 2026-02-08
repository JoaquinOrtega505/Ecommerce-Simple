namespace EcommerceApi.Models;

public class PlanSuscripcion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int MaxProductos { get; set; }
    public decimal PrecioMensual { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // MercadoPago - ID del plan de suscripción en MP
    public string? MercadoPagoPlanId { get; set; }
    public DateTime? MercadoPagoSyncDate { get; set; }

    // Relación con Tiendas
    public ICollection<Tienda> Tiendas { get; set; } = new List<Tienda>();
}
