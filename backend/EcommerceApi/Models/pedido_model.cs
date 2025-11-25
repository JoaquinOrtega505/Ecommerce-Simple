namespace EcommerceApi.Models;

public class Pedido
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "Pendiente"; // Pendiente, Pagado, Enviado, Entregado, Cancelado
    public string DireccionEnvio { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaEntrega { get; set; }
    
    // Relaciones
    public Usuario? Usuario { get; set; }
    public ICollection<PedidoItem> Items { get; set; } = new List<PedidoItem>();
}
